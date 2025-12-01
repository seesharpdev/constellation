namespace DistributedCarAuction.Domain.Entities;

using DistributedCarAuction.Domain.Common;

public class Bid : BaseEntity
{
    public Guid BidderId { get; set; }

    public Guid LotId { get; set; }

    public decimal Amount { get; set; }

    public DateTime BidTime { get; set; }

    public long Sequence { get; set; } // For bid ordering in distributed system

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

