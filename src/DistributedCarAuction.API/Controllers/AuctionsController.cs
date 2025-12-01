namespace DistributedCarAuction.API.Controllers;

using DistributedCarAuction.Application.DTOs;
using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/auctions")]
[EnableRateLimiting("fixed")]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionService _auctionService;
    private readonly ILogger<AuctionsController> _logger;

    public AuctionsController(IAuctionService auctionService, ILogger<AuctionsController> logger)
    {
        _auctionService = auctionService ?? throw new ArgumentNullException(nameof(auctionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAuction([FromBody] CreateAuctionRequest request)
    {
        try
        {
            Auction auction = await _auctionService.CreateAuctionAsync(request);

            return CreatedAtAction(nameof(GetAuction), new { id = auction.Id }, auction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating auction");

            return StatusCode(500, new { error = "An unexpected error occurred while creating the auction" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAuction(Guid id)
    {
        Auction? auction = await _auctionService.GetByIdAsync(id);
        if (auction == null)
            return NotFound();

        return Ok(auction);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAuctions()
    {
        List<Auction> auctions = await _auctionService.GetAllAsync();

        return Ok(auctions);
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartAuction(Guid id)
    {
        try
        {
            await _auctionService.StartAuctionAsync(id);

            return Ok(new { message = "Auction started successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting auction {AuctionId}", id);

            return StatusCode(500, new { error = "An error occurred while starting the auction" });
        }
    }

    [HttpPost("{id}/close")]
    public async Task<IActionResult> CloseAuction(Guid id)
    {
        try
        {
            await _auctionService.CloseAuctionAsync(id);

            return Ok(new { message = "Auction closed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing auction {AuctionId}", id);

            return StatusCode(500, new { error = "An error occurred while closing the auction" });
        }
    }
}