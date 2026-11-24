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

/// <summary>Contains a permission decision and the explicit rules that applied.</summary>
public sealed class PermissionDecision
{
    /// <summary>Creates a decision with its matching rules.</summary>
    public PermissionDecision(bool allowed, IEnumerable<PermissionRuleMatch> matchingRules)
    {
        ArgumentNullException.ThrowIfNull(matchingRules);
        Allowed = allowed;
        MatchingRules = Array.AsReadOnly(matchingRules.ToArray());
    }

    /// <summary>Gets whether the requested action is allowed.</summary>
    public bool Allowed { get; }

    /// <summary>Gets all matching rules. An empty list indicates that no rule applied.</summary>
    public IReadOnlyList<PermissionRuleMatch> MatchingRules { get; }
}