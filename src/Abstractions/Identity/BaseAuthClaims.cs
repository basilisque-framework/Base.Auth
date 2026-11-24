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

namespace Basilisque.Base.Auth.Abstractions.Identity;

/// <summary>
/// Defines the claim contract at the product authentication boundary.
/// </summary>
public static class BaseAuthClaims
{
    /// <summary>
    /// The actor kind claim.
    /// </summary>
    public const string ActorKind = "actor_type";

    /// <summary>
    /// The application context claim.
    /// </summary>
    public const string ContextId = "context_id";

    /// <summary>
    /// The service actor kind.
    /// </summary>
    public const string Service = "service";

    /// <summary>
    /// The human actor kind.
    /// </summary>
    public const string User = "user";
}