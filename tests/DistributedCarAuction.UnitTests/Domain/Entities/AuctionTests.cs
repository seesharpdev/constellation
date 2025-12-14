namespace DistributedCarAuction.UnitTests.Domain.Entities;

using AutoFixture.Xunit2;
using DistributedCarAuction.Domain.Entities;
using DistributedCarAuction.Domain.Enums;
using DistributedCarAuction.UnitTests.Fixtures;
using FluentAssertions;
using Xunit;

public class AuctionTests
{
    #region Constructor Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTitle_ThrowsArgumentException(string? invalidTitle)
    {
        // Act
        var act = () => new Auction(invalidTitle!, "description");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("title");
    }

    [Theory, AutoDomainData]
    public void Constructor_WithValidTitle_CreatesAuction(string title, string description)
    {
        // Act
        var auction = new Auction(title, description);

        // Assert
        auction.Title.Should().Be(title);
        auction.Description.Should().Be(description);
        auction.State.Should().Be(AuctionState.Created);
        auction.Id.Should().NotBeEmpty();
        auction.Lots.Should().BeEmpty();
    }

    [Theory, AutoDomainData]
    public void Constructor_WithNullDescription_SetsEmptyDescription(string title)
    {
        // Act
        var auction = new Auction(title, null!);

        // Assert
        auction.Description.Should().BeEmpty();
    }

    #endregion

    #region AddLot Tests

    [Theory, AutoDomainData]
    public void AddLot_InCreatedState_AddsLotSuccessfully(Auction auction, Lot lot)
    {
        // Act
        auction.AddLot(lot);

        // Assert
        auction.Lots.Should().ContainSingle()
            .Which.Should().Be(lot);
    }

    [Theory, AutoDomainData]
    public void AddLot_InCreatedState_IncrementsVersion(Auction auction, Lot lot)
    {
        // Arrange
        var initialVersion = auction.Version;

        // Act
        auction.AddLot(lot);

        // Assert
        auction.Version.Should().Be(initialVersion + 1);
    }

