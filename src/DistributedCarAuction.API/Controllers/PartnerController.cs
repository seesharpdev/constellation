namespace DistributedCarAuction.API.Controllers;

using DistributedCarAuction.Application.DTOs;
using DistributedCarAuction.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Partner API - Allows external auction platforms to place bids.
/// Partners receive auction events by consuming from Kafka topic "auction-events".
/// </summary>
[ApiController]
[Route("api/partners")]
public class PartnerController : ControllerBase
{
    private readonly ILotService _lotService;
    private readonly ILogger<PartnerController> _logger;

    public PartnerController(
        ILotService lotService,
        ILogger<PartnerController> logger)
    {
        _lotService = lotService ?? throw new ArgumentNullException(nameof(lotService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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