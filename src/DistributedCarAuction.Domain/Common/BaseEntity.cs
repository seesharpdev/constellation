namespace DistributedCarAuction.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; private set; }
    
    /// <summary>
    /// Optimistic concurrency version. Incremented on each update.
    /// Used to detect concurrent modifications.
    /// </summary>
    public int Version { get; private set; } = 1;

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the timestamp and increments the version for optimistic concurrency.
    /// </summary>
    protected void SetUpdatedAt()
    {
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }
}

