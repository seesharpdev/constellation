namespace DistributedCarAuction.Infrastructure.Persistence;

using DistributedCarAuction.Application.Interfaces.Repositories;
using DistributedCarAuction.Domain.Entities;
using DistributedCarAuction.Domain.Exceptions;
using System.Collections.Concurrent;

public class InMemoryLotRepository : ILotRepository
{
    private readonly ConcurrentDictionary<Guid, Lot> _lots = new();
    
    /// <summary>
    /// Tracks the stored version for each entity, separate from the entity object.
    /// This simulates database-level version tracking for optimistic concurrency.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, int> _storedVersions = new();
    private readonly object _updateLock = new();

    public Task<Lot> AddAsync(Lot lot)
    {
        ArgumentNullException.ThrowIfNull(lot);

        if (!_lots.TryAdd(lot.Id, lot))
            throw new InvalidOperationException($"Lot with ID {lot.Id} already exists");

        // Store the initial version
        _storedVersions[lot.Id] = lot.Version;

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

    /// <summary>
    /// Updates a lot with optimistic concurrency check.
    /// The entity's version must be exactly one greater than the stored version.
    /// Throws ConcurrencyException if another update occurred since the entity was read.
    /// </summary>
    public Task UpdateAsync(Lot lot)
    {
        ArgumentNullException.ThrowIfNull(lot);

        lock (_updateLock)
        {
            if (!_storedVersions.TryGetValue(lot.Id, out int storedVersion))
            {
                throw new InvalidOperationException($"Lot with ID {lot.Id} not found");
            }

            // The entity's version should be storedVersion + 1 after modification
            int expectedVersion = storedVersion + 1;
            
            if (lot.Version != expectedVersion)
            {
                throw new ConcurrencyException(
                    nameof(Lot),
                    lot.Id,
                    expectedVersion,
                    lot.Version);
            }

            // Update successful - store new version
            _lots[lot.Id] = lot;
            _storedVersions[lot.Id] = lot.Version;
        }

        return Task.CompletedTask;
    }
}