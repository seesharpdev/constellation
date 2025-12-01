namespace DistributedCarAuction.Application.Interfaces;

using DistributedCarAuction.Domain.Entities;

/// <summary>
/// Broadcasts auction events to external partners via Kafka for reliable, ordered delivery.
/// Events are partitioned by AuctionId to ensure ordering within each auction.
/// Partners consume directly from Kafka topic "auction-events".
/// </summary>
public interface IBroadcastService
{
    /// <summary>
    /// Publishes auction lifecycle events (Created, Started, Ended) to Kafka.
    /// </summary>
    Task BroadcastAuctionAsync(Auction auction);
    
    /// <summary>
    /// Publishes bid events to Kafka for partner synchronization.
    /// Partitioned by AuctionId to maintain bid ordering.
    /// </summary>
    Task BroadcastBidAsync(Guid auctionId, Guid lotId, decimal amount);
}

