namespace DistributedCarAuction.Application.DTOs.Events;

/// <summary>
/// Payload for AuctionCreated, AuctionStarted, AuctionEnded events.
/// </summary>
public record AuctionStatePayload(
    string Title,
    string Description,
    string State,
    int LotCount
);

