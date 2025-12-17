# Distributed Car Auction - Design Write-Up

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              API Layer                                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │
│  │ AuctionsController│  │ LotsController  │  │ PartnerController           │  │
│  │ [ApiKeyRequired]  │  │ [ApiKeyRequired]│  │ [AllowAnonymous] ⚠️         │  │
│  └────────┬─────────┘  └────────┬────────┘  └─────────────┬───────────────┘  │
└───────────┼─────────────────────┼─────────────────────────┼──────────────────┘
            │                     │                         │
            ▼                     ▼                         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Application Layer                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                        Service Classes                               │    │
│  │  ┌─────────────────┐  ┌─────────────────┐                           │    │
│  │  │ AuctionService  │  │ LotService      │                           │    │
│  │  │                 │  │                 │                           │    │
│  │  │ • UoW only      │  │ • UoW only      │                           │    │
│  │  │ • Per-entity    │  │ • Per-entity    │                           │    │
│  │  │   locking       │  │   locking       │                           │    │
│  │  │ • Retry logic   │  │ • Retry logic   │                           │    │
│  │  └────────┬────────┘  └────────┬────────┘                           │    │
│  └───────────┼────────────────────┼────────────────────────────────────┘    │
│              │                    │                                          │
│              ▼                    ▼                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    Unit of Work Factory                              │    │
│  │  • Creates transactional scope                                       │    │
│  │  • Tracks pending changes                                            │    │
│  │  • Atomic commit/rollback                                            │    │
│  └────────────────────────────────┬────────────────────────────────────┘    │
└───────────────────────────────────┼──────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Infrastructure Layer                                  │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────────┐   │
│  │ InMemory         │  │ InMemory         │  │ InMemory                 │   │
│  │ AuctionRepository│  │ LotRepository    │  │ SequenceGenerator        │   │
│  │ • Version check  │  │ • Version check  │  │ • Per-lot counters       │   │
│  └──────────────────┘  └──────────────────┘  └──────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## CAP Theorem Trade-offs

This system uses a **nuanced CAP approach**—different operations have different requirements:

| Operation | CAP Choice | Rationale |
|-----------|------------|-----------|
| **Bid Placement** | AP (Availability) | Accept all bids; don't lose customer engagement during partitions |
| **Winner Determination** | CP (Consistency) | Must be accurate; legal/financial implications |
| **Auction State** | CP (Consistency) | State transitions must be atomic and consistent |

### High-Availability Bidding

Bids are accepted **optimistically** without rejecting based on current highest bid:

```csharp
public void PlaceBid(Guid bidderId, decimal amount, long sequence)
{
    // Accept all bids for availability
    Bid bid = new(bidderId, Id, amount, sequence);
    lock (_bidsLock)
    {
        _bids.Add(bid);
    }
    SetUpdatedAt();
}
```

Users receive immediate feedback on whether their bid is currently the highest (`IsCurrentlyHighest`), but the bid is never rejected due to amount.

### Consistent Winner Resolution

Validity is enforced at **query time** when consistency matters:

```csharp
public List<Bid> GetValidBids()
{
    List<Bid> bidsSnapshot;
    lock (_bidsLock)
    {
        bidsSnapshot = _bids.ToList();
    }

    List<Bid> validBids = new();
    decimal currentHighest = StartingBid;
    
    foreach (Bid bid in bidsSnapshot.OrderBy(b => b.Sequence))
    {
        if (bid.Amount > currentHighest)
        {
            validBids.Add(bid);
            currentHighest = bid.Amount;
        }
    }
    return validBids;
}
```

---

## Concurrency Control Architecture

### Thread-Safety Layers

| Layer | Mechanism | Purpose |
|-------|-----------|---------|
| **Domain Entities** | `lock` objects, `Interlocked` | Protect internal collections and atomic operations |
| **Service Layer** | `SemaphoreSlim` per entity | Serialize operations on same auction/lot |
| **Repository Layer** | Version checking | Detect concurrent modifications |
| **Transaction Layer** | Unit of Work | Atomic multi-repository commits |

### Per-Entity Locking Pattern

```csharp
private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _lotLocks = new();

public async Task<BidResult> PlaceBidAsync(BidRequest request)
{
    SemaphoreSlim lotLock = _lotLocks.GetOrAdd(request.LotId, _ => new SemaphoreSlim(1, 1));
    await lotLock.WaitAsync();
    try
    {
        // All bid processing serialized per-lot
        // Different lots processed concurrently
    }
    finally
    {
        lotLock.Release();
    }
}
```

### Optimistic Concurrency Control

```csharp
// BaseEntity
public int Version { get; private set; } = 1;

protected void SetUpdatedAt()
{
    UpdatedAt = DateTime.UtcNow;
    Version++;
}

// Repository
public Task UpdateAsync(Lot lot)
{
    if (lot.Version != storedVersion + 1)
        throw new ConcurrencyException(nameof(Lot), lot.Id, storedVersion + 1, lot.Version);
    
    _lots[lot.Id] = lot;
    _storedVersions[lot.Id] = lot.Version;
}
```

### Retry with Exponential Backoff

```csharp
for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
{
    await using IUnitOfWork uow = _unitOfWorkFactory.Create();
    try
    {
        // ... operation ...
        await uow.CommitAsync();
        break;
    }
    catch (ConcurrencyException) when (attempt < MaxRetryAttempts - 1)
    {
        await Task.Delay(RetryBaseDelay * (int)Math.Pow(2, attempt));
    }
}
```

---

## Unit of Work Pattern

### Interface

```csharp
public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    IAuctionRepository Auctions { get; }
    ILotRepository Lots { get; }
    IVehicleRepository Vehicles { get; }
    
    Task<int> CommitAsync();
    void Rollback();
    bool HasPendingChanges { get; }
}
```

