namespace DistributedCarAuction.Application.Interfaces;

using DistributedCarAuction.Application.DTOs;
using DistributedCarAuction.Domain.Entities;

public interface ILotService
{
    Task<Lot> CreateLotAsync(CreateLotRequest request);

    Task<BidResult> PlaceBidAsync(BidRequest request);

    Task<decimal> GetHighestBidAsync(Guid lotId);

    Task<Guid?> GetWinnerAsync(Guid lotId);

    Task<Lot?> GetByIdAsync(Guid lotId);
}