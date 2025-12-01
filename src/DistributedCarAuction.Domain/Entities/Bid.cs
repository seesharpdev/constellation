namespace DistributedCarAuction.Domain.Entities;

using DistributedCarAuction.Domain.Common;

public class Bid : BaseEntity
{
    public Guid BidderId { get; init; }

    public Guid LotId { get; init; }

    public decimal Amount { get; init; }

    public DateTime BidTime { get; init; }

    public long Sequence { get; init; } // For bid ordering in distributed system

    private Bid() { }

    public Bid(Guid bidderId, Guid lotId, decimal amount, long sequence)
    {
        if (bidderId == Guid.Empty)
            throw new ArgumentException("Bidder ID cannot be empty", nameof(bidderId));
        
        if (lotId == Guid.Empty)
            throw new ArgumentException("Lot ID cannot be empty", nameof(lotId));
        
        if (amount <= 0)
            throw new ArgumentException("Bid amount must be greater than zero", nameof(amount));

        BidderId = bidderId;
        LotId = lotId;
        Amount = amount;
        BidTime = DateTime.UtcNow;
        Sequence = sequence;
    }
}

