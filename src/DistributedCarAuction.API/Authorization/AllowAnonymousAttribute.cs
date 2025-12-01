namespace DistributedCarAuction.API.Authorization;

/// <summary>
/// Marks an endpoint as publicly accessible (no API key required).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AllowAnonymousAttribute : Attribute { }

