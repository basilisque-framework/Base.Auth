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

/// <summary>Defines an explicit rule in a permission role.</summary>
public sealed class RolePermissionRule
{
    /// <summary>Creates a rule for a permission object and one of its supported actions.</summary>
    public RolePermissionRule(string objectId, string actionId, PermissionEffect effect)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        if (effect is not (PermissionEffect.Allow or PermissionEffect.Deny))
            throw new ArgumentOutOfRangeException(nameof(effect));

        ObjectId = objectId;
        ActionId = actionId;
        Effect = effect;
    }

    /// <summary>Gets the permission object on which the rule is defined.</summary>
    public string ObjectId { get; }

    /// <summary>Gets the action identifier used to match direct and inherited rules.</summary>
    public string ActionId { get; }

    /// <summary>Gets the effect of the rule.</summary>
    public PermissionEffect Effect { get; }
}