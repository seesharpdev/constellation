namespace DistributedCarAuction.Application.DTOs;

public record BidResult(
    bool Success,
    string Message,
    Guid? BidId = null,
    decimal? CurrentHighestBid = null,
    bool? IsCurrentlyHighest = null  // High-availability feedback: was bid valid at time of placement?
);

