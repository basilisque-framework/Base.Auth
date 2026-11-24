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

using System.Collections.Frozen;

namespace Basilisque.Base.Auth.Authorization.Abstractions;

/// <summary>Identifies a business action and the scopes applicable to the target resource.</summary>
public sealed class PermissionCheck
{
    /// <summary>Creates a check with scope IDs resolved by the application from trusted resource data.</summary>
    public PermissionCheck(string objectId, string actionId, IEnumerable<string> applicableScopeIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        ArgumentNullException.ThrowIfNull(applicableScopeIds);

        ObjectId = objectId;
        ActionId = actionId;
        ApplicableScopeIds = applicableScopeIds.ToFrozenSet(StringComparer.Ordinal);
        foreach (var scopeId in ApplicableScopeIds)
            ArgumentException.ThrowIfNullOrWhiteSpace(scopeId, nameof(applicableScopeIds));
    }

    /// <summary>Gets the ID of the permission object.</summary>
    public string ObjectId { get; }

    /// <summary>Gets the stable ID of the requested action.</summary>
    public string ActionId { get; }

    /// <summary>Gets the set of applicable scope IDs. An empty set confers no role assignments.</summary>
    public IReadOnlySet<string> ApplicableScopeIds { get; }
}