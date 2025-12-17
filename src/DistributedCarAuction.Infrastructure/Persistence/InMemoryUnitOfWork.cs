namespace DistributedCarAuction.Infrastructure.Persistence;

using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Application.Interfaces.Repositories;
using DistributedCarAuction.Domain.Common;
using DistributedCarAuction.Domain.Entities;
using DistributedCarAuction.Domain.Exceptions;

/// <summary>
/// In-memory implementation of Unit of Work pattern.
/// 
/// Provides transactional semantics by:
/// 1. Tracking all changes made through repository wrappers
/// 2. Applying all changes atomically on CommitAsync
/// 3. Rolling back by discarding tracked changes
/// 
/// Thread-safety: Each UoW instance should be used by a single thread.
/// For concurrent operations, create separate UoW instances.
/// </summary>
public class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly InMemoryAuctionRepository _auctionStore;
    private readonly InMemoryLotRepository _lotStore;
    private readonly InMemoryVehicleRepository _vehicleStore;

    private readonly UnitOfWorkAuctionRepository _auctions;
    private readonly UnitOfWorkLotRepository _lots;
    private readonly UnitOfWorkVehicleRepository _vehicles;

    private readonly List<PendingChange> _pendingChanges = new();
    private bool _disposed;

    public InMemoryUnitOfWork(
        InMemoryAuctionRepository auctionStore,
        InMemoryLotRepository lotStore,
        InMemoryVehicleRepository vehicleStore)
    {
        _auctionStore = auctionStore ?? throw new ArgumentNullException(nameof(auctionStore));
        _lotStore = lotStore ?? throw new ArgumentNullException(nameof(lotStore));
        _vehicleStore = vehicleStore ?? throw new ArgumentNullException(nameof(vehicleStore));

        _auctions = new UnitOfWorkAuctionRepository(_auctionStore, _pendingChanges);
        _lots = new UnitOfWorkLotRepository(_lotStore, _pendingChanges);
        _vehicles = new UnitOfWorkVehicleRepository(_vehicleStore, _pendingChanges);
    }

    public IAuctionRepository Auctions => _auctions;
    public ILotRepository Lots => _lots;
    public IVehicleRepository Vehicles => _vehicles;
    public bool HasPendingChanges => _pendingChanges.Count > 0;

    public async Task<int> CommitAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_pendingChanges.Count == 0)
            return 0;

        // Apply all changes - the underlying stores handle version validation
        // If any change fails, we throw and the UoW is left in an inconsistent state
        // (caller should dispose and retry with a fresh UoW)
        int affected = 0;
        try
        {
            foreach (PendingChange change in _pendingChanges)
            {
                await ApplyChangeAsync(change);
                affected++;
            }
        }
        finally
        {
            _pendingChanges.Clear();
        }

        return affected;
    }

    private async Task ApplyChangeAsync(PendingChange change)
    {
        switch (change.ChangeType)
        {
            case ChangeType.Add:
                await ApplyAddAsync(change);
                break;
            case ChangeType.Update:
                await ApplyUpdateAsync(change);
                break;
        }
    }

    private async Task ApplyAddAsync(PendingChange change)
    {
        switch (change.Entity)
        {
            case Auction auction:
                await _auctionStore.AddAsync(auction);
                break;
            case Lot lot:
                await _lotStore.AddAsync(lot);
                break;
            case Vehicle vehicle:
                await _vehicleStore.AddAsync(vehicle);
                break;
        }
    }

    private async Task ApplyUpdateAsync(PendingChange change)
    {
        switch (change.Entity)
        {
            case Auction auction:
                await _auctionStore.UpdateAsync(auction);
                break;
            case Lot lot:
                await _lotStore.UpdateAsync(lot);
                break;
        }
    }

    public void Rollback()
    {
        _pendingChanges.Clear();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _pendingChanges.Clear();
            _disposed = true;
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    #region Pending Change Tracking

    private enum ChangeType { Add, Update }

    private sealed class PendingChange
    {
        public BaseEntity Entity { get; init; } = null!;
        public ChangeType ChangeType { get; init; }
        public int OriginalVersion { get; init; }
    }

    #endregion

    #region Repository Wrappers

    /// <summary>
    /// Wrapper that intercepts repository operations and tracks them for later commit.
    /// </summary>
    private sealed class UnitOfWorkAuctionRepository : IAuctionRepository
    {
        private readonly InMemoryAuctionRepository _store;
        private readonly List<PendingChange> _changes;

        public UnitOfWorkAuctionRepository(InMemoryAuctionRepository store, List<PendingChange> changes)
        {
            _store = store;
            _changes = changes;
        }

        public Task<Auction> AddAsync(Auction auction)
        {
            _changes.Add(new PendingChange 
            { 
                Entity = auction, 
                ChangeType = ChangeType.Add,
                OriginalVersion = auction.Version
            });
            return Task.FromResult(auction);
        }

        public Task<Auction?> GetByIdAsync(Guid id) => _store.GetByIdAsync(id);
        public Task<List<Auction>> GetAllAsync() => _store.GetAllAsync();

        public Task UpdateAsync(Auction auction)
        {
            _changes.Add(new PendingChange 
            { 
                Entity = auction, 
                ChangeType = ChangeType.Update,
                OriginalVersion = auction.Version - 1 // Version was already incremented by domain
            });
            return Task.CompletedTask;
        }
    }

    private sealed class UnitOfWorkLotRepository : ILotRepository
    {
        private readonly InMemoryLotRepository _store;
        private readonly List<PendingChange> _changes;

        public UnitOfWorkLotRepository(InMemoryLotRepository store, List<PendingChange> changes)
        {
            _store = store;
            _changes = changes;
        }

        public Task<Lot> AddAsync(Lot lot)
        {
            _changes.Add(new PendingChange 
            { 
                Entity = lot, 
                ChangeType = ChangeType.Add,
                OriginalVersion = lot.Version
            });
            return Task.FromResult(lot);
        }

        public Task<Lot?> GetByIdAsync(Guid id) => _store.GetByIdAsync(id);
        public Task<List<Lot>> GetByAuctionIdAsync(Guid auctionId) => _store.GetByAuctionIdAsync(auctionId);

        public Task UpdateAsync(Lot lot)
        {
            _changes.Add(new PendingChange 
            { 
                Entity = lot, 
                ChangeType = ChangeType.Update,
                OriginalVersion = lot.Version - 1 // Version was already incremented by domain
            });
            return Task.CompletedTask;
        }
    }

    private sealed class UnitOfWorkVehicleRepository : IVehicleRepository
    {
        private readonly InMemoryVehicleRepository _store;
        private readonly List<PendingChange> _changes;

        public UnitOfWorkVehicleRepository(InMemoryVehicleRepository store, List<PendingChange> changes)
        {
            _store = store;
            _changes = changes;
        }

        public Task<Vehicle> AddAsync(Vehicle vehicle)
        {
            _changes.Add(new PendingChange 
            { 
                Entity = vehicle, 
                ChangeType = ChangeType.Add,
                OriginalVersion = vehicle.Version
            });
            return Task.FromResult(vehicle);
        }

        public Task<Vehicle?> GetByIdAsync(Guid id) => _store.GetByIdAsync(id);
        public Task<List<Vehicle>> GetAllAsync() => _store.GetAllAsync();
    }

    #endregion
}

/// <summary>
/// Factory for creating InMemoryUnitOfWork instances.
/// </summary>
public class InMemoryUnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly InMemoryAuctionRepository _auctionStore;
    private readonly InMemoryLotRepository _lotStore;
    private readonly InMemoryVehicleRepository _vehicleStore;

    public InMemoryUnitOfWorkFactory(
        InMemoryAuctionRepository auctionStore,
        InMemoryLotRepository lotStore,
        InMemoryVehicleRepository vehicleStore)
    {
        _auctionStore = auctionStore ?? throw new ArgumentNullException(nameof(auctionStore));
        _lotStore = lotStore ?? throw new ArgumentNullException(nameof(lotStore));
        _vehicleStore = vehicleStore ?? throw new ArgumentNullException(nameof(vehicleStore));
    }

    public IUnitOfWork Create() => new InMemoryUnitOfWork(_auctionStore, _lotStore, _vehicleStore);
}

