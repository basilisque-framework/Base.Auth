using Microsoft.AspNetCore.Identity;

namespace Basilisque.Base.Auth.Server.Identity;

internal sealed class ProvisionedLocalUserValidator : IUserValidator<LocalUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<LocalUser> manager, LocalUser user)
    {
        return Task.FromResult(!string.IsNullOrWhiteSpace(user.ContextId) && user.ContextId.Length <= 256
            ? IdentityResult.Success
            : IdentityResult.Failed(new IdentityError
            {
                Code = "LocalUserContextRequired",
                Description = "This account must be provisioned by an administrator."
            }));
    }
}
