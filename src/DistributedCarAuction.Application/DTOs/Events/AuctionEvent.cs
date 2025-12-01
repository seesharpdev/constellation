namespace DistributedCarAuction.Application.DTOs.Events;

/// <summary>
/// Base event envelope for Kafka messages.
/// All auction events share this structure for consistent processing.
/// </summary>
public record AuctionEvent
{
    /// <summary>
    /// Unique event ID for idempotency. Partners can deduplicate using this.
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Event type discriminator: "AuctionCreated", "AuctionStarted", "AuctionEnded", "BidPlaced"
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Kafka partition key. All events for an auction go to the same partition for ordering.
    /// </summary>
    public required Guid AuctionId { get; init; }

    /// <summary>
    /// Event timestamp (UTC).
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Event-specific payload. Type depends on EventType.
    /// </summary>
    public required object Payload { get; init; }
}