using Basilisque.Base.Auth.Identity;
using Basilisque.Base.Auth.Server.Configuration;
using Basilisque.Base.Auth.Server.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Globalization;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Basilisque.Base.Auth.Server.Extensions;

/// <summary>
/// Connects browser authentication to the OpenIddict authorization endpoint.
/// </summary>
public static class LocalUserEndpointRouteBuilderExtensions
{
    private const string SecurityStampClaim = "base_auth.local_security_stamp";
    private const string LocalAccountIdClaim = "base_auth.local_account_id";
    private const string ProtocolScheme = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;
    internal const string AuthorizationPath = "/connect/authorize";

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps the authorization endpoint for provisioned local accounts.
        /// </summary>
        public IEndpointConventionBuilder MapBaseAuthLocalUserAuthorization()
            => endpoints.MapMethods(AuthorizationPath, [HttpMethods.Get, HttpMethods.Post], authorizeAsync).AllowAnonymous();
    }

    private static async Task<IResult> authorizeAsync(HttpContext context, SignInManager<LocalUser> signIn, ILocalUserAdmission admission, IOptions<AuthServerOptions> options, IOpenIddictApplicationManager applications)
    {
        var request = context.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("The OpenIddict middleware did not process this request.");

        var application = await applications.FindByClientIdAsync(request.ClientId!, context.RequestAborted);

        if (application is null || !await applications.HasConsentTypeAsync(application, ConsentTypes.Implicit, context.RequestAborted))
            return protocolError(Errors.ConsentRequired, "This client is not provisioned for first-party sign-in.");

        if (request.HasPromptValue(PromptValues.Consent))
            return protocolError(Errors.ConsentRequired, "Interactive client consent is not available.");

        var authentication = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        var issuedAt = authentication.Properties?.IssuedUtc;
        var renew = request.HasPromptValue(PromptValues.Login) || request.MaxAge is long maxAge &&
            (maxAge == 0 || issuedAt is null || (DateTimeOffset.UtcNow - issuedAt.Value).TotalSeconds > maxAge);
        var user = authentication.Succeeded && !renew ? await signIn.ValidateSecurityStampAsync(authentication.Principal) : null;

        if (user is null)
        {
            if (request.HasPromptValue(PromptValues.None))
                return protocolError(Errors.LoginRequired, "Sign in to continue.");

            var returnPath = context.Request.PathBase + context.Request.Path;
            var query = new QueryString();

            IEnumerable<KeyValuePair<string, StringValues>> parameters = HttpMethods.IsPost(context.Request.Method)
                ? await context.Request.ReadFormAsync(context.RequestAborted)
                : context.Request.Query;

            foreach (var parameter in parameters)
            {
                if (renew && string.Equals(parameter.Key, Parameters.MaxAge, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (renew && string.Equals(parameter.Key, Parameters.Prompt, StringComparison.OrdinalIgnoreCase))
                {
                    var prompts = request.GetPromptValues().Where(prompt => prompt != PromptValues.Login);
                    var remaining = string.Join(' ', prompts);
                    if (remaining.Length > 0)
                        query = query.Add(Parameters.Prompt, remaining);
                    continue;
                }

                foreach (var value in parameter.Value)
                    query = query.Add(parameter.Key, value ?? "");
            }

            return Results.Challenge(new AuthenticationProperties { RedirectUri = returnPath + query }, [IdentityConstants.ApplicationScheme]);
        }

        var actor = await signIn.CanSignInAsync(user) ? await admission.ResolveAsync(user, context.RequestAborted) : null;
        if (actor is null)
            return protocolError(Errors.AccessDenied, "The account has no access to this product.");

        var identity = new ClaimsIdentity("Base.Auth", Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, actor.SubjectId);
        identity.SetClaim(BaseAuthClaims.ActorKind, actor.Kind);
        identity.SetClaim(BaseAuthClaims.ContextId, actor.ContextId);
        identity.SetClaim(LocalAccountIdClaim, await signIn.UserManager.GetUserIdAsync(user));
        identity.SetClaim(SecurityStampClaim, await signIn.UserManager.GetSecurityStampAsync(user));

        if (issuedAt is not null)
            identity.AddClaim(new Claim("auth_time", issuedAt.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64));

        identity.SetDestinations(claim => claim.Type switch
        {
            Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],
            BaseAuthClaims.ActorKind or BaseAuthClaims.ContextId => [Destinations.AccessToken],
            "auth_time" => [Destinations.IdentityToken],
            _ => []
        });

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        if (principal.HasScope(options.Value.ApiScope))
            principal.SetResources(options.Value.Audience);

        return Results.SignIn(principal, authenticationScheme: ProtocolScheme);
    }

    internal static async Task<IResult> ExchangeCodeAsync(HttpContext context, SignInManager<LocalUser> signIn, ILocalUserAdmission admission)
    {
        var authentication = await context.AuthenticateAsync(ProtocolScheme);
        var principal = authentication.Principal;

        if (!authentication.Succeeded || principal is null || principal.GetClaim(BaseAuthClaims.ActorKind) != BaseAuthClaims.User ||
            principal.GetClaim(Claims.Subject) is not { } subject ||
            principal.GetClaim(LocalAccountIdClaim) is not { } accountId)
            return protocolError(Errors.InvalidGrant, "The authorization code is invalid.");

        var user = await signIn.UserManager.FindByIdAsync(accountId);
        var actor = user is not null && await signIn.CanSignInAsync(user)
            ? await admission.ResolveAsync(user, context.RequestAborted)
            : null;

        if (actor is null || user is null || actor.SubjectId != subject ||
            actor.ContextId != principal.GetClaim(BaseAuthClaims.ContextId) ||
            principal.GetClaim(SecurityStampClaim) != await signIn.UserManager.GetSecurityStampAsync(user))
            return protocolError(Errors.InvalidGrant, "The account is no longer authorized.");

        return Results.SignIn(principal, authenticationScheme: ProtocolScheme);
    }

    private static IResult protocolError(string error, string description)
        => Results.Forbid(new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        }), [ProtocolScheme]);
}
