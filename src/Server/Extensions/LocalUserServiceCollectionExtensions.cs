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

using Basilisque.Base.Auth.Server.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Basilisque.Base.Auth.Server.Extensions;

/// <summary>Configures local accounts alongside the Base.Auth token server.</summary>
public static class LocalUserServiceCollectionExtensions
{
    /// <param name="services">The service collection that receives the local-account services.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Enables local Identity accounts and the authorization code grant. The host must supply an Identity user store
        /// and a login UI, and map the Base.Auth authorization endpoint.
        /// </summary>
        public IdentityBuilder AddBaseAuthLocalUsers(Action<IdentityOptions>? configure = null)
        {
            services.TryAddScoped<ILocalUserAdmission, LocalUserAdmission>();
            services.AddAuthentication().AddIdentityCookies();
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                options.SlidingExpiration = false;
            });
            services.AddOpenIddict().AddServer(options =>
            {
                options.SetAuthorizationEndpointUris(LocalUserEndpointRouteBuilderExtensions.AuthorizationPath.TrimStart('/'));
                options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
                options.UseAspNetCore().EnableAuthorizationEndpointPassthrough();
            });
            return services.AddIdentityCore<LocalUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                configure?.Invoke(options);
            })
                .AddUserValidator<LocalUserContextValidator>()
                .AddSignInManager<LocalUserSignInManager>()
                .AddDefaultTokenProviders();
        }
    }
}