namespace DistributedCarAuction.Application.DTOs;

using System.ComponentModel.DataAnnotations;
using DistributedCarAuction.Domain.Enums;

public record SearchFilter(
    [EnumDataType(typeof(VehicleType), ErrorMessage = "Invalid vehicle type")]
    VehicleType? VehicleType = null,

    [StringLength(100, ErrorMessage = "Make cannot exceed 100 characters")]
    string? Make = null,

    [StringLength(100, ErrorMessage = "Model cannot exceed 100 characters")]
    string? Model = null,

    [Range(1900, 2100, ErrorMessage = "Min year must be between 1900 and 2100")]
    int? MinYear = null,

    [Range(1900, 2100, ErrorMessage = "Max year must be between 1900 and 2100")]
    int? MaxYear = null,

    [Range(0, 10000000, ErrorMessage = "Max mileage must be between 0 and 10 million")]
    decimal? MaxMileage = null,

    [StringLength(50, ErrorMessage = "Color cannot exceed 50 characters")]
    string? Color = null
);