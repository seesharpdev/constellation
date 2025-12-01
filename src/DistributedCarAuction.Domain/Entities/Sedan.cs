namespace DistributedCarAuction.Domain.Entities;

using DistributedCarAuction.Domain.Enums;

public class Sedan : Vehicle
{
    public int NumberOfDoors { get; init; }

    public bool HasSunroof { get; init; }

    private Sedan() { }

    public Sedan(string make, string model, int year, string vin, decimal mileage, string color, int numberOfDoors, bool hasSunroof)
        : base(make, model, year, vin, mileage, color, VehicleType.Sedan)
    {
        NumberOfDoors = numberOfDoors;
        HasSunroof = hasSunroof;
    }
}

