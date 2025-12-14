namespace DistributedCarAuction.UnitTests.Domain.Entities;

using DistributedCarAuction.Domain.Entities;
using FluentAssertions;
using Xunit;

public class BidTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithEmptyBidderId_ThrowsArgumentException()
    {
        // Act
        var act = () => new Bid(Guid.Empty, Guid.NewGuid(), 1000m, 1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("bidderId");
    }

    [Fact]
    public void Constructor_WithEmptyLotId_ThrowsArgumentException()
    {
        // Act
        var act = () => new Bid(Guid.NewGuid(), Guid.Empty, 1000m, 1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("lotId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidAmount_ThrowsArgumentException(decimal invalidAmount)
    {
        // Act
        var act = () => new Bid(Guid.NewGuid(), Guid.NewGuid(), invalidAmount, 1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("amount");
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesBid()
    {
        // Arrange
        var bidderId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        var amount = 5000m;
        var sequence = 42L;

        // Act
        var bid = new Bid(bidderId, lotId, amount, sequence);

        // Assert
        bid.BidderId.Should().Be(bidderId);
        bid.LotId.Should().Be(lotId);
        bid.Amount.Should().Be(amount);
        bid.Sequence.Should().Be(sequence);
        bid.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_SetsBidTimeToUtcNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var bid = new Bid(Guid.NewGuid(), Guid.NewGuid(), 1000m, 1);

        // Assert
        var afterCreation = DateTime.UtcNow;
        bid.BidTime.Should().BeOnOrAfter(beforeCreation);
        bid.BidTime.Should().BeOnOrBefore(afterCreation);
    }

    #endregion
}

