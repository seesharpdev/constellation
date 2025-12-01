namespace DistributedCarAuction.Application.Interfaces;

using DistributedCarAuction.Domain.Entities;

/// <summary>
/// Broadcasts auction events to external partner platforms for distributed synchronization.
/// Production: Implement via webhooks (simple) or message queues (scalable, ordered).
/// </summary>
public interface IBroadcastService
{
    /// <summary>
    /// Broadcasts auction state to all registered partners (create, start, end events).
    /// </summary>
    Task BroadcastAuctionAsync(Auction auction);
    
    /// <summary>
    /// Broadcasts bid to all partners for distributed consistency and conflict prevention.
    /// </summary>
    Task BroadcastBidAsync(Guid auctionId, Guid lotId, decimal amount);
    
    /// <summary>
    /// Registers a partner's callback URL for webhook notifications.
    /// </summary>
    Task RegisterPartnerAsync(string partnerId, string callbackUrl);
}

