namespace DistributedCarAuction.Domain.Entities;

using DistributedCarAuction.Domain.Enums;

public class SUV : Vehicle
{
    public int SeatingCapacity { get; init; }

    public bool HasFourWheelDrive { get; init; }

    public decimal CargoCapacityLiters { get; init; }

    private SUV() { }

    public SUV(string make, string model, int year, string vin, decimal mileage, string color, int seatingCapacity, bool hasFourWheelDrive, decimal cargoCapacityLiters)
        : base(make, model, year, vin, mileage, color, VehicleType.SUV)
    {
        SeatingCapacity = seatingCapacity;
        HasFourWheelDrive = hasFourWheelDrive;
        CargoCapacityLiters = cargoCapacityLiters;
    }
}

