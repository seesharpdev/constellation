namespace DistributedCarAuction.Domain.Exceptions;

/// <summary>
/// Exception thrown when a concurrent modification conflict is detected.
/// This occurs when an entity has been modified by another operation
/// between the time it was read and when the update was attempted.
/// </summary>
public class ConcurrencyException : Exception
{
    /// <summary>
    /// The type of entity that had the conflict.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// The ID of the entity that had the conflict.
    /// </summary>
    public Guid EntityId { get; }

    /// <summary>
    /// The version that was expected (from the entity being updated).
    /// </summary>
    public int ExpectedVersion { get; }

    /// <summary>
    /// The actual version found in storage.
    /// </summary>
    public int ActualVersion { get; }

    public ConcurrencyException(string entityType, Guid entityId, int expectedVersion, int actualVersion)
        : base($"Concurrency conflict on {entityType} with ID {entityId}. Expected version {expectedVersion}, but found version {actualVersion}.")
    {
        EntityType = entityType;
        EntityId = entityId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    public ConcurrencyException(string entityType, Guid entityId, int expectedVersion, int actualVersion, Exception innerException)
        : base($"Concurrency conflict on {entityType} with ID {entityId}. Expected version {expectedVersion}, but found version {actualVersion}.", innerException)
    {
        EntityType = entityType;
        EntityId = entityId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}

