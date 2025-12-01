namespace DistributedCarAuction.Application.Interfaces.Repositories;

using DistributedCarAuction.Domain.Entities;

public interface ILotRepository
{
    Task<Lot> AddAsync(Lot lot);

    Task<Lot?> GetByIdAsync(Guid id);

    Task<List<Lot>> GetByAuctionIdAsync(Guid auctionId);

    Task UpdateAsync(Lot lot);
}