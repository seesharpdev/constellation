namespace DistributedCarAuction.Infrastructure.Services;

using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Domain.Enums;
using Microsoft.Extensions.Logging;

/// <summary>
/// Simulates real-time notifications to internal users.
/// Production: Replace with SignalR/WebSocket implementation.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task NotifyAuctionStateChanged(Guid auctionId, AuctionState newState)
    {
        _logger.LogInformation(
            "Notification: Auction {AuctionId} state changed to {NewState}",
            auctionId, newState);

        // Production: await _hubContext.Clients.Group($"auction-{auctionId}")
        //     .SendAsync("AuctionStateChanged", auctionId, newState);
        
        return Task.CompletedTask;
    }

    public Task NotifyBidPlaced(Guid lotId, Guid bidderId, decimal amount)
    {
        _logger.LogInformation(
            "Notification: Bid placed on lot {LotId} - Amount: {Amount:C}",
            lotId, amount);

        // Production: await _hubContext.Clients.Group($"lot-{lotId}")
        //     .SendAsync("BidPlaced", new { lotId, bidderId, amount });
        
        return Task.CompletedTask;
    }

    public Task NotifyPartner(string partnerId, object eventData)
    {
        _logger.LogInformation(
            "Notification: Partner {PartnerId} event - {@EventData}",
            partnerId, eventData);

        return Task.CompletedTask;
    }
}