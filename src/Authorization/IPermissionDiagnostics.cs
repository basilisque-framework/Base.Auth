using Basilisque.Base.Auth.Abstractions.Identity;
using Basilisque.Base.Auth.Authorization.Abstractions;

namespace Basilisque.Base.Auth.Authorization;

/// <summary>Explains effective permissions for application-owned administration.</summary>
public interface IPermissionDiagnostics
{
    /// <summary>Returns a decision and its matching rules for each supplied check key.</summary>
    ValueTask<IReadOnlyDictionary<string, PermissionDecision>> Explain(Actor actor,
        IReadOnlyDictionary<string, PermissionCheck> checks, CancellationToken cancellationToken = default);
}