    [Theory, AutoDomainData]
    public void AddLot_WithNullLot_ThrowsArgumentNullException(Auction auction)
    {
        // Act
        var act = () => auction.AddLot(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory, AutoDomainData]
    public void AddLot_InActiveState_ThrowsInvalidOperationException(Auction auction, Lot lot1, Lot lot2)
    {
        // Arrange
        auction.AddLot(lot1);
        auction.Start();

        // Act
        var act = () => auction.AddLot(lot2);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Created state*");
    }

    [Theory, AutoDomainData]
    public void AddLot_InEndedState_ThrowsInvalidOperationException(Auction auction, Lot lot1, Lot lot2)
    {
        // Arrange
        auction.AddLot(lot1);
        auction.Start();
        auction.End();

        // Act
        var act = () => auction.AddLot(lot2);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Created state*");
    }

    #endregion

    #region Start Tests

    [Theory, AutoDomainData]
    public void Start_FromCreatedStateWithLots_TransitionsToActive(Auction auction, Lot lot)
    {
        // Arrange
        auction.AddLot(lot);

        // Act
        auction.Start();

        // Assert
        auction.State.Should().Be(AuctionState.Active);
        auction.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory, AutoDomainData]
    public void Start_FromCreatedStateWithLots_IncrementsVersion(Auction auction, Lot lot)
    {
        // Arrange
        auction.AddLot(lot);
        var versionBeforeStart = auction.Version;

        // Act
        auction.Start();

        // Assert
        auction.Version.Should().Be(versionBeforeStart + 1);
    }

    [Theory, AutoDomainData]
    public void Start_WithoutLots_ThrowsInvalidOperationException(Auction auction)
    {
        // Act
        var act = () => auction.Start();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*without lots*");
    }

    [Theory, AutoDomainData]
    public void Start_FromActiveState_ThrowsInvalidOperationException(Auction auction, Lot lot)
    {
        // Arrange
        auction.AddLot(lot);
        auction.Start();

        // Act
        var act = () => auction.Start();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot start*");
    }

    [Theory, AutoDomainData]
    public void Start_FromEndedState_ThrowsInvalidOperationException(Auction auction, Lot lot)
    {
        // Arrange
        auction.AddLot(lot);
        auction.Start();
        auction.End();

        // Act
        var act = () => auction.Start();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot start*");
    }

    #endregion

    #region End Tests

    [Theory, AutoDomainData]
    public void End_FromActiveState_TransitionsToEnded(Auction auction, Lot lot)
    {
        // Arrange
        auction.AddLot(lot);
        auction.Start();

        // Act
        auction.End();

        // Assert
        auction.State.Should().Be(AuctionState.Ended);
        auction.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory, AutoDomainData]
    public void End_FromActiveState_IncrementsVersion(Auction auction, Lot lot)
    {
        // Arrange
        auction.AddLot(lot);
        auction.Start();
        var versionBeforeEnd = auction.Version;

        // Act
        auction.End();

        // Assert
        auction.Version.Should().Be(versionBeforeEnd + 1);
    }

    [Theory, AutoDomainData]
    public void End_FromCreatedState_ThrowsInvalidOperationException(Auction auction)
    {
        // Act
        var act = () => auction.End();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot end*");
    }

    [Theory, AutoDomainData]
    public void End_FromEndedState_ThrowsInvalidOperationException(Auction auction, Lot lot)
    {
        // Arrange
        auction.AddLot(lot);
        auction.Start();
        auction.End();

        // Act
        var act = () => auction.End();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot end*");
    }

    #endregion

    #region CanAcceptBids Tests

    [Theory, AutoDomainData]
    public void CanAcceptBids_InCreatedState_ReturnsFalse(Auction auction)
    {
        // Assert
        auction.CanAcceptBids().Should().BeFalse();
    }

    [Theory, AutoDomainData]
    public void CanAcceptBids_InActiveState_ReturnsTrue(Auction auction, Lot lot)
    {
        // Arrange
        auction.AddLot(lot);
        auction.Start();

        // Assert
        auction.CanAcceptBids().Should().BeTrue();
    }

    [Theory, AutoDomainData]
    public void CanAcceptBids_InEndedState_ReturnsFalse(Auction auction, Lot lot)
    {
        // Arrange
        auction.AddLot(lot);
        auction.Start();
        auction.End();

        // Assert
        auction.CanAcceptBids().Should().BeFalse();
    }

    #endregion

    #region GetLot Tests

    [Theory, AutoDomainData]
    public void GetLot_WithExistingLotId_ReturnsLot(Auction auction, Lot lot)
    {
        // Arrange
        auction.AddLot(lot);

        // Act
        var result = auction.GetLot(lot.Id);

        // Assert
        result.Should().Be(lot);
    }

    [Theory, AutoDomainData]
    public void GetLot_WithNonExistingLotId_ReturnsNull(Auction auction, Lot lot)
    {
        // Arrange
        auction.AddLot(lot);

        // Act
        var result = auction.GetLot(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Thread-Safety Tests

    [Theory, AutoDomainData]
    public void Lots_Property_ReturnsSnapshot(Auction auction, Lot lot1, Lot lot2)
    {
        // Arrange
        auction.AddLot(lot1);
        var snapshot = auction.Lots;
        
        // Act - Add another lot after getting snapshot
        auction.AddLot(lot2);

        // Assert - Original snapshot should be unchanged
        snapshot.Should().HaveCount(1);
        auction.Lots.Should().HaveCount(2);
    }

    [Theory, AutoDomainData]
    public async Task AddLot_ConcurrentCalls_AllLotsAdded(Auction auction)
    {
        // Arrange
        const int concurrentLots = 10;
        var lots = Enumerable.Range(0, concurrentLots)
            .Select(_ => new Lot(Guid.NewGuid(), new Sedan("Make", "Model", 2020, "VIN" + Guid.NewGuid(), 10000m, "Blue", 4, false), 1000m))
            .ToList();

        // Act
        var tasks = lots.Select(lot => Task.Run(() => auction.AddLot(lot)));
        await Task.WhenAll(tasks);

        // Assert
        auction.Lots.Should().HaveCount(concurrentLots);
    }

    #endregion
}

