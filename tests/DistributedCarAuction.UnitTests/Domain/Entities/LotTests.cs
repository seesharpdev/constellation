namespace DistributedCarAuction.UnitTests.Domain.Entities;

using AutoFixture.Xunit2;
using DistributedCarAuction.Domain.Entities;
using DistributedCarAuction.UnitTests.Fixtures;
using FluentAssertions;
using Xunit;

public class LotTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithEmptyAuctionId_ThrowsArgumentException()
    {
        // Arrange
        var vehicle = new Sedan("Make", "Model", 2020, "VIN123", 10000m, "Blue", 4, false);

        // Act
        var act = () => new Lot(Guid.Empty, vehicle, 1000m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("auctionId");
    }

    [Fact]
    public void Constructor_WithNullVehicle_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new Lot(Guid.NewGuid(), null!, 1000m);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("vehicle");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidStartingBid_ThrowsArgumentException(decimal invalidBid)
    {
        // Arrange
        var vehicle = new Sedan("Make", "Model", 2020, "VIN123", 10000m, "Blue", 4, false);

        // Act
        var act = () => new Lot(Guid.NewGuid(), vehicle, invalidBid);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("startingBid");
    }

    [Theory, AutoDomainData]
    public void Constructor_WithValidParameters_CreatesLot(Guid auctionId, Vehicle vehicle)
    {
        // Arrange
        var startingBid = 5000m;
        var reservePrice = 10000m;

        // Act
        var lot = new Lot(auctionId, vehicle, startingBid, reservePrice);

        // Assert
        lot.AuctionId.Should().Be(auctionId);
        lot.Vehicle.Should().Be(vehicle);
        lot.StartingBid.Should().Be(startingBid);
        lot.ReservePrice.Should().Be(reservePrice);
        lot.Bids.Should().BeEmpty();
        lot.Id.Should().NotBeEmpty();
    }

    #endregion

    #region PlaceBid Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void PlaceBid_WithInvalidAmount_ThrowsArgumentException(decimal invalidAmount)
    {
        // Arrange
        var lot = CreateValidLot();

        // Act
        var act = () => lot.PlaceBid(Guid.NewGuid(), invalidAmount);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("amount");
    }

    [Fact]
    public void PlaceBid_WithValidAmount_AddsBidToCollection()
    {
        // Arrange
        var lot = CreateValidLot();
        var bidderId = Guid.NewGuid();
        var amount = 6000m;

        // Act
        lot.PlaceBid(bidderId, amount);

        // Assert
        lot.Bids.Should().ContainSingle()
            .Which.Should().Match<Bid>(b => 
                b.BidderId == bidderId && 
                b.Amount == amount &&
                b.LotId == lot.Id);
    }

    [Fact]
    public void PlaceBid_MultipleBids_AssignsUniqueSequenceNumbers()
    {
        // Arrange
        var lot = CreateValidLot();

        // Act
        lot.PlaceBid(Guid.NewGuid(), 6000m);
        lot.PlaceBid(Guid.NewGuid(), 7000m);
        lot.PlaceBid(Guid.NewGuid(), 8000m);

        // Assert
        var sequences = lot.Bids.Select(b => b.Sequence).ToList();
        sequences.Should().OnlyHaveUniqueItems();
        sequences.Should().BeInAscendingOrder();
    }

    [Fact]
    public void PlaceBid_IncrementsVersion()
    {
        // Arrange
        var lot = CreateValidLot();
        var initialVersion = lot.Version;

        // Act
        lot.PlaceBid(Guid.NewGuid(), 6000m);

        // Assert
        lot.Version.Should().Be(initialVersion + 1);
    }

    #endregion

    #region GetValidBids Tests

    [Fact]
    public void GetValidBids_WithNoBids_ReturnsEmptyList()
    {
        // Arrange
        var lot = CreateValidLot();

        // Act
        var validBids = lot.GetValidBids();

        // Assert
        validBids.Should().BeEmpty();
    }

    [Fact]
    public void GetValidBids_WithAscendingBids_ReturnsAllBids()
    {
        // Arrange
        var lot = CreateValidLot(startingBid: 1000m);
        lot.PlaceBid(Guid.NewGuid(), 2000m);
        lot.PlaceBid(Guid.NewGuid(), 3000m);
        lot.PlaceBid(Guid.NewGuid(), 4000m);

        // Act
        var validBids = lot.GetValidBids();

        // Assert
        validBids.Should().HaveCount(3);
        validBids.Select(b => b.Amount).Should().BeEquivalentTo([2000m, 3000m, 4000m]);
    }

    [Fact]
    public void GetValidBids_WithNonAscendingBids_FiltersInvalidBids()
    {
        // Arrange
        var lot = CreateValidLot(startingBid: 1000m);
        lot.PlaceBid(Guid.NewGuid(), 3000m);  // Valid: > 1000
        lot.PlaceBid(Guid.NewGuid(), 2000m);  // Invalid: < 3000
        lot.PlaceBid(Guid.NewGuid(), 4000m);  // Valid: > 3000
        lot.PlaceBid(Guid.NewGuid(), 3500m);  // Invalid: < 4000

        // Act
        var validBids = lot.GetValidBids();

        // Assert
        validBids.Should().HaveCount(2);
        validBids.Select(b => b.Amount).Should().BeEquivalentTo([3000m, 4000m]);
    }

    [Fact]
    public void GetValidBids_WithBidEqualToStartingBid_ExcludesBid()
    {
        // Arrange
        var lot = CreateValidLot(startingBid: 1000m);
        lot.PlaceBid(Guid.NewGuid(), 1000m);  // Invalid: not > 1000

        // Act
        var validBids = lot.GetValidBids();

        // Assert
        validBids.Should().BeEmpty();
    }

    #endregion

    #region GetHighestBidAmount Tests

    [Fact]
    public void GetHighestBidAmount_WithNoBids_ReturnsStartingBid()
    {
        // Arrange
        var startingBid = 5000m;
        var lot = CreateValidLot(startingBid: startingBid);

        // Act
        var highest = lot.GetHighestBidAmount();

        // Assert
        highest.Should().Be(startingBid);
    }

    [Fact]
    public void GetHighestBidAmount_WithValidBids_ReturnsHighestValidBid()
    {
        // Arrange
        var lot = CreateValidLot(startingBid: 1000m);
        lot.PlaceBid(Guid.NewGuid(), 3000m);
        lot.PlaceBid(Guid.NewGuid(), 2000m);  // Invalid
        lot.PlaceBid(Guid.NewGuid(), 5000m);

        // Act
        var highest = lot.GetHighestBidAmount();

        // Assert
        highest.Should().Be(5000m);
    }

    #endregion

    #region GetHighestBid Tests

    [Fact]
    public void GetHighestBid_WithNoBids_ReturnsNull()
    {
        // Arrange
        var lot = CreateValidLot();

        // Act
        var highest = lot.GetHighestBid();

        // Assert
        highest.Should().BeNull();
    }

    [Fact]
    public void GetHighestBid_WithValidBids_ReturnsHighestValidBid()
    {
        // Arrange
        var lot = CreateValidLot(startingBid: 1000m);
        var bidder1 = Guid.NewGuid();
        var bidder2 = Guid.NewGuid();
        
        lot.PlaceBid(bidder1, 3000m);
        lot.PlaceBid(bidder2, 5000m);

        // Act
        var highest = lot.GetHighestBid();

        // Assert
        highest.Should().NotBeNull();
        highest!.Amount.Should().Be(5000m);
        highest.BidderId.Should().Be(bidder2);
    }

    #endregion

    #region GetWinningBidderId Tests

    [Fact]
    public void GetWinningBidderId_WithNoBids_ReturnsNull()
    {
        // Arrange
        var lot = CreateValidLot();

        // Act
        var winner = lot.GetWinningBidderId();

        // Assert
        winner.Should().BeNull();
    }

    [Fact]
    public void GetWinningBidderId_WithBidsAboveReserve_ReturnsHighestBidder()
    {
        // Arrange
        var lot = CreateValidLot(startingBid: 1000m, reservePrice: 4000m);
        var bidder1 = Guid.NewGuid();
        var bidder2 = Guid.NewGuid();
        
        lot.PlaceBid(bidder1, 3000m);
        lot.PlaceBid(bidder2, 5000m);  // Above reserve

        // Act
        var winner = lot.GetWinningBidderId();

        // Assert
        winner.Should().Be(bidder2);
    }

    [Fact]
    public void GetWinningBidderId_WithBidsBelowReserve_ReturnsNull()
    {
        // Arrange
        var lot = CreateValidLot(startingBid: 1000m, reservePrice: 10000m);
        lot.PlaceBid(Guid.NewGuid(), 3000m);
        lot.PlaceBid(Guid.NewGuid(), 5000m);  // Below reserve

        // Act
        var winner = lot.GetWinningBidderId();

        // Assert
        winner.Should().BeNull();
    }

    [Fact]
    public void GetWinningBidderId_WithNoReservePrice_ReturnsHighestBidder()
    {
        // Arrange
        var lot = CreateValidLot(startingBid: 1000m, reservePrice: null);
        var bidder = Guid.NewGuid();
        lot.PlaceBid(bidder, 2000m);

        // Act
        var winner = lot.GetWinningBidderId();

        // Assert
        winner.Should().Be(bidder);
    }

    #endregion

    #region WouldBidBeValid Tests

    [Fact]
    public void WouldBidBeValid_WithAmountAboveHighest_ReturnsTrue()
    {
        // Arrange
        var lot = CreateValidLot(startingBid: 1000m);
        lot.PlaceBid(Guid.NewGuid(), 2000m);

        // Act
        var result = lot.WouldBidBeValid(3000m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void WouldBidBeValid_WithAmountBelowHighest_ReturnsFalse()
    {
        // Arrange
        var lot = CreateValidLot(startingBid: 1000m);
        lot.PlaceBid(Guid.NewGuid(), 3000m);

        // Act
        var result = lot.WouldBidBeValid(2000m);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void WouldBidBeValid_WithAmountEqualToHighest_ReturnsFalse()
    {
        // Arrange
        var lot = CreateValidLot(startingBid: 1000m);
        lot.PlaceBid(Guid.NewGuid(), 3000m);

        // Act
        var result = lot.WouldBidBeValid(3000m);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Thread-Safety Tests

    [Fact]
    public void Bids_Property_ReturnsSnapshot()
    {
        // Arrange
        var lot = CreateValidLot();
        lot.PlaceBid(Guid.NewGuid(), 2000m);
        var snapshot = lot.Bids;

        // Act - Add another bid after getting snapshot
        lot.PlaceBid(Guid.NewGuid(), 3000m);

        // Assert - Original snapshot should be unchanged
        snapshot.Should().HaveCount(1);
        lot.Bids.Should().HaveCount(2);
    }

    [Fact]
    public async Task PlaceBid_ConcurrentCalls_AllBidsRecorded()
    {
        // Arrange
        var lot = CreateValidLot(startingBid: 100m);
        const int concurrentBids = 50;

        // Act
        var tasks = Enumerable.Range(1, concurrentBids)
            .Select(i => Task.Run(() => lot.PlaceBid(Guid.NewGuid(), 100m + i)))
            .ToList();
        
        await Task.WhenAll(tasks);

        // Assert
        lot.Bids.Should().HaveCount(concurrentBids);
    }

    [Fact]
    public async Task PlaceBid_ConcurrentCalls_UniqueSequenceNumbers()
    {
        // Arrange
        var lot = CreateValidLot(startingBid: 100m);
        const int concurrentBids = 50;

        // Act
        var tasks = Enumerable.Range(1, concurrentBids)
            .Select(i => Task.Run(() => lot.PlaceBid(Guid.NewGuid(), 100m + i)))
            .ToList();
        
        await Task.WhenAll(tasks);

        // Assert
        var sequences = lot.Bids.Select(b => b.Sequence).ToList();
        sequences.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region Helper Methods

    private static Lot CreateValidLot(decimal startingBid = 5000m, decimal? reservePrice = null)
    {
        var vehicle = CreateVehicle();
        return new Lot(Guid.NewGuid(), vehicle, startingBid, reservePrice);
    }

    private static Sedan CreateVehicle() =>
        new("Toyota", "Camry", 2020, "VIN123456789", 30000m, "Silver", 4, false);

    #endregion
}

