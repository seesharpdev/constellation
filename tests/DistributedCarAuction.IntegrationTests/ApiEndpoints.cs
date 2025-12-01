namespace DistributedCarAuction.IntegrationTests;

/// <summary>
/// Centralized API endpoint constants for integration tests.
/// </summary>
public static class ApiEndpoints
{
    public static class Vehicles
    {
        public const string Base = "/api/vehicles";
        public const string Search = "/api/vehicles/search";
        
        public static string ById(Guid id) => $"/api/vehicles/{id}";
    }

    public static class Auctions
    {
        public const string Base = "/api/auctions";
        
        public static string ById(Guid id) => $"/api/auctions/{id}";
        public static string Start(Guid id) => $"/api/auctions/{id}/start";
        public static string Close(Guid id) => $"/api/auctions/{id}/close";
    }

    public static class Lots
    {
        public const string Base = "/api/lots";
        public const string Bids = "/api/lots/bids";
        
        public static string ById(Guid id) => $"/api/lots/{id}";
        public static string HighestBid(Guid id) => $"/api/lots/{id}/highest-bid";
        public static string Winner(Guid id) => $"/api/lots/{id}/winner";
    }

    public static class Partners
    {
        public const string Register = "/api/partners/register";
        public const string Bids = "/api/partners/bids";
    }
}