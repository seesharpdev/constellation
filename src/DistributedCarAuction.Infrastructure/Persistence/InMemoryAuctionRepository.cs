namespace DistributedCarAuction.Infrastructure.Persistence;

using DistributedCarAuction.Application.Interfaces.Repositories;
using DistributedCarAuction.Domain.Entities;
using System.Collections.Concurrent;

public class InMemoryAuctionRepository : IAuctionRepository
{
    private readonly ConcurrentDictionary<Guid, Auction> _auctions = new();

    public Task<Auction> AddAsync(Auction auction)
    {
		ArgumentNullException.ThrowIfNull(auction);

		if (!_auctions.TryAdd(auction.Id, auction))
            throw new InvalidOperationException($"Auction with ID {auction.Id} already exists");
            
        return Task.FromResult(auction);
    }

    public Task<Auction?> GetByIdAsync(Guid id)
    {
        _auctions.TryGetValue(id, out var auction);

        return Task.FromResult(auction);
    }

    public Task<List<Auction>> GetAllAsync()
    {
        return Task.FromResult(_auctions.Values.ToList());
    }

    public Task UpdateAsync(Auction auction)
    {
        _auctions[auction.Id] = auction;

        return Task.CompletedTask;
    }
}