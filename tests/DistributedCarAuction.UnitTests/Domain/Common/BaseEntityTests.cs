namespace DistributedCarAuction.UnitTests.Domain.Common;

using DistributedCarAuction.Domain.Entities;
using FluentAssertions;
using Xunit;

public class BaseEntityTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_GeneratesUniqueId()
    {
        // Act
        var entity1 = CreateVehicle("VIN1");
        var entity2 = CreateVehicle("VIN2");

        // Assert
        entity1.Id.Should().NotBeEmpty();
        entity2.Id.Should().NotBeEmpty();
        entity1.Id.Should().NotBe(entity2.Id);
    }

    [Fact]
    public void Constructor_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var entity = CreateVehicle();

        // Assert
        var afterCreation = DateTime.UtcNow;
        entity.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        entity.CreatedAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Constructor_SetsInitialVersionToOne()
    {
        // Act
        var entity = CreateVehicle();

        // Assert
        entity.Version.Should().Be(1);
    }

    [Fact]
    public void Constructor_SetsUpdatedAtToNull()
    {
        // Act
        var entity = CreateVehicle();

        // Assert
        entity.UpdatedAt.Should().BeNull();
    }

    #endregion

    #region Version Tests

    [Fact]
    public void Version_IncrementsOnModification()
    {
        // Arrange
        var auction = new Auction("Test Auction", "Description");
        var initialVersion = auction.Version;
        var lot = new Lot(Guid.NewGuid(), CreateVehicle(), 1000m);

        // Act - AddLot calls SetUpdatedAt which increments version
        auction.AddLot(lot);

        // Assert
        auction.Version.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void Version_IncrementsMultipleTimes()
    {
        // Arrange
        var auction = new Auction("Test Auction", "Description");
        var lot1 = new Lot(Guid.NewGuid(), CreateVehicle("VIN1"), 1000m);
        var lot2 = new Lot(Guid.NewGuid(), CreateVehicle("VIN2"), 2000m);

        // Act
        auction.AddLot(lot1);  // Version: 1 -> 2
        auction.AddLot(lot2);  // Version: 2 -> 3
        auction.Start();       // Version: 3 -> 4
        auction.End();         // Version: 4 -> 5

        // Assert
        auction.Version.Should().Be(5);
    }

    #endregion

    #region UpdatedAt Tests

    [Fact]
    public void UpdatedAt_SetOnModification()
    {
        // Arrange
        var auction = new Auction("Test Auction", "Description");
        var lot = new Lot(Guid.NewGuid(), CreateVehicle(), 1000m);
        auction.UpdatedAt.Should().BeNull();

        // Act
        var beforeUpdate = DateTime.UtcNow;
        auction.AddLot(lot);
        var afterUpdate = DateTime.UtcNow;

        // Assert
        auction.UpdatedAt.Should().NotBeNull();
        auction.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
        auction.UpdatedAt.Should().BeOnOrBefore(afterUpdate);
    }

    [Fact]
    public void UpdatedAt_UpdatesOnEachModification()
    {
        // Arrange
        var lot = new Lot(Guid.NewGuid(), CreateVehicle(), 1000m);
        lot.PlaceBid(Guid.NewGuid(), 2000m);
        var firstUpdate = lot.UpdatedAt;

        // Small delay to ensure time difference
        Thread.Sleep(10);

        // Act
        lot.PlaceBid(Guid.NewGuid(), 3000m);

        // Assert
        lot.UpdatedAt.Should().BeAfter(firstUpdate!.Value);
    }

    #endregion

    #region Helper Methods

    private static Sedan CreateVehicle(string vin = "VIN123456789") =>
        new("Make", "Model", 2020, vin, 10000m, "Blue", 4, false);

    #endregion
}

