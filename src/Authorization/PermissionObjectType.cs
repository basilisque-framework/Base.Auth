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

namespace Basilisque.Base.Auth.Authorization;

/// <summary>Defines the actions supported by a kind of permission object.</summary>
public sealed class PermissionObjectType
{
    /// <summary>Creates a type with the stable action IDs supported by its objects.</summary>
    public PermissionObjectType(string id, IEnumerable<string> actionIds, string? displayName = null, string? sourceKey = null, bool hidden = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(actionIds);

        Id = id;
        DisplayName = displayName;
        SourceKey = sourceKey;
        Hidden = hidden;
        ActionIds = actionIds.ToFrozenSet(StringComparer.Ordinal);
        foreach (var actionId in ActionIds)
            ArgumentException.ThrowIfNullOrWhiteSpace(actionId, nameof(actionIds));
    }

    /// <summary>Gets the stable type identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the optional label for display. Use the ID when the label is absent.</summary>
    public string? DisplayName { get; }

    /// <summary>Gets the optional key of the module that supplies this entry.</summary>
    public string? SourceKey { get; }

    /// <summary>Gets the visibility hint for the application.</summary>
    public bool Hidden { get; }

    /// <summary>Gets the action IDs supported by this type.</summary>
    public IReadOnlySet<string> ActionIds { get; }
}