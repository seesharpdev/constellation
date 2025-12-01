namespace DistributedCarAuction.Application;

using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Application.Services;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application services
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IAuctionService, AuctionService>();
        services.AddScoped<ILotService, LotService>();

        return services;
    }
}