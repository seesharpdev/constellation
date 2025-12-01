namespace DistributedCarAuction.Application.DTOs;

using DistributedCarAuction.Domain.Enums;

public record CreateVehicleRequest(
    VehicleType VehicleType,
    string Make,
    string Model,
    int Year,
    string VIN,
    decimal Mileage,
    string Color,
    Dictionary<string, object>? AdditionalAttributes = null
);