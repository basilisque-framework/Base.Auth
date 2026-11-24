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

using Basilisque.Base.Auth.Abstractions.Identity;

namespace Basilisque.Base.Auth.Server.Identity;

/// <summary>
/// Resolves an authenticated local account to its admitted product identity.
/// </summary>
public interface ILocalUserAdmission
{
    /// <summary>
    /// Returns the admitted actor, or null when the account has no product access.
    /// </summary>
    ValueTask<Actor?> ResolveAsync(LocalUser user, CancellationToken cancellationToken = default);
}