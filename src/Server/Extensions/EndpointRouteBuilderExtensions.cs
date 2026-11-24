/*
   Copyright 2026 Alexander Stärk

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using Basilisque.Base.Auth.Server.Configuration;
using Basilisque.Base.Auth.Server.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
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
    /// <param name="endpoints">The route builder that receives the token endpoint.</param>
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps the token endpoint for client credentials and, when enabled, local user authorization codes.
        /// Requires UseAuthentication in the ASP.NET pipeline.
        /// </summary>
        public IEndpointConventionBuilder MapBaseAuthTokenEndpoint()
        {
            var settings = endpoints.ServiceProvider.GetRequiredService<IOptions<AuthServerOptions>>().Value;

            return endpoints.MapPost(settings.TokenEndpointPath, async (HttpContext context, IServiceActorFactory actors, IOpenIddictApplicationManager applications) =>
            {
                var request = context.GetOpenIddictServerRequest() ?? throw new InvalidOperationException(Messages.RequestNotProcessedByServerMiddleware);
                if (request.IsAuthorizationCodeGrantType())
                {
                    return await LocalUserEndpointRouteBuilderExtensions.ExchangeCodeAsync(context,
                        context.RequestServices.GetRequiredService<SignInManager<LocalUser>>(),
                        context.RequestServices.GetRequiredService<ILocalUserAdmission>(),
                        context.RequestServices.GetRequiredService<IStringLocalizer<AuthProtocolMessages>>());
                }

                if (!request.IsClientCredentialsGrantType())
                    return Results.BadRequest();

                var application = await applications.FindByClientIdAsync(request.ClientId!, context.RequestAborted) ?? throw new InvalidOperationException(Messages.AuthenticatedClientNotFound);

                if (request.GetScopes().Any(scope => scope != settings.ApiScope) ||
                    !await applications.HasPermissionAsync(application, Permissions.Prefixes.Scope + settings.ApiScope, context.RequestAborted))
                {
                    return Results.Forbid(authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
                        properties: new Microsoft.AspNetCore.Authentication.AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidScope
                        }));
                }

                var principal = await actors.CreateAsync(request.ClientId!, context.RequestAborted);
                principal.SetScopes(settings.ApiScope);
                principal.SetResources(settings.Audience);

                return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }).AllowAnonymous();
        }
    }
}