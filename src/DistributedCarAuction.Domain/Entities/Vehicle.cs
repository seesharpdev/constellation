namespace DistributedCarAuction.Domain.Entities;

using DistributedCarAuction.Domain.Common;
using DistributedCarAuction.Domain.Enums;

public abstract class Vehicle : BaseEntity
{
    public string Make { get; set; }

    public string Model { get; set; }

    public int Year { get; set; }

    public string VIN { get; set; }

    public decimal Mileage { get; set; }

    public string Color { get; set; }

    public VehicleType VehicleType { get; set; }

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

