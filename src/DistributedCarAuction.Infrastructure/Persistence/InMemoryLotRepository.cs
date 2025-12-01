namespace DistributedCarAuction.Infrastructure.Persistence;

using DistributedCarAuction.Application.Interfaces.Repositories;
using DistributedCarAuction.Domain.Entities;
using System.Collections.Concurrent;

public class InMemoryLotRepository : ILotRepository
{
    private readonly ConcurrentDictionary<Guid, Lot> _lots = new();

    public Task<Lot> AddAsync(Lot lot)
    {
        if (!_lots.TryAdd(lot.Id, lot))
            throw new InvalidOperationException($"Lot with ID {lot.Id} already exists");

        return Task.FromResult(lot);
    }

    public Task<Lot?> GetByIdAsync(Guid id)
    {
        _lots.TryGetValue(id, out var lot);

        return Task.FromResult(lot);
    }

    public Task<List<Lot>> GetByAuctionIdAsync(Guid auctionId)
    {
		List<Lot> lots = [.. _lots.Values.Where(l => l.AuctionId == auctionId)];

        return Task.FromResult(lots);
    }

    public Task UpdateAsync(Lot lot)
    {
        _lots[lot.Id] = lot;

        return Task.CompletedTask;
    }
}