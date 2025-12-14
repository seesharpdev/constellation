namespace DistributedCarAuction.Infrastructure.Services;

using DistributedCarAuction.Application.Interfaces;
using System.Collections.Concurrent;

/// <summary>
/// In-memory sequence generator for single-instance deployments.
/// 
/// Thread-safe using Interlocked operations.
/// NOT suitable for multi-instance deployments - use RedisSequenceGenerator instead.
/// </summary>
public class InMemorySequenceGenerator : ISequenceGenerator
{
    /// <summary>
    /// Per-lot sequence counters.
    /// Each lot maintains its own independent sequence to preserve bid ordering.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, long> _sequences = new();

    public Task<long> GetNextSequenceAsync(Guid lotId)
    {
        return Task.FromResult(GetNextSequence(lotId));
    }

    public long GetNextSequence(Guid lotId)
    {
        // AddOrUpdate with increment logic
        // If key doesn't exist, starts at 1; otherwise increments
        return _sequences.AddOrUpdate(
            lotId,
            addValue: 1,
            updateValueFactory: (_, currentValue) => currentValue + 1);
    }

    /// <summary>
    /// Gets the current sequence value for a lot without incrementing.
    /// Useful for testing and diagnostics.
    /// </summary>
    public long GetCurrentSequence(Guid lotId)
    {
        return _sequences.TryGetValue(lotId, out var value) ? value : 0;
    }

    /// <summary>
    /// Resets the sequence for a lot. Only for testing purposes.
    /// </summary>
    internal void Reset(Guid lotId)
    {
        _sequences.TryRemove(lotId, out _);
    }

    /// <summary>
    /// Resets all sequences. Only for testing purposes.
    /// </summary>
    internal void ResetAll()
    {
        _sequences.Clear();
    }
}