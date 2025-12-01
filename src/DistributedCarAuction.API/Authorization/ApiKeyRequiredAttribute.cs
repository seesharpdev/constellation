namespace DistributedCarAuction.API.Authorization;

using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// Marks a controller/action as requiring API key authentication.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyRequiredAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => true;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
        return new ApiKeyAuthorizationFilter(configuration);
    }
}

