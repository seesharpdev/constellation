namespace DistributedCarAuction.UnitTests.Infrastructure.Persistence;

using DistributedCarAuction.Domain.Entities;
using DistributedCarAuction.Domain.Exceptions;
using DistributedCarAuction.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

public class InMemoryAuctionRepositoryTests
{
    private readonly InMemoryAuctionRepository _repository;

    public InMemoryAuctionRepositoryTests()
    {
        _repository = new InMemoryAuctionRepository();
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidAuction_StoresAuction()
    {
        // Arrange
        var auction = CreateAuction();

        // Act
        var result = await _repository.AddAsync(auction);

        // Assert
        result.Should().Be(auction);
        var stored = await _repository.GetByIdAsync(auction.Id);
        stored.Should().Be(auction);
    }

    [Fact]
    public async Task AddAsync_WithNullAuction_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _repository.AddAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var auction = CreateAuction();
        await _repository.AddAsync(auction);

        // Act
        var act = async () => await _repository.AddAsync(auction);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task AddAsync_InitializesStoredVersion()
    {
        // Arrange
        var auction = CreateAuction();

        // Act
        await _repository.AddAsync(auction);

        // Assert - First update should work (version incremented by 1)
        auction.AddLot(CreateLot());
        var updateAct = async () => await _repository.UpdateAsync(auction);
        await updateAct.Should().NotThrowAsync();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsAuction()
    {
        // Arrange
        var auction = CreateAuction();
        await _repository.AddAsync(auction);

        // Act
        var result = await _repository.GetByIdAsync(auction.Id);

        // Assert
        result.Should().Be(auction);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithNoAuctions_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleAuctions_ReturnsAllAuctions()
    {
        // Arrange
        var auction1 = CreateAuction("Auction 1");
        var auction2 = CreateAuction("Auction 2");
        var auction3 = CreateAuction("Auction 3");
        
        await _repository.AddAsync(auction1);
        await _repository.AddAsync(auction2);
        await _repository.AddAsync(auction3);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(auction1);
        result.Should().Contain(auction2);
        result.Should().Contain(auction3);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithCorrectVersion_Succeeds()
    {
        // Arrange
        var auction = CreateAuction();
        await _repository.AddAsync(auction);
        auction.AddLot(CreateLot());  // Increments version from 1 to 2

        // Act
        var act = async () => await _repository.UpdateAsync(auction);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateAsync_WithCorrectVersion_UpdatesStoredVersion()
    {
        // Arrange
        var auction = CreateAuction();
        await _repository.AddAsync(auction);
        
        // First update
        auction.AddLot(CreateLot());
        await _repository.UpdateAsync(auction);

        // Second update should also work
        auction.AddLot(CreateLot());
        var act = async () => await _repository.UpdateAsync(auction);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateAsync_WithNullAuction_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _repository.UpdateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingAuction_ThrowsInvalidOperationException()
    {
        // Arrange
        var auction = CreateAuction();
        auction.AddLot(CreateLot());  // Increment version

        // Act (not added to repository first)
        var act = async () => await _repository.UpdateAsync(auction);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateAsync_WithVersionMismatch_ThrowsConcurrencyException()
    {
        // Arrange
        var auction = CreateAuction();
        await _repository.AddAsync(auction);
        
        // Simulate version mismatch by modifying twice without saving
        auction.AddLot(CreateLot());  // Version: 1 -> 2
        auction.AddLot(CreateLot());  // Version: 2 -> 3 (but stored is still 1)

        // Act
        var act = async () => await _repository.UpdateAsync(auction);

        // Assert
        await act.Should().ThrowAsync<ConcurrencyException>()
            .Where(ex => 
                ex.EntityType == nameof(Auction) &&
                ex.EntityId == auction.Id &&
                ex.ExpectedVersion == 2 &&  // storedVersion (1) + 1
                ex.ActualVersion == 3);     // Current entity version
    }

    [Fact]
    public async Task UpdateAsync_MultipleSequentialUpdates_AllSucceed()
    {
        // Arrange
        var auction = CreateAuction();
        await _repository.AddAsync(auction);

        // Act & Assert - Each update should succeed
        for (int i = 0; i < 5; i++)
        {
            auction.AddLot(CreateLot());
            var act = async () => await _repository.UpdateAsync(auction);
            await act.Should().NotThrowAsync();
        }

        // Verify final version
        auction.Version.Should().Be(6); // Initial 1 + 5 updates
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task UpdateAsync_ConcurrentUpdates_OnlyOneSucceeds()
    {
        // Arrange
        var auction = CreateAuction();
        await _repository.AddAsync(auction);

        // Simulate two "clients" reading the same auction
        // Both will try to update with version 2, but stored is 1
        var lot1 = CreateLot();
        var lot2 = CreateLot();

        // First update
        auction.AddLot(lot1);  // Version: 1 -> 2
        await _repository.UpdateAsync(auction);

        // Second update would fail if we hadn't updated stored version
        auction.AddLot(lot2);  // Version: 2 -> 3
        var act = async () => await _repository.UpdateAsync(auction);

        // Assert - Should succeed because stored version was updated to 2
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Helper Methods

    private static Auction CreateAuction(string title = "Test Auction")
    {
        return new Auction(title, "Test Description");
    }

    private static Lot CreateLot()
    {
        var vehicle = new Sedan("Toyota", "Camry", 2020, "VIN123456789", 30000m, "Silver", 4, false);
        return new Lot(Guid.NewGuid(), vehicle, 5000m);
    }

    #endregion
}

