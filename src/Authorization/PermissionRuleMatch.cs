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

/// <summary>Identifies an applicable direct or inherited role rule and the scopes activating it.</summary>
public sealed class PermissionRuleMatch
{
    /// <summary>Creates a matching rule and identifies the scopes in which its role applies.</summary>
    public PermissionRuleMatch(string roleId, RolePermissionRule rule, bool inherited, IEnumerable<string> scopeIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleId);
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(scopeIds);

        RoleId = roleId;
        Rule = rule;
        Inherited = inherited;
        ScopeIds = Array.AsReadOnly(scopeIds.ToArray());
    }

    /// <summary>Gets the role that defines the rule.</summary>
    public string RoleId { get; }

    /// <summary>Gets the rule including its source object, action, and effect.</summary>
    public RolePermissionRule Rule { get; }

    /// <summary>Gets whether the rule came from an ancestor of the requested object.</summary>
    public bool Inherited { get; }

    /// <summary>Gets the scopes with active assignments for this role.</summary>
    public IReadOnlyList<string> ScopeIds { get; }
}