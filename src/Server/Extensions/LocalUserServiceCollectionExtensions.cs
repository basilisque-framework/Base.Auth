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
                .AddUserValidator<ProvisionedLocalUserValidator>()
                .AddSignInManager<LocalUserSignInManager>()
                .AddDefaultTokenProviders();
        }
    }
}
