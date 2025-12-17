namespace DistributedCarAuction.Infrastructure;

using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Application.Interfaces.Repositories;
using DistributedCarAuction.Infrastructure.Persistence;
using DistributedCarAuction.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services with in-memory sequence generation.
    /// For multi-instance deployments, use AddInfrastructureWithRedis instead.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Register concrete repository types as singletons (in-memory storage)
        // These are needed by both direct injection and UnitOfWork
        services.AddSingleton<InMemoryVehicleRepository>();
        services.AddSingleton<InMemoryAuctionRepository>();
        services.AddSingleton<InMemoryLotRepository>();

        // Register interfaces pointing to the same singleton instances
        services.AddSingleton<IVehicleRepository>(sp => sp.GetRequiredService<InMemoryVehicleRepository>());
        services.AddSingleton<IAuctionRepository>(sp => sp.GetRequiredService<InMemoryAuctionRepository>());
        services.AddSingleton<ILotRepository>(sp => sp.GetRequiredService<InMemoryLotRepository>());

        // Register Unit of Work factory
        services.AddSingleton<IUnitOfWorkFactory, InMemoryUnitOfWorkFactory>();

        // Register services
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IBroadcastService, BroadcastService>();

        // Register sequence generator (in-memory for single-instance)
        // For multi-instance deployments, replace with RedisSequenceGenerator
        services.AddSingleton<ISequenceGenerator, InMemorySequenceGenerator>();

        return services;
    }

    /// <summary>
    /// Adds infrastructure services with Redis-based sequence generation.
    /// Use this for multi-instance deployments.
    /// Requires Redis connection to be configured separately.
    /// </summary>
    public static IServiceCollection AddInfrastructureWithRedis(this IServiceCollection services)
    {
        // Register concrete repository types as singletons (in-memory storage)
        // In production, these would be replaced with database-backed repositories
        services.AddSingleton<InMemoryVehicleRepository>();
        services.AddSingleton<InMemoryAuctionRepository>();
        services.AddSingleton<InMemoryLotRepository>();

        // Register interfaces pointing to the same singleton instances
        services.AddSingleton<IVehicleRepository>(sp => sp.GetRequiredService<InMemoryVehicleRepository>());
        services.AddSingleton<IAuctionRepository>(sp => sp.GetRequiredService<InMemoryAuctionRepository>());
        services.AddSingleton<ILotRepository>(sp => sp.GetRequiredService<InMemoryLotRepository>());

        // Register Unit of Work factory
        services.AddSingleton<IUnitOfWorkFactory, InMemoryUnitOfWorkFactory>();

        // Register services
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IBroadcastService, BroadcastService>();

        // Register Redis-based sequence generator for distributed deployments
        // Requires: services.AddSingleton<IConnectionMultiplexer>(...) to be registered
        services.AddSingleton<ISequenceGenerator, RedisSequenceGenerator>();

        return services;
    }
}