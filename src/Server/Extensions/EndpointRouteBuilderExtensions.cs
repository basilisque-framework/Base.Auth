using Basilisque.Base.Auth.Server.Configuration;
using Basilisque.Base.Auth.Server.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Basilisque.Base.Auth.Server.Extensions;

/// <summary>
/// Maps the application part of the token endpoint; OpenIddict performs protocol validation first.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the configured client-credentials endpoint. Requires UseAuthentication in the ASP.NET pipeline.
    /// </summary>
    public static IEndpointConventionBuilder MapBaseAuthTokenEndpoint(this IEndpointRouteBuilder endpoints)
    {
        var settings = endpoints.ServiceProvider.GetRequiredService<IOptions<AuthServerOptions>>().Value;
        return endpoints.MapPost(settings.TokenEndpointPath, async (HttpContext context,
            IServiceActorFactory actors, IOpenIddictApplicationManager applications) =>
        {
            var request = context.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("The OpenIddict server middleware did not process this request.");
            if (!request.IsClientCredentialsGrantType())
                return Results.BadRequest();
            var application = await applications.FindByClientIdAsync(request.ClientId!, context.RequestAborted)
                ?? throw new InvalidOperationException("The authenticated client no longer exists.");
            if (request.GetScopes().Any(scope => scope != settings.ApiScope) ||
                !await applications.HasPermissionAsync(application, Permissions.Prefixes.Scope + settings.ApiScope, context.RequestAborted))
                return Results.Forbid(authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
                    properties: new Microsoft.AspNetCore.Authentication.AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidScope
                    }));
            var principal = await actors.CreateAsync(request.ClientId!, context.RequestAborted);
            principal.SetScopes(settings.ApiScope);
            principal.SetResources(settings.Audience);
            return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }).AllowAnonymous();
    }
}
