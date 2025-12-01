namespace DistributedCarAuction.Application.DTOs;

public record BidResult(
    bool Success,
    string Message,
    Guid? BidId = null,
    decimal? CurrentHighestBid = null
);

