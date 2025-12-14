namespace DistributedCarAuction.Domain.Entities;

using DistributedCarAuction.Domain.Common;

public class Lot : BaseEntity
{
    public Guid AuctionId { get; init; }

    public Vehicle Vehicle { get; init; }

    public decimal StartingBid { get; init; }

    public decimal? ReservePrice { get; init; }

    private readonly List<Bid> _bids = new();
    private readonly object _bidsLock = new();

    /// <summary>
    /// Returns a snapshot of the current bids (thread-safe).
    /// </summary>
    public IReadOnlyList<Bid> Bids
    {
        get
        {
            lock (_bidsLock)
            {
                return _bids.ToList().AsReadOnly();
            }
        }
    }

    private long _bidSequence = 0;

    private Lot() 
    { 
        Vehicle = null!;
    }

    public Lot(Guid auctionId, Vehicle vehicle, decimal startingBid, decimal? reservePrice = null)
    {
        if (auctionId == Guid.Empty)
            throw new ArgumentException("Auction ID cannot be empty", nameof(auctionId));
        
        if (vehicle == null)
            throw new ArgumentNullException(nameof(vehicle));
        
        if (startingBid <= 0)
            throw new ArgumentException("Starting bid must be greater than zero", nameof(startingBid));

        AuctionId = auctionId;
        Vehicle = vehicle;
        StartingBid = startingBid;
        ReservePrice = reservePrice;
    }

    /// <summary>
    /// HIGH-AVAILABILITY BIDDING: Accepts all bids optimistically.
    /// Validation happens at query time (GetValidBids, GetWinningBid).
    /// This ensures maximum bid acceptance during network partitions.
    /// Thread-safe: Uses Interlocked for sequence and lock for collection.
    /// </summary>
    public void PlaceBid(Guid bidderId, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Bid amount must be greater than zero", nameof(amount));

        long sequence = Interlocked.Increment(ref _bidSequence);
        Bid bid = new(bidderId, Id, amount, sequence);
        
        lock (_bidsLock)
        {
            _bids.Add(bid);
        }
        
        SetUpdatedAt();
    }

    /// <summary>
    /// Returns all bids (including potentially invalid ones).
    /// Use GetValidBids() for filtered list.
    /// Thread-safe: Delegates to GetValidBids which holds the lock.
    /// </summary>
    public decimal GetHighestBidAmount()
    {
        List<Bid> validBids = GetValidBids();
        if (validBids.Count == 0)
            return StartingBid;

        return validBids.Max(b => b.Amount);
    }

    /// <summary>
    /// CONSISTENCY AT QUERY TIME: Filters bids to only those that were valid
    /// (higher than the previous valid bid in sequence order).
    /// Thread-safe: Takes a snapshot of bids under lock.
    /// </summary>
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

    /// <summary>
    /// Returns the highest valid bid (consistency enforced at read time).
    /// Thread-safe: Delegates to GetValidBids which holds the lock.
    /// </summary>
    public Bid? GetHighestBid()
    {
        List<Bid> validBids = GetValidBids();
        if (validBids.Count == 0)
            return null;

        // Last valid bid is the highest (already filtered and ordered)
        return validBids.LastOrDefault();
    }

    /// <summary>
    /// CONSISTENT WINNER DETERMINATION: Only valid bids are considered.
    /// Reserve price check happens here.
    /// Thread-safe: Delegates to GetHighestBid.
    /// </summary>
    public Guid? GetWinningBidderId()
    {
        Bid? highestBid = GetHighestBid();
        
        // Check if reserve price is met (if set)
        if (ReservePrice.HasValue && (highestBid?.Amount ?? 0) < ReservePrice.Value)
            return null;

        return highestBid?.BidderId;
    }

    /// <summary>
    /// Check if a bid amount would be valid (for client feedback).
    /// Note: Bid is still accepted even if this returns false.
    /// Thread-safe: Delegates to GetHighestBidAmount.
    /// </summary>
    public bool WouldBidBeValid(decimal amount)
    {
        return amount > GetHighestBidAmount();
    }
}

