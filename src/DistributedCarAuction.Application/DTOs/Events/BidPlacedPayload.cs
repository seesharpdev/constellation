namespace DistributedCarAuction.Application.DTOs.Events;

/// <summary>
/// Payload for BidPlaced events.
/// </summary>
public record BidPlacedPayload(
    Guid LotId,
    Guid BidderId,
    decimal Amount,
    long Sequence,
    bool IsCurrentlyHighest,
    decimal CurrentHighestBid
);

