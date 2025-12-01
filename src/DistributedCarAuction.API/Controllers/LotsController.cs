namespace DistributedCarAuction.API.Controllers;

using DistributedCarAuction.Application.DTOs;
using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/lots")]
[EnableRateLimiting("fixed")]
public class LotsController : ControllerBase
{
    private readonly ILotService _lotService;
    private readonly ILogger<LotsController> _logger;

    public LotsController(ILotService lotService, ILogger<LotsController> logger)
    {
        _lotService = lotService ?? throw new ArgumentNullException(nameof(lotService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<IActionResult> CreateLot([FromBody] CreateLotRequest request)
    {
        try
        {
            Lot lot = await _lotService.CreateLotAsync(request);

            return CreatedAtAction(nameof(GetLot), new { id = lot.Id }, lot);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lot");

            return StatusCode(500, new { error = "An unexpected error occurred while creating the lot" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLot(Guid id)
    {
        Lot? lot = await _lotService.GetByIdAsync(id);
        if (lot == null)
            return NotFound();

        return Ok(lot);
    }

    [HttpPost("bids")]
    [EnableRateLimiting("bids")]
    public async Task<IActionResult> PlaceBid([FromBody] BidRequest request)
    {
        BidResult result = await _lotService.PlaceBidAsync(request);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{id}/highest-bid")]
    public async Task<IActionResult> GetHighestBid(Guid id)
    {
        try
        {
            decimal highestBid = await _lotService.GetHighestBidAsync(id);

            return Ok(new { lotId = id, highestBid });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("{id}/winner")]
    public async Task<IActionResult> GetWinner(Guid id)
    {
        try
        {
            Guid? winnerId = await _lotService.GetWinnerAsync(id);
            
            if (winnerId == null)
                return Ok(new { lotId = id, winner = (Guid?)null, message = "No winner yet or reserve not met" });

            return Ok(new { lotId = id, winner = winnerId });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}