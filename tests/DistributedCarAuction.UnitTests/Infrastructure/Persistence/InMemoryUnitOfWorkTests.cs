namespace DistributedCarAuction.UnitTests.Infrastructure.Persistence;

using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Domain.Entities;
using DistributedCarAuction.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

public class InMemoryUnitOfWorkTests
{
    private readonly InMemoryAuctionRepository _auctionStore;
    private readonly InMemoryLotRepository _lotStore;
    private readonly InMemoryVehicleRepository _vehicleStore;
    private readonly InMemoryUnitOfWorkFactory _factory;

    public InMemoryUnitOfWorkTests()
    {
        _auctionStore = new InMemoryAuctionRepository();
        _lotStore = new InMemoryLotRepository();
        _vehicleStore = new InMemoryVehicleRepository();
        _factory = new InMemoryUnitOfWorkFactory(_auctionStore, _lotStore, _vehicleStore);
    }

    #region Basic Operations Tests

    [Fact]
    public async Task CommitAsync_WithNoChanges_ReturnsZero()
    {
        // Arrange
        await using IUnitOfWork uow = _factory.Create();

        // Act
        int affected = await uow.CommitAsync();

        // Assert
        affected.Should().Be(0);
    }

    [Fact]
    public async Task CommitAsync_WithAddedAuction_PersistsToStore()
    {
        // Arrange
        Auction auction = new("Test Auction", "Description");

        await using IUnitOfWork uow = _factory.Create();
        await uow.Auctions.AddAsync(auction);

        // Act
        int affected = await uow.CommitAsync();

        // Assert
        affected.Should().Be(1);
        Auction? persisted = await _auctionStore.GetByIdAsync(auction.Id);
        persisted.Should().NotBeNull();
        persisted!.Title.Should().Be("Test Auction");
    }

    [Fact]
    public async Task CommitAsync_WithAddedLot_PersistsToStore()
    {
        // Arrange
        Auction auction = new("Test Auction", "Description");
        await _auctionStore.AddAsync(auction);

        Sedan vehicle = new("Toyota", "Camry", 2020, "VIN123", 20000m, "Blue", 4, false);
        Lot lot = new(auction.Id, vehicle, 5000m);

        await using IUnitOfWork uow = _factory.Create();
        await uow.Lots.AddAsync(lot);

        // Act
        int affected = await uow.CommitAsync();

        // Assert
        affected.Should().Be(1);
        Lot? persisted = await _lotStore.GetByIdAsync(lot.Id);
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task CommitAsync_WithMultipleChanges_AllPersisted()
    {
        // Arrange
        Auction auction = new("Test Auction", "Description");
        Sedan vehicle = new("Toyota", "Camry", 2020, "VIN123", 20000m, "Blue", 4, false);
        Lot lot = new(auction.Id, vehicle, 5000m);

        await using IUnitOfWork uow = _factory.Create();
        await uow.Auctions.AddAsync(auction);
        await uow.Lots.AddAsync(lot);

        // Act
        int affected = await uow.CommitAsync();

        // Assert
        affected.Should().Be(2);
        (await _auctionStore.GetByIdAsync(auction.Id)).Should().NotBeNull();
        (await _lotStore.GetByIdAsync(lot.Id)).Should().NotBeNull();
    }

    #endregion

    #region Rollback Tests

    [Fact]
    public async Task Rollback_DiscardsUncommittedChanges()
    {
        // Arrange
        Auction auction = new("Test Auction", "Description");

        await using IUnitOfWork uow = _factory.Create();
        await uow.Auctions.AddAsync(auction);
        uow.HasPendingChanges.Should().BeTrue();

        // Act
        uow.Rollback();

        // Assert
        uow.HasPendingChanges.Should().BeFalse();
        (await _auctionStore.GetByIdAsync(auction.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Dispose_DiscardsUncommittedChanges()
    {
        // Arrange
        Auction auction = new("Test Auction", "Description");

        IUnitOfWork uow = _factory.Create();
        await uow.Auctions.AddAsync(auction);

        // Act
        uow.Dispose();

        // Assert
        (await _auctionStore.GetByIdAsync(auction.Id)).Should().BeNull();
    }

    #endregion

    #region HasPendingChanges Tests

    [Fact]
    public void HasPendingChanges_InitialState_IsFalse()
    {
        // Arrange
        using IUnitOfWork uow = _factory.Create();

        // Assert
        uow.HasPendingChanges.Should().BeFalse();
    }

    [Fact]
    public async Task HasPendingChanges_AfterAdd_IsTrue()
    {
        // Arrange
        await using IUnitOfWork uow = _factory.Create();
        Auction auction = new("Test Auction", "Description");

        // Act
        await uow.Auctions.AddAsync(auction);

        // Assert
        uow.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task HasPendingChanges_AfterCommit_IsFalse()
    {
        // Arrange
        await using IUnitOfWork uow = _factory.Create();
        Auction auction = new("Test Auction", "Description");
        await uow.Auctions.AddAsync(auction);

        // Act
        await uow.CommitAsync();

        // Assert
        uow.HasPendingChanges.Should().BeFalse();
    }

    #endregion

    #region Transactional Atomicity Tests

    [Fact]
    public async Task CommitAsync_SuccessfulUpdate_IncrementsVersion()
    {
        // Arrange - Create and persist an auction with a lot
        Auction auction = new("Test Auction", "Description");
        Sedan vehicle = new("Toyota", "Camry", 2020, "VIN123", 20000m, "Blue", 4, false);
        Lot lot = new(auction.Id, vehicle, 5000m);
        auction.AddLot(lot);
        await _auctionStore.AddAsync(auction);
        await _lotStore.AddAsync(lot);

        int initialVersion = auction.Version;

        // Start a UoW and load the auction
        await using IUnitOfWork uow = _factory.Create();
        Auction? loadedAuction = await uow.Auctions.GetByIdAsync(auction.Id);

        // Modify through UoW
        loadedAuction!.Start();
        await uow.Auctions.UpdateAsync(loadedAuction);

        // Act
        int affected = await uow.CommitAsync();

        // Assert
        affected.Should().Be(1);
        loadedAuction.Version.Should().BeGreaterThan(initialVersion);
    }

    [Fact]
    public async Task CommitAsync_MultipleEntities_AllCommitted()
    {
        // Arrange
        Auction auction = new("Test Auction", "Description");
        Sedan vehicle1 = new("Toyota", "Camry", 2020, "VIN123", 20000m, "Blue", 4, false);
        Sedan vehicle2 = new("Honda", "Accord", 2021, "VIN456", 22000m, "Red", 4, false);
        
        await using IUnitOfWork uow = _factory.Create();
        
        await uow.Auctions.AddAsync(auction);
        await uow.Vehicles.AddAsync(vehicle1);
        await uow.Vehicles.AddAsync(vehicle2);

        // Act
        int affected = await uow.CommitAsync();

        // Assert
        affected.Should().Be(3);
        (await _auctionStore.GetByIdAsync(auction.Id)).Should().NotBeNull();
        (await _vehicleStore.GetByIdAsync(vehicle1.Id)).Should().NotBeNull();
        (await _vehicleStore.GetByIdAsync(vehicle2.Id)).Should().NotBeNull();
    }

    #endregion

    #region Read Through Tests

    [Fact]
    public async Task GetByIdAsync_ReadsFromStore()
    {
        // Arrange
        Auction auction = new("Test Auction", "Description");
        await _auctionStore.AddAsync(auction);

        await using IUnitOfWork uow = _factory.Create();

        // Act
        Auction? loaded = await uow.Auctions.GetByIdAsync(auction.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Title.Should().Be("Test Auction");
    }

    [Fact]
    public async Task GetAllAsync_ReadsFromStore()
    {
        // Arrange
        await _auctionStore.AddAsync(new Auction("Auction 1", "Desc 1"));
        await _auctionStore.AddAsync(new Auction("Auction 2", "Desc 2"));

        await using IUnitOfWork uow = _factory.Create();

        // Act
        List<Auction> auctions = await uow.Auctions.GetAllAsync();

        // Assert
        auctions.Should().HaveCount(2);
    }

    #endregion

    #region Factory Tests

    [Fact]
    public void Create_ReturnsNewInstance()
    {
        // Act
        IUnitOfWork uow1 = _factory.Create();
        IUnitOfWork uow2 = _factory.Create();

        // Assert
        uow1.Should().NotBeSameAs(uow2);

        uow1.Dispose();
        uow2.Dispose();
    }

    #endregion
}

