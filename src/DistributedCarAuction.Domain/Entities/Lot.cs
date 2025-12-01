namespace DistributedCarAuction.Domain.Entities;

using DistributedCarAuction.Domain.Common;

public class Lot : BaseEntity
{
    public Guid AuctionId { get; init; }

    public Vehicle Vehicle { get; init; }

    public decimal StartingBid { get; init; }

    public decimal? ReservePrice { get; init; }

    private readonly List<Bid> _bids = new();

    public IReadOnlyList<Bid> Bids => _bids.AsReadOnly();

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
    /// </summary>
    public void PlaceBid(Guid bidderId, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Bid amount must be greater than zero", nameof(amount));

        Bid bid = new(bidderId, Id, amount, ++_bidSequence);
        _bids.Add(bid);
        SetUpdatedAt();
    }

    /// <summary>
    /// Returns all bids (including potentially invalid ones).
    /// Use GetValidBids() for filtered list.
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
    /// </summary>
    public List<Bid> GetValidBids()
    {
        List<Bid> validBids = new();
        decimal currentHighest = StartingBid;

        foreach (Bid bid in _bids.OrderBy(b => b.Sequence))
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
    /// </summary>
    public bool WouldBidBeValid(decimal amount)
    {
        return amount > GetHighestBidAmount();
    }
}

