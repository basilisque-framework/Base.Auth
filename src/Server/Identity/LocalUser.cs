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

using Microsoft.AspNetCore.Identity;

namespace Basilisque.Base.Auth.Server.Identity;

/// <summary>
/// A local sign-in account with an optional application context.
/// </summary>
public sealed class LocalUser : IdentityUser<Guid>
{
    /// <summary>Creates a local account with a UUIDv7 identifier.</summary>
    public LocalUser()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>
    /// Gets or sets the context assigned during trusted provisioning.
    /// </summary>
    public string? ContextId { get; set; }

    /// <summary>
    /// Gets or sets whether the user can sign in to the product.
    /// </summary>
    public bool Enabled { get; set; } = true;
}