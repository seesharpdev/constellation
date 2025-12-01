namespace DistributedCarAuction.API.Authorization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// Simple API Key authorization filter.
/// Validates X-Api-Key header against configured key.
/// </summary>
public class ApiKeyAuthorizationFilter : IAuthorizationFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly IConfiguration _configuration;

    public ApiKeyAuthorizationFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Skip if endpoint has [AllowAnonymous] or no [ApiKeyRequired]
        if (context.ActionDescriptor.EndpointMetadata.Any(m => m is AllowAnonymousAttribute))
            return;

        string? providedKey = context.HttpContext.Request.Headers[ApiKeyHeaderName];
        string? configuredKey = _configuration["ApiKey"];

        if (string.IsNullOrEmpty(configuredKey))
        {
            // No API key configured = allow all (development mode)
            return;
        }

        if (string.IsNullOrEmpty(providedKey) || providedKey != configuredKey)
        {
            context.Result = new UnauthorizedObjectResult(new 
            { 
                error = "Invalid or missing API key",
                header = ApiKeyHeaderName
            });
        }
    }
}