namespace DistributedCarAuction.Domain.Entities;

using DistributedCarAuction.Domain.Common;

public class Lot : BaseEntity
{
    public Guid AuctionId { get; set; }

    public Vehicle Vehicle { get; set; }

    public decimal StartingBid { get; set; }

    public decimal? ReservePrice { get; set; }

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
    /// NOTE: This implementation is suitable for single-instance deployments.
    /// For distributed deployments, implement optimistic concurrency with version numbers
    /// or use database-level unique constraints on (LotId, Amount, Sequence).
    /// </summary>
    public void PlaceBid(Guid bidderId, decimal amount)
    {
		decimal currentHighestBid = GetHighestBidAmount();
        
        if (amount <= currentHighestBid)
        {
            throw new InvalidOperationException(
                $"Bid amount must be greater than current highest bid of {currentHighestBid}");
        }

		Bid bid = new(bidderId, Id, amount, ++_bidSequence);
        _bids.Add(bid);
        SetUpdatedAt();
    }

    public decimal GetHighestBidAmount()
    {
        if (_bids.Count == 0)
            return StartingBid;

        return _bids.Max(b => b.Amount);
    }

    public Bid? GetHighestBid()
    {
        if (_bids.Count == 0)
            return null;

        // Order by amount descending, then by sequence for tie-breaking
        return _bids.OrderByDescending(b => b.Amount)
                    .ThenByDescending(b => b.Sequence)
                    .FirstOrDefault();
    }

    public Guid? GetWinningBidderId()
    {
		Bid? highestBid = GetHighestBid();
        
        // Check if reserve price is met (if set)
        if (ReservePrice.HasValue && (highestBid?.Amount ?? 0) < ReservePrice.Value)
            return null;

        return highestBid?.BidderId;
    }
}

