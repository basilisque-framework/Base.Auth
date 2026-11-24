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

/// <summary>Groups permission rules into a reusable role.</summary>
public sealed class PermissionRole
{
    /// <summary>Creates a role with a stable identifier and its rules.</summary>
    public PermissionRole(string id, IEnumerable<RolePermissionRule> rules, string? displayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(rules);

        Id = id;
        DisplayName = displayName;
        Rules = Array.AsReadOnly(rules.ToArray());
    }

    /// <summary>Gets the stable role identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the optional label for display. Use the ID when the label is absent.</summary>
    public string? DisplayName { get; }

    /// <summary>Gets the rules defined by this role.</summary>
    public IReadOnlyList<RolePermissionRule> Rules { get; }
}