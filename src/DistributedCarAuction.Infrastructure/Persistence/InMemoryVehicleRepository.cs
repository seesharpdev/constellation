namespace DistributedCarAuction.Infrastructure.Persistence;

using DistributedCarAuction.Application.Interfaces.Repositories;
using DistributedCarAuction.Domain.Entities;
using System.Collections.Concurrent;

/// <summary>
/// In-memory vehicle repository. Minimal implementation for LotService dependency.
/// </summary>
public class InMemoryVehicleRepository : IVehicleRepository
{
    private readonly ConcurrentDictionary<Guid, Vehicle> _vehicles = new();

    public Task<Vehicle> AddAsync(Vehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        if (!_vehicles.TryAdd(vehicle.Id, vehicle))
            throw new InvalidOperationException($"Vehicle with ID {vehicle.Id} already exists");
            
        return Task.FromResult(vehicle);
    }

    public Task<Vehicle?> GetByIdAsync(Guid id)
    {
        _vehicles.TryGetValue(id, out var vehicle);
        return Task.FromResult(vehicle);
    }

    public Task<List<Vehicle>> GetAllAsync()
    {
        return Task.FromResult(_vehicles.Values.ToList());
    }
}