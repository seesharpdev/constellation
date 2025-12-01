namespace DistributedCarAuction.API.Controllers;

using DistributedCarAuction.Application.DTOs;
using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/vehicles")]
[EnableRateLimiting("fixed")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;
    private readonly ILogger<VehiclesController> _logger;

    public VehiclesController(IVehicleService vehicleService, ILogger<VehiclesController> logger)
    {
        _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleRequest request)
    {
        try
        {
            Vehicle vehicle = await _vehicleService.CreateVehicleAsync(request);

            return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vehicle");

            return StatusCode(500, new { error = "An unexpected error occurred while creating the vehicle" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetVehicle(Guid id)
    {
        Vehicle? vehicle = await _vehicleService.GetByIdAsync(id);
        if (vehicle == null)
            return NotFound();

        return Ok(vehicle);
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchVehicles([FromBody] SearchFilter filter)
    {
        List<Vehicle> vehicles = await _vehicleService.SearchAsync(filter);

        return Ok(vehicles);
    }
}