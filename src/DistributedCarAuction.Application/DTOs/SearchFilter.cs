namespace DistributedCarAuction.Application.DTOs;

using DistributedCarAuction.Domain.Enums;

public record SearchFilter(
    VehicleType? VehicleType = null,
    string? Make = null,
    string? Model = null,
    int? MinYear = null,
    int? MaxYear = null,
    decimal? MaxMileage = null,
    string? Color = null
);