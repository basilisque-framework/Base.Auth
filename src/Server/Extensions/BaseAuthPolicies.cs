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

namespace Basilisque.Base.Auth.Server.Extensions;

/// <summary>
/// Admission policies for product users and services.
/// </summary>
public static class BaseAuthPolicies
{
    /// <summary>
    /// Requires an access token representing a product user.
    /// </summary>
    public const string User = "Base.Auth.User";

    /// <summary>
    /// Requires a validated service token with an internal identity and context.
    /// </summary>
    public const string Service = "Base.Auth.Service";
}