namespace DistributedCarAuction.Application.Interfaces;

/// <summary>
/// Generates unique, monotonically increasing sequence numbers for bids.
/// 
/// In a distributed system, this must be implemented with a centralized
/// sequence source (e.g., Redis INCR, database sequence) to ensure
/// uniqueness across all application instances.
/// </summary>
public interface ISequenceGenerator
{
    /// <summary>
    /// Gets the next sequence number for a specific lot.
    /// Each lot has its own independent sequence to maintain bid ordering per lot.
    /// </summary>
    /// <param name="lotId">The lot ID to generate a sequence for.</param>
    /// <returns>A unique, monotonically increasing sequence number for the lot.</returns>
    Task<long> GetNextSequenceAsync(Guid lotId);

    /// <summary>
    /// Gets the next sequence number synchronously.
    /// Use this only when async is not available (e.g., in domain entities).
    /// </summary>
    /// <param name="lotId">The lot ID to generate a sequence for.</param>
    /// <returns>A unique, monotonically increasing sequence number for the lot.</returns>
    long GetNextSequence(Guid lotId);
}

