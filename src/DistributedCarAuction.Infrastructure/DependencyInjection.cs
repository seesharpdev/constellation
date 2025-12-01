namespace DistributedCarAuction.Infrastructure;

using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Application.Interfaces.Repositories;
using DistributedCarAuction.Infrastructure.Persistence;
using DistributedCarAuction.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Register repositories as singletons (in-memory storage)
        services.AddSingleton<IVehicleRepository, InMemoryVehicleRepository>();
        services.AddSingleton<IAuctionRepository, InMemoryAuctionRepository>();
        services.AddSingleton<ILotRepository, InMemoryLotRepository>();

        // Register services
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IBroadcastService, BroadcastService>();

        return services;
    }
}