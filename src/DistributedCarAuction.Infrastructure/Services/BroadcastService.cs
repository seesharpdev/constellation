namespace DistributedCarAuction.Infrastructure.Services;

using DistributedCarAuction.Application.DTOs.Events;
using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Domain.Entities;
using DistributedCarAuction.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
/// Kafka-based broadcast service for publishing auction events.
/// Events are partitioned by AuctionId for ordering guarantees.
/// 
/// Production: Inject IProducer&lt;string, string&gt; from Confluent.Kafka.
/// This implementation simulates Kafka behavior with logging.
/// 
/// Partners consume directly from Kafka topic "auction-events".
/// Legacy webhook delivery handled by separate Webhook Adapter Service.
/// </summary>
public class BroadcastService : IBroadcastService
{
    private readonly ILogger<BroadcastService> _logger;
    private const string TopicName = "auction-events";

    public BroadcastService(ILogger<BroadcastService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task BroadcastAuctionAsync(Auction auction)
    {
        string eventType = auction.State switch
        {
            AuctionState.Created => "AuctionCreated",
            AuctionState.Active => "AuctionStarted",
            AuctionState.Ended => "AuctionEnded",
            _ => "AuctionStateChanged"
        };

        AuctionEvent evt = new()
        {
            EventType = eventType,
            AuctionId = auction.Id,
            Payload = new AuctionStatePayload(
                auction.Title,
                auction.Description,
                auction.State.ToString(),
                auction.Lots.Count
            )
        };

        return PublishToKafkaAsync(evt);
    }

    public Task BroadcastBidAsync(Guid auctionId, Guid lotId, decimal amount)
    {
        AuctionEvent evt = new()
        {
            EventType = "BidPlaced",
            AuctionId = auctionId,
            Payload = new BidPlacedPayload(
                LotId: lotId,
                BidderId: Guid.Empty,  // Would be passed in production
                Amount: amount,
                Sequence: 0,           // Would be passed in production
                IsCurrentlyHighest: true,
                CurrentHighestBid: amount
            )
        };

        return PublishToKafkaAsync(evt);
    }

    /// <summary>
    /// Publishes event to Kafka topic.
    /// Production: Use Confluent.Kafka IProducer with proper error handling.
    /// </summary>
    private Task PublishToKafkaAsync(AuctionEvent evt)
    {
        string partitionKey = evt.AuctionId.ToString();
        string messageValue = JsonSerializer.Serialize(evt, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Simulated Kafka publish - in production use:
        // await _producer.ProduceAsync(TopicName, new Message<string, string>
        // {
        //     Key = partitionKey,
        //     Value = messageValue
        // });

        _logger.LogInformation(
            "KAFKA [{Topic}] Key={PartitionKey} | {EventType} | EventId={EventId}",
            TopicName,
            partitionKey,
            evt.EventType,
            evt.EventId);

        _logger.LogDebug("KAFKA Message: {Message}", messageValue);

        return Task.CompletedTask;
    }
}