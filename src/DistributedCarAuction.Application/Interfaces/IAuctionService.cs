namespace DistributedCarAuction.Application.Interfaces;

using DistributedCarAuction.Application.DTOs;
using DistributedCarAuction.Domain.Entities;

public interface IAuctionService
{
    Task<Auction> CreateAuctionAsync(CreateAuctionRequest request);

    Task StartAuctionAsync(Guid auctionId);

    Task CloseAuctionAsync(Guid auctionId);

    Task<Auction?> GetByIdAsync(Guid auctionId);

    Task<List<Auction>> GetAllAsync();
}

