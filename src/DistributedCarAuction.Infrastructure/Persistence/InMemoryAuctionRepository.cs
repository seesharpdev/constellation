namespace DistributedCarAuction.Infrastructure.Persistence;

using DistributedCarAuction.Application.Interfaces.Repositories;
using DistributedCarAuction.Domain.Entities;
using DistributedCarAuction.Domain.Exceptions;
using System.Collections.Concurrent;

public class InMemoryAuctionRepository : IAuctionRepository
{
    private readonly ConcurrentDictionary<Guid, Auction> _auctions = new();
    
    /// <summary>
    /// Tracks the stored version for each entity, separate from the entity object.
    /// This simulates database-level version tracking for optimistic concurrency.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, int> _storedVersions = new();
    private readonly object _updateLock = new();

    public Task<Auction> AddAsync(Auction auction)
    {
        ArgumentNullException.ThrowIfNull(auction);

        if (!_auctions.TryAdd(auction.Id, auction))
            throw new InvalidOperationException($"Auction with ID {auction.Id} already exists");
        
        // Store the initial version
        _storedVersions[auction.Id] = auction.Version;
            
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

    /// <summary>
    /// Updates an auction with optimistic concurrency check.
    /// The entity's version must be exactly one greater than the stored version.
    /// Throws ConcurrencyException if another update occurred since the entity was read.
    /// </summary>
    public Task UpdateAsync(Auction auction)
    {
        ArgumentNullException.ThrowIfNull(auction);

        lock (_updateLock)
        {
            if (!_storedVersions.TryGetValue(auction.Id, out int storedVersion))
            {
                throw new InvalidOperationException($"Auction with ID {auction.Id} not found");
            }

            // The entity's version should be storedVersion + 1 after modification
            // If it's higher, multiple modifications happened; if different, someone else updated
            int expectedVersion = storedVersion + 1;
            
            if (auction.Version != expectedVersion)
            {
                throw new ConcurrencyException(
                    nameof(Auction),
                    auction.Id,
                    expectedVersion,
                    auction.Version);
            }

            // Update successful - store new version
            _auctions[auction.Id] = auction;
            _storedVersions[auction.Id] = auction.Version;
        }

        return Task.CompletedTask;
    }
}