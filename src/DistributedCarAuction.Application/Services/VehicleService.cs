namespace DistributedCarAuction.Application.Services;

using DistributedCarAuction.Application.DTOs;
using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Application.Interfaces.Repositories;
using DistributedCarAuction.Domain.Entities;
using DistributedCarAuction.Domain.Enums;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;

    public VehicleService(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository ?? throw new ArgumentNullException(nameof(vehicleRepository));
    }

    public async Task<Vehicle> CreateVehicleAsync(CreateVehicleRequest request)
    {
        Vehicle vehicle = request.VehicleType switch
        {
            VehicleType.Sedan => new Sedan(
                request.Make,
                request.Model,
                request.Year,
                request.VIN,
                request.Mileage,
                request.Color,
                GetAttribute<int>(request.AdditionalAttributes, "NumberOfDoors", 4),
                GetAttribute<bool>(request.AdditionalAttributes, "HasSunroof", false)
            ),
            VehicleType.SUV => new SUV(
                request.Make,
                request.Model,
                request.Year,
                request.VIN,
                request.Mileage,
                request.Color,
                GetAttribute<int>(request.AdditionalAttributes, "SeatingCapacity", 5),
                GetAttribute<bool>(request.AdditionalAttributes, "HasFourWheelDrive", false),
                GetAttribute<decimal>(request.AdditionalAttributes, "CargoCapacityLiters", 0m)
            ),
            VehicleType.Truck => new Truck(
                request.Make,
                request.Model,
                request.Year,
                request.VIN,
                request.Mileage,
                request.Color,
                GetAttribute<decimal>(request.AdditionalAttributes, "LoadCapacityKg", 0m),
                GetAttribute<int>(request.AdditionalAttributes, "BedLengthCm", 0),
                GetAttribute<bool>(request.AdditionalAttributes, "HasFourWheelDrive", false)
            ),
            _ => throw new ArgumentException($"Unsupported vehicle type: {request.VehicleType}")
        };

        return await _vehicleRepository.AddAsync(vehicle);
    }

    public async Task<List<Vehicle>> SearchAsync(SearchFilter searchFilter)
    {
        List<Vehicle> allVehicles = await _vehicleRepository.GetAllAsync();

        return allVehicles.Where(v =>
            (!searchFilter.VehicleType.HasValue || v.VehicleType == searchFilter.VehicleType.Value) &&
            (string.IsNullOrEmpty(searchFilter.Make) || v.Make.Contains(searchFilter.Make, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(searchFilter.Model) || v.Model.Contains(searchFilter.Model, StringComparison.OrdinalIgnoreCase)) &&
            (!searchFilter.MinYear.HasValue || v.Year >= searchFilter.MinYear.Value) &&
            (!searchFilter.MaxYear.HasValue || v.Year <= searchFilter.MaxYear.Value) &&
            (!searchFilter.MaxMileage.HasValue || v.Mileage <= searchFilter.MaxMileage.Value) &&
            (string.IsNullOrEmpty(searchFilter.Color) || v.Color.Equals(searchFilter.Color, StringComparison.OrdinalIgnoreCase))
        ).ToList();
    }

    public async Task<Vehicle?> GetByIdAsync(Guid vehicleId)
    {
        return await _vehicleRepository.GetByIdAsync(vehicleId);
    }

    private T GetAttribute<T>(Dictionary<string, object>? attributes, string key, T defaultValue)
    {
        if (attributes == null || !attributes.TryGetValue(key, out var value))
            return defaultValue;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
}