using Basilisque.Base.Auth.Identity;

namespace Basilisque.Base.Auth.Authorization;

/// <summary>Evaluates business permissions for an authenticated product actor.</summary>
public interface IPermissionEvaluator
{
    /// <summary>Returns whether the actor may perform the requested action.</summary>
    ValueTask<bool> IsAllowed(Actor actor, PermissionCheck check, CancellationToken cancellationToken = default);

    /// <summary>Returns one decision for each supplied check, keyed by its caller-provided key.</summary>
    /// <remarks>All checks use the same evaluation time. Evaluation failures are reported as errors.</remarks>
    ValueTask<IReadOnlyDictionary<string, bool>> AreAllowed(Actor actor,
        IReadOnlyDictionary<string, PermissionCheck> checks, CancellationToken cancellationToken = default);
}
