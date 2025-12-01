namespace DistributedCarAuction.Domain.Entities;

using DistributedCarAuction.Domain.Common;
using DistributedCarAuction.Domain.Enums;

public abstract class Vehicle : BaseEntity
{
    public string Make { get; init; }

    public string Model { get; init; }

    public int Year { get; init; }

    public string VIN { get; init; }

    public decimal Mileage { get; init; }

    public string Color { get; init; }

    public VehicleType VehicleType { get; init; }

    protected Vehicle() 
    { 
        Make = string.Empty;
        Model = string.Empty;
        VIN = string.Empty;
        Color = string.Empty;
    }

    protected Vehicle(string make, string model, int year, string vin, decimal mileage, string color, VehicleType vehicleType)
    {
        Make = make ?? throw new ArgumentNullException(nameof(make));
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Year = year;
        VIN = vin ?? throw new ArgumentNullException(nameof(vin));
        Mileage = mileage;
        Color = color ?? throw new ArgumentNullException(nameof(color));
        VehicleType = vehicleType;
    }
}

