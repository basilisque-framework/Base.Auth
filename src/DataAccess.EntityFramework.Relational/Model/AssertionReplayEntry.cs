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

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.Relational.Model;

/// <summary>
/// A one-time-use marker for a validated client assertion.
/// </summary>
public sealed class AssertionReplayEntry
{
    /// <summary>
    /// Gets or sets the SHA-256 identifier derived from client ID and JWT ID.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the expiration in UTC ticks for portable indexed pruning.
    /// </summary>
    public required long ExpiresAtUtcTicks { get; set; }
}