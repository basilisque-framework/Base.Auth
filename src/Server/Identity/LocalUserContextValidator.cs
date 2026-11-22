using Microsoft.AspNetCore.Identity;

namespace Basilisque.Base.Auth.Server.Identity;

internal sealed class LocalUserContextValidator : IUserValidator<LocalUser>
{
    /// <inheritdoc />
    public Task<IdentityResult> ValidateAsync(UserManager<LocalUser> manager, LocalUser user)
    {
        return Task.FromResult(user.ContextId is null || (!string.IsNullOrWhiteSpace(user.ContextId) && user.ContextId.Length <= 256)
            ? IdentityResult.Success
            : IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidLocalUserContext",
                Description = Messages.InvalidLocalUserContext
            }));
    }
}
