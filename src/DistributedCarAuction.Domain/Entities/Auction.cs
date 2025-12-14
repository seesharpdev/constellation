namespace DistributedCarAuction.Domain.Entities;

using DistributedCarAuction.Domain.Common;
using DistributedCarAuction.Domain.Enums;

public class Auction : BaseEntity
{
    public string Title { get; init; }

    public string Description { get; init; }

    public AuctionState State { get; private set; }

    public DateTime? StartTime { get; private set; }

    public DateTime? EndTime { get; private set; }

    private readonly List<Lot> _lots = new();
    private readonly object _lotsLock = new();
    private readonly object _stateLock = new();

    /// <summary>
    /// Returns a snapshot of the current lots (thread-safe).
    /// </summary>
    public IReadOnlyList<Lot> Lots
    {
        get
        {
            lock (_lotsLock)
            {
                return _lots.ToList().AsReadOnly();
            }
        }
    }

    private Auction() 
    { 
        Title = string.Empty;
        Description = string.Empty;
    }

    public Auction(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        Title = title;
        Description = description ?? string.Empty;
        State = AuctionState.Created;
    }

    /// <summary>
    /// Adds a lot to this auction.
    /// Thread-safe: Uses locks to protect state check and collection modification.
    /// </summary>
    public void AddLot(Lot lot)
    {
        ArgumentNullException.ThrowIfNull(lot);

        lock (_stateLock)
        {
            if (State != AuctionState.Created)
                throw new InvalidOperationException("Lots can only be added to auctions in Created state");

            lock (_lotsLock)
            {
                _lots.Add(lot);
            }
            
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Starts the auction, enabling bid acceptance.
    /// Thread-safe: Uses lock to ensure atomic state transition.
    /// </summary>
    public void Start()
    {
        lock (_stateLock)
        {
            if (State != AuctionState.Created)
                throw new InvalidOperationException($"Cannot start auction from {State} state");

            int lotCount;
            lock (_lotsLock)
            {
                lotCount = _lots.Count;
            }

            if (lotCount == 0)
                throw new InvalidOperationException("Cannot start auction without lots");

            State = AuctionState.Active;
            StartTime = DateTime.UtcNow;
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Ends the auction, preventing further bids.
    /// Thread-safe: Uses lock to ensure atomic state transition.
    /// </summary>
    public void End()
    {
        lock (_stateLock)
        {
            if (State != AuctionState.Active)
                throw new InvalidOperationException($"Cannot end auction from {State} state");

            State = AuctionState.Ended;
            EndTime = DateTime.UtcNow;
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Checks if the auction can currently accept bids.
    /// Thread-safe: State read is atomic for enum types.
    /// </summary>
    public bool CanAcceptBids()
    {
        // State is an enum (int), reads are atomic on x86/x64
        // For absolute safety in all scenarios, we could lock, but this is acceptable
        return State == AuctionState.Active;
    }

    /// <summary>
    /// Retrieves a lot by its ID.
    /// Thread-safe: Takes a snapshot under lock.
    /// </summary>
    public Lot? GetLot(Guid lotId)
    {
        lock (_lotsLock)
        {
            return _lots.FirstOrDefault(l => l.Id == lotId);
        }
    }
}

