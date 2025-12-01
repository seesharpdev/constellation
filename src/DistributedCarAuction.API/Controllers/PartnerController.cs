namespace DistributedCarAuction.API.Controllers;

using DistributedCarAuction.Application.DTOs;
using DistributedCarAuction.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Partner API - Allows external auction platforms to integrate with this system
/// </summary>
[ApiController]
[Route("api/partners")]
public class PartnerController : ControllerBase
{
    private readonly ILotService _lotService;
    private readonly IBroadcastService _broadcastService;
    private readonly ILogger<PartnerController> _logger;

    public PartnerController(
        ILotService lotService,
        IBroadcastService broadcastService,
        ILogger<PartnerController> logger)
    {
        _lotService = lotService ?? throw new ArgumentNullException(nameof(lotService));
        _broadcastService = broadcastService ?? throw new ArgumentNullException(nameof(broadcastService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register as a partner to receive auction broadcasts
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterPartner([FromBody] RegisterPartnerRequest request)
    {
        try
        {
            await _broadcastService.RegisterPartnerAsync(request.PartnerId, request.CallbackUrl);

            return Ok(new { message = "Partner registered successfully", partnerId = request.PartnerId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering partner");

            return StatusCode(500, new { error = "An error occurred while registering the partner" });
        }
    }

    /// <summary>
    /// Place a bid through partner API
    /// </summary>
    [HttpPost("bids")]
    public async Task<IActionResult> PlaceBidAsPartner([FromBody] PartnerBidRequest request)
    {
        _logger.LogInformation(
            "Partner bid received - PartnerId: {PartnerId}, LotId: {LotId}, Amount: {Amount}",
            request.PartnerId, request.LotId, request.Amount);

        BidRequest bidRequest = new(request.LotId, request.BidderId, request.Amount);
        BidResult result = await _lotService.PlaceBidAsync(bidRequest);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}