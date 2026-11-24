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

/// <summary>Contains the permission actions, object types, and nodes available to the application.</summary>
public sealed class PermissionCatalog
{
    /// <summary>Creates a catalog from the given types and objects.</summary>
    public PermissionCatalog(IEnumerable<PermissionObjectType> objectTypes, IEnumerable<PermissionObject> objects,
        IEnumerable<PermissionAction>? actions = null)
    {
        ArgumentNullException.ThrowIfNull(objectTypes);
        ArgumentNullException.ThrowIfNull(objects);

        ObjectTypes = Array.AsReadOnly(objectTypes.ToArray());
        Objects = Array.AsReadOnly(objects.ToArray());
        Actions = Array.AsReadOnly((actions ?? ObjectTypes.SelectMany(type => type.ActionIds)
            .Distinct(StringComparer.Ordinal).Select(id => new PermissionAction(id))).ToArray());
    }

    /// <summary>Gets the types and their supported actions.</summary>
    public IReadOnlyList<PermissionObjectType> ObjectTypes { get; }

    /// <summary>Gets the nodes in the permission catalog.</summary>
    public IReadOnlyList<PermissionObject> Objects { get; }

    /// <summary>Gets the available actions and their display labels.</summary>
    public IReadOnlyList<PermissionAction> Actions { get; }
}