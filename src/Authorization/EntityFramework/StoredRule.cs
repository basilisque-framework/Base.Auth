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

namespace Basilisque.Base.Auth.Authorization.EntityFramework;

internal sealed class StoredRule
{
    public Guid RoleId { get; init; }
    public Guid ObjectId { get; init; }
    public Guid ActionId { get; init; }
    public required PermissionEffect Effect { get; set; }
    public StoredObject Object { get; init; } = null!;
    public StoredAction Action { get; init; } = null!;

    public RolePermissionRule ToModel() => new(Object.Key, Action.Key, Effect);
}