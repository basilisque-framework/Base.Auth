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

internal sealed class StoredAssignment
{
    public Guid Id { get; init; }
    public Guid RoleId { get; init; }
    public StoredRole Role { get; init; } = null!;
    public required string ActorKind { get; init; }
    public required string SubjectId { get; init; }
    public required string ScopeId { get; init; }
    public DateTime? ValidFromUtc { get; init; }
    public DateTime? ValidUntilUtc { get; init; }

    public RoleAssignment ToModel() => new(Role.Key, ActorKind, SubjectId, ScopeId,
        ValidFromUtc is DateTime from ? new DateTimeOffset(DateTime.SpecifyKind(from, DateTimeKind.Utc)) : null,
        ValidUntilUtc is DateTime until ? new DateTimeOffset(DateTime.SpecifyKind(until, DateTimeKind.Utc)) : null);
}