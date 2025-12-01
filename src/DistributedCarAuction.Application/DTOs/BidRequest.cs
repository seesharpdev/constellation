namespace DistributedCarAuction.Application.DTOs;

public record BidRequest(
    Guid LotId,
    Guid BidderId,
    decimal Amount
);

