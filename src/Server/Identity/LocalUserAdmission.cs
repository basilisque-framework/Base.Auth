using Basilisque.Base.Auth.Identity;

namespace Basilisque.Base.Auth.Server.Identity;

internal sealed class LocalUserAdmission : ILocalUserAdmission
{
    public ValueTask<Actor?> ResolveAsync(LocalUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(user.Enabled && !string.IsNullOrWhiteSpace(user.ContextId)
            ? new Actor(user.Id, BaseAuthClaims.User, user.ContextId)
            : null);
    }
}
