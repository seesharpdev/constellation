namespace DistributedCarAuction.UnitTests.Fixtures;

using AutoFixture;
using DistributedCarAuction.Domain.Entities;

/// <summary>
/// AutoFixture customization for domain entities.
/// Configures AutoFixture to properly create domain objects with valid data.
/// </summary>
public class DomainCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        // Configure Sedan with valid data using the full constructor
        fixture.Customize<Sedan>(composer => composer
            .FromFactory(() => new Sedan(
                fixture.Create<string>(),           // make
                fixture.Create<string>(),           // model
                DateTime.UtcNow.Year - 2,           // year
                "VIN" + Guid.NewGuid().ToString("N")[..13], // vin (17 chars typical)
                Math.Abs(fixture.Create<decimal>()) + 1000m, // mileage
                fixture.Create<string>(),           // color
                4,                                  // numberOfDoors
                fixture.Create<bool>()              // hasSunroof
            )));

        // Configure Vehicle creation (abstract class - use concrete Sedan)
        fixture.Register<Vehicle>(() => fixture.Create<Sedan>());

        // Configure Auction with valid title
        fixture.Customize<Auction>(composer => composer
            .FromFactory(() => new Auction(
                fixture.Create<string>(),
                fixture.Create<string>())));

        // Configure Lot with valid data (requires existing auction and vehicle)
        fixture.Customize<Lot>(composer => composer
            .FromFactory(() =>
            {
                var auctionId = Guid.NewGuid();
                var vehicle = fixture.Create<Vehicle>();
                var startingBid = Math.Abs(fixture.Create<decimal>()) + 100m; // Ensure positive
                return new Lot(auctionId, vehicle, startingBid);
            }));

        // Configure Bid with valid data
        fixture.Customize<Bid>(composer => composer
            .FromFactory(() => new Bid(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Math.Abs(fixture.Create<decimal>()) + 100m, // Ensure positive
                fixture.Create<long>())));
    }
}

