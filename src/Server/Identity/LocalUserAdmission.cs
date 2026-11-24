using Basilisque.Base.Auth.Abstractions.Identity;

namespace Basilisque.Base.Auth.Server.Identity;

internal sealed class LocalUserAdmission : ILocalUserAdmission
{
    public ValueTask<Actor?> ResolveAsync(LocalUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(user.Enabled
            ? new Actor(user.Id.ToString("D"), BaseAuthClaims.User, user.ContextId)
            : null);
    }
}