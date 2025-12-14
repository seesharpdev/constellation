namespace DistributedCarAuction.UnitTests.Infrastructure.Persistence;

using DistributedCarAuction.Domain.Entities;
using DistributedCarAuction.Domain.Exceptions;
using DistributedCarAuction.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

public class InMemoryLotRepositoryTests
{
    private readonly InMemoryLotRepository _repository;

    public InMemoryLotRepositoryTests()
    {
        _repository = new InMemoryLotRepository();
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidLot_StoresLot()
    {
        // Arrange
        var lot = CreateLot();

        // Act
        var result = await _repository.AddAsync(lot);

        // Assert
        result.Should().Be(lot);
        var stored = await _repository.GetByIdAsync(lot.Id);
        stored.Should().Be(lot);
    }

    [Fact]
    public async Task AddAsync_WithNullLot_ThrowsArgumentNullException()
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
        var lot = CreateLot();
        await _repository.AddAsync(lot);

        // Act
        var act = async () => await _repository.AddAsync(lot);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsLot()
    {
        // Arrange
        var lot = CreateLot();
        await _repository.AddAsync(lot);

        // Act
        var result = await _repository.GetByIdAsync(lot.Id);

        // Assert
        result.Should().Be(lot);
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

    #region GetByAuctionIdAsync Tests

    [Fact]
    public async Task GetByAuctionIdAsync_WithMatchingLots_ReturnsFilteredLots()
    {
        // Arrange
        var auctionId = Guid.NewGuid();
        var otherAuctionId = Guid.NewGuid();
        
        var lot1 = CreateLot(auctionId);
        var lot2 = CreateLot(auctionId);
        var lot3 = CreateLot(otherAuctionId);
        
        await _repository.AddAsync(lot1);
        await _repository.AddAsync(lot2);
        await _repository.AddAsync(lot3);

        // Act
        var result = await _repository.GetByAuctionIdAsync(auctionId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(lot1);
        result.Should().Contain(lot2);
        result.Should().NotContain(lot3);
    }

    [Fact]
    public async Task GetByAuctionIdAsync_WithNoMatchingLots_ReturnsEmptyList()
    {
        // Arrange
        var lot = CreateLot(Guid.NewGuid());
        await _repository.AddAsync(lot);

        // Act
        var result = await _repository.GetByAuctionIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithCorrectVersion_Succeeds()
    {
        // Arrange
        var lot = CreateLot();
        await _repository.AddAsync(lot);
        lot.PlaceBid(Guid.NewGuid(), 6000m);  // Increments version

        // Act
        var act = async () => await _repository.UpdateAsync(lot);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateAsync_WithNullLot_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _repository.UpdateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingLot_ThrowsInvalidOperationException()
    {
        // Arrange
        var lot = CreateLot();
        lot.PlaceBid(Guid.NewGuid(), 6000m);

        // Act (not added to repository first)
        var act = async () => await _repository.UpdateAsync(lot);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateAsync_WithVersionMismatch_ThrowsConcurrencyException()
    {
        // Arrange
        var lot = CreateLot();
        await _repository.AddAsync(lot);
        
        // Simulate version mismatch by modifying twice without saving
        lot.PlaceBid(Guid.NewGuid(), 6000m);  // Version: 1 -> 2
        lot.PlaceBid(Guid.NewGuid(), 7000m);  // Version: 2 -> 3

        // Act
        var act = async () => await _repository.UpdateAsync(lot);

        // Assert
        await act.Should().ThrowAsync<ConcurrencyException>()
            .Where(ex => 
                ex.EntityType == nameof(Lot) &&
                ex.EntityId == lot.Id &&
                ex.ExpectedVersion == 2 &&
                ex.ActualVersion == 3);
    }

    [Fact]
    public async Task UpdateAsync_MultipleSequentialUpdates_AllSucceed()
    {
        // Arrange
        var lot = CreateLot();
        await _repository.AddAsync(lot);

        // Act & Assert - Each update should succeed
        for (int i = 0; i < 10; i++)
        {
            lot.PlaceBid(Guid.NewGuid(), 5000m + (i * 1000));
            var act = async () => await _repository.UpdateAsync(lot);
            await act.Should().NotThrowAsync();
        }

        // Verify bids were recorded
        lot.Bids.Should().HaveCount(10);
    }

    #endregion

    #region Concurrency Exception Details Tests

    [Fact]
    public async Task UpdateAsync_ConcurrencyException_ContainsCorrectDetails()
    {
        // Arrange
        var lot = CreateLot();
        await _repository.AddAsync(lot);
        
        lot.PlaceBid(Guid.NewGuid(), 6000m);  // Version: 1 -> 2
        lot.PlaceBid(Guid.NewGuid(), 7000m);  // Version: 2 -> 3

        // Act & Assert
        ConcurrencyException? caughtException = null;
        try
        {
            await _repository.UpdateAsync(lot);
        }
        catch (ConcurrencyException ex)
        {
            caughtException = ex;
        }

        caughtException.Should().NotBeNull();
        caughtException!.EntityType.Should().Be(nameof(Lot));
        caughtException.EntityId.Should().Be(lot.Id);
        caughtException.ExpectedVersion.Should().Be(2);
        caughtException.ActualVersion.Should().Be(3);
        caughtException.Message.Should().Contain("Concurrency conflict");
        caughtException.Message.Should().Contain(lot.Id.ToString());
    }

    #endregion

    #region Helper Methods

    private static Lot CreateLot(Guid? auctionId = null)
    {
        var vehicle = new Sedan("Toyota", "Camry", 2020, "VIN123456789", 30000m, "Silver", 4, false);
        return new Lot(auctionId ?? Guid.NewGuid(), vehicle, 5000m);
    }

    #endregion
}