### Usage in Services

```csharp
public async Task<Lot> CreateLotAsync(CreateLotRequest request)
{
    await using IUnitOfWork uow = _unitOfWorkFactory.Create();
    
    Vehicle? vehicle = await uow.Vehicles.GetByIdAsync(request.VehicleId);
    Auction? auction = await uow.Auctions.GetByIdAsync(request.AuctionId);
    
    Lot lot = new(request.AuctionId, vehicle, request.StartingBid);
    auction.AddLot(lot);
    
    await uow.Auctions.UpdateAsync(auction);
    await uow.Lots.AddAsync(lot);
    
    await uow.CommitAsync();  // Atomic - both succeed or both fail
    
    return lot;
}
```

---

## Distributed Sequence Generation

### Interface

```csharp
public interface ISequenceGenerator
{
    Task<long> GetNextSequenceAsync(Guid lotId);
    long GetNextSequence(Guid lotId);
}
```

### Single-Instance (In-Memory)

```csharp
public class InMemorySequenceGenerator : ISequenceGenerator
{
    private readonly ConcurrentDictionary<Guid, long> _sequences = new();

    public long GetNextSequence(Guid lotId)
    {
        return _sequences.AddOrUpdate(lotId, 1, (_, v) => v + 1);
    }
}
```

### Multi-Instance (Redis) - Stub

```csharp
public class RedisSequenceGenerator : ISequenceGenerator
{
    public async Task<long> GetNextSequenceAsync(Guid lotId)
    {
        var db = _redis.GetDatabase();
        var key = $"bid:seq:{lotId}";
        return await db.StringIncrementAsync(key);  // Atomic INCR
    }
}
```

---

## Broadcast Approach: Kafka-Based Event Streaming

### Architecture: Dual-Channel Communication

| Channel | Technology | Purpose |
|---------|------------|---------|
| **NotificationService** | SignalR/WebSockets | Real-time updates to connected users |
| **BroadcastService** | Apache Kafka | Reliable event streaming to partners |

### Why Kafka?

| Requirement | Kafka Solution |
|-------------|----------------|
| **Guaranteed Delivery** | Persistent log; messages retained until consumed |
| **Event Ordering** | Partition by `AuctionId` = ordered events per auction |
| **Partner Downtime** | Partners catch up from last offset when back online |
| **Audit Trail** | Full event history for replay and debugging |
| **Scalability** | Horizontal scaling with consumer groups |

### Kafka Topic Design

```
auction-events (partitioned by AuctionId)
├── AuctionCreated    { auctionId, title, timestamp }
├── AuctionStarted    { auctionId, timestamp }
├── AuctionEnded      { auctionId, winnerId, finalAmount, timestamp }
└── BidPlaced         { auctionId, lotId, bidderId, amount, sequence, timestamp }
```

### Event Flow

```
┌─────────────┐      Publish       ┌─────────────────────────────────────┐
│ Auction API │ ─────────────────► │           Kafka Cluster             │
└─────────────┘                    │  Topic: auction-events              │
                                   │  Partitions: by AuctionId           │
                                   └──────────────┬──────────────────────┘
                                                  │
                    ┌─────────────────────────────┼─────────────────────────────┐
                    │                             │                             │
                    ▼                             ▼                             ▼
         ┌──────────────────┐          ┌──────────────────┐          ┌──────────────────┐
         │ Partner A        │          │ Partner B        │          │ Webhook Adapter  │
         │ (Kafka Consumer) │          │ (Kafka Consumer) │          │ (for legacy)     │
         └──────────────────┘          └──────────────────┘          └──────────────────┘
```

### Event Schema

```csharp
public record AuctionEvent
{
    public Guid EventId { get; init; }           // Idempotency key
    public string EventType { get; init; }       // "BidPlaced", "AuctionEnded", etc.
    public Guid AuctionId { get; init; }         // Partition key
    public DateTime Timestamp { get; init; }
    public object Payload { get; init; }         // Event-specific data
}

public record BidPlacedPayload(
    Guid LotId,
    Guid BidderId,
    decimal Amount,
    long Sequence,
    bool IsCurrentlyHighest,
    decimal CurrentHighestBid
);
```

### Partner Integration Options

| Option | Use Case | Implementation |
|--------|----------|----------------|
| **Kafka Consumer** | Tech-savvy partners | Direct topic subscription |
| **Webhook Adapter** | Legacy partners | Kafka → HTTP POST adapter service |
| **REST Polling** | Simple integration | `/api/events?since={timestamp}` endpoint |

### Production Considerations

| Concern | Solution |
|---------|----------|
| **Idempotency** | `EventId` allows partners to deduplicate |
| **Schema Evolution** | Schema Registry (Avro/Protobuf) for compatibility |
| **Dead Letter Queue** | Failed webhook deliveries → DLQ for retry |
| **Monitoring** | Consumer lag alerts, throughput metrics |
| **Security** | SASL/SSL authentication, ACLs per partner |
| **Retention** | 7-day retention for replay capability |

---

## Known Security Considerations

| Issue | Severity | Status |
|-------|----------|--------|
| Partner API is `[AllowAnonymous]` | 🔴 Critical | Open |
| BidderId from request body (impersonation risk) | 🔴 Critical | Open |
| No API key = allow all (dev mode) | 🟠 Medium | Open |
| String comparison for API key (timing attack) | 🟡 Low | Open |
| Static lock dictionaries grow unbounded | 🟡 Low | Open |

See `TODO.md` for full security remediation plan.

---

*This design prioritizes availability for bid acceptance while ensuring consistency for winner determination, with comprehensive concurrency controls for multi-instance deployments.*
