namespace DistributedCarAuction.Domain.Entities;

using DistributedCarAuction.Domain.Enums;

public class Truck : Vehicle
{
    public decimal LoadCapacityKg { get; init; }

    public int BedLengthCm { get; init; }

    public bool HasFourWheelDrive { get; init; }

    private Truck() { }

    public Truck(string make, string model, int year, string vin, decimal mileage, string color, decimal loadCapacityKg, int bedLengthCm, bool hasFourWheelDrive)
        : base(make, model, year, vin, mileage, color, VehicleType.Truck)
    {
        LoadCapacityKg = loadCapacityKg;
        BedLengthCm = bedLengthCm;
        HasFourWheelDrive = hasFourWheelDrive;
    }
}

