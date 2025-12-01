namespace DistributedCarAuction.Application.DTOs;

public record CreateLotRequest(
    Guid AuctionId,
    Guid VehicleId,
    decimal StartingBid,
    decimal? ReservePrice = null
);

