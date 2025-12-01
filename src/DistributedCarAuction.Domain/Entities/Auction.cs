namespace DistributedCarAuction.Domain.Entities;

using DistributedCarAuction.Domain.Common;
using DistributedCarAuction.Domain.Enums;

public class Auction : BaseEntity
{
    public string Title { get; set; }

    public string Description { get; set; }

    public AuctionState State { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    private readonly List<Lot> _lots = new();

    public IReadOnlyList<Lot> Lots => _lots.AsReadOnly();

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

    public void AddLot(Lot lot)
    {
        if (State != AuctionState.Created)
            throw new InvalidOperationException("Lots can only be added to auctions in Created state");

		ArgumentNullException.ThrowIfNull(lot);

		_lots.Add(lot);
        SetUpdatedAt();
    }

    public void Start()
    {
        if (State != AuctionState.Created)
            throw new InvalidOperationException($"Cannot start auction from {State} state");

        if (_lots.Count == 0)
            throw new InvalidOperationException("Cannot start auction without lots");

        State = AuctionState.Active;
        StartTime = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void End()
    {
        if (State != AuctionState.Active)
            throw new InvalidOperationException($"Cannot end auction from {State} state");

        State = AuctionState.Ended;
        EndTime = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public bool CanAcceptBids()
    {
        return State == AuctionState.Active;
    }

    public Lot? GetLot(Guid lotId)
    {
        return _lots.FirstOrDefault(l => l.Id == lotId);
    }
}

