namespace DistributedCarAuction.Application.Interfaces.Repositories;

using DistributedCarAuction.Domain.Entities;

/// <summary>
/// Repository for vehicle persistence.
/// </summary>
public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id);

    Task<Vehicle> AddAsync(Vehicle vehicle);

    Task<List<Vehicle>> GetAllAsync();
}