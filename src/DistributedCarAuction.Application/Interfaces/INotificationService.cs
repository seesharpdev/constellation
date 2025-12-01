namespace DistributedCarAuction.Application.Interfaces;

using DistributedCarAuction.Domain.Enums;

/// <summary>
/// Notifies internal users in real-time about auction events.
/// Production: Implement via SignalR/WebSockets for live UI updates.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Notifies users when auction state changes (Created → Active → Ended).
    /// </summary>
    Task NotifyAuctionStateChanged(Guid auctionId, AuctionState newState);
    
    /// <summary>
    /// Notifies users watching a lot when a new bid is placed.
    /// </summary>
    Task NotifyBidPlaced(Guid lotId, Guid bidderId, decimal amount);
    
    /// <summary>
    /// Sends partner-specific event notifications.
    /// </summary>
    Task NotifyPartner(string partnerId, object eventData);
}

