namespace DistributedCarAuction.Application.Interfaces;

using DistributedCarAuction.Application.DTOs;
using DistributedCarAuction.Domain.Entities;

public interface IVehicleService
{
    Task<Vehicle> CreateVehicleAsync(CreateVehicleRequest request);

    Task<List<Vehicle>> SearchAsync(SearchFilter searchFilter);

    Task<Vehicle?> GetByIdAsync(Guid vehicleId);
}