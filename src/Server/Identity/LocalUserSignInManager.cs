using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Basilisque.Base.Auth.Server.Identity;

internal sealed class LocalUserSignInManager : SignInManager<LocalUser>
{
    /// <summary>
    /// Creates the Identity sign-in manager for provisioned accounts.
    /// </summary>
    public LocalUserSignInManager(UserManager<LocalUser> users, IHttpContextAccessor context,
        IUserClaimsPrincipalFactory<LocalUser> principals, IOptions<IdentityOptions> options,
        ILogger<SignInManager<LocalUser>> logger, IAuthenticationSchemeProvider schemes, IUserConfirmation<LocalUser> confirmation)
        : base(users, context, principals, options, logger, schemes, confirmation)
    {
    }

    public override async Task<bool> CanSignInAsync(LocalUser user)
        => user.Enabled && await base.CanSignInAsync(user);

    public override Task<SignInResult> PasswordSignInAsync(LocalUser user, string password, bool isPersistent, bool lockoutOnFailure)
        => base.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure: true);
}