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

namespace Basilisque.Base.Auth.Authorization;

/// <summary>Assigns a role to an actor within one application-defined scope.</summary>
public sealed class RoleAssignment
{
    /// <summary>Creates a scoped role assignment with optional validity bounds.</summary>
    public RoleAssignment(string roleId, string actorKind, string subjectId, string scopeId,
        DateTimeOffset? validFrom = null, DateTimeOffset? validUntil = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeId);
        if (validFrom >= validUntil)
            throw new ArgumentException(Messages.InvalidAssignmentPeriod, nameof(validUntil));

        RoleId = roleId;
        ActorKind = actorKind;
        SubjectId = subjectId;
        ScopeId = scopeId;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
    }

    /// <summary>Gets the assigned role identifier.</summary>
    public string RoleId { get; }

    /// <summary>Gets the actor kind.</summary>
    public string ActorKind { get; }

    /// <summary>Gets the actor's stable subject identifier.</summary>
    public string SubjectId { get; }

    /// <summary>Gets the application-defined scope identifier.</summary>
    public string ScopeId { get; }

    /// <summary>Gets the inclusive start of the validity period, if specified.</summary>
    public DateTimeOffset? ValidFrom { get; }

    /// <summary>Gets the exclusive end of the validity period, if specified.</summary>
    public DateTimeOffset? ValidUntil { get; }
}