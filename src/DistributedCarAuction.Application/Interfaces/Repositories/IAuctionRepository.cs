namespace DistributedCarAuction.Application.Interfaces.Repositories;

using DistributedCarAuction.Domain.Entities;

public interface IAuctionRepository
{
    Task<Auction> AddAsync(Auction auction);

    Task<Auction?> GetByIdAsync(Guid id);

    Task<List<Auction>> GetAllAsync();

    Task UpdateAsync(Auction auction);
}