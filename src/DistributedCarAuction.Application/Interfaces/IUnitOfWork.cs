namespace DistributedCarAuction.Application.Interfaces;

using DistributedCarAuction.Application.Interfaces.Repositories;

/// <summary>
/// Unit of Work pattern for coordinating transactional operations across repositories.
/// 
/// Provides:
/// - Atomic commits: All changes succeed or all fail
/// - Change tracking: Entities are tracked until commit
/// - Rollback support: Discard all uncommitted changes
/// 
/// Usage:
/// <code>
/// await using var uow = unitOfWorkFactory.Create();
/// var auction = await uow.Auctions.GetByIdAsync(auctionId);
/// auction.Start();
/// var lot = new Lot(auctionId, vehicle, startingBid);
/// await uow.Lots.AddAsync(lot);
/// await uow.CommitAsync(); // Both changes applied atomically
/// </code>
/// </summary>
public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Repository for auction operations within this unit of work.
    /// </summary>
    IAuctionRepository Auctions { get; }

    /// <summary>
    /// Repository for lot operations within this unit of work.
    /// </summary>
    ILotRepository Lots { get; }

    /// <summary>
    /// Repository for vehicle operations within this unit of work.
    /// </summary>
    IVehicleRepository Vehicles { get; }

    /// <summary>
    /// Commits all pending changes atomically.
    /// Throws ConcurrencyException if any entity has been modified since it was loaded.
    /// </summary>
    /// <returns>Number of entities affected.</returns>
    Task<int> CommitAsync();

    /// <summary>
    /// Discards all pending changes and resets the unit of work.
    /// </summary>
    void Rollback();

    /// <summary>
    /// Gets whether there are any pending changes.
    /// </summary>
    bool HasPendingChanges { get; }
}

/// <summary>
/// Factory for creating Unit of Work instances.
/// Each operation that requires transactional semantics should create a new UoW.
/// </summary>
public interface IUnitOfWorkFactory
{
    /// <summary>
    /// Creates a new Unit of Work instance.
    /// </summary>
    IUnitOfWork Create();
}

