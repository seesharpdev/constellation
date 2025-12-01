namespace DistributedCarAuction.Application.DTOs;

using System.ComponentModel.DataAnnotations;
using DistributedCarAuction.Domain.Enums;

public record CreateVehicleRequest(
    [Required]
    [EnumDataType(typeof(VehicleType), ErrorMessage = "Invalid vehicle type")]
    VehicleType VehicleType,

    [Required(ErrorMessage = "Make is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Make must be between 1 and 100 characters")]
    string Make,

    [Required(ErrorMessage = "Model is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Model must be between 1 and 100 characters")]
    string Model,

    [Required]
    [Range(1900, 2100, ErrorMessage = "Year must be between 1900 and 2100")]
    int Year,

    [Required(ErrorMessage = "VIN is required")]
    [StringLength(17, MinimumLength = 17, ErrorMessage = "VIN must be exactly 17 characters")]
    string VIN,

    [Required]
    [Range(0, 10000000, ErrorMessage = "Mileage must be between 0 and 10 million")]
    decimal Mileage,

    [Required(ErrorMessage = "Color is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Color must be between 1 and 50 characters")]
    string Color,

    Dictionary<string, object>? AdditionalAttributes = null
);