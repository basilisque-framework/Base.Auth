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
using Basilisque.Base.Auth.Authorization.Abstractions;

namespace Basilisque.Base.Auth.Authorization;

/// <summary>Explains effective permissions for application-owned administration.</summary>
public interface IPermissionDiagnostics
{
    /// <summary>Returns a decision and its matching rules for each supplied check key.</summary>
    ValueTask<IReadOnlyDictionary<string, PermissionDecision>> Explain(Actor actor,
        IReadOnlyDictionary<string, PermissionCheck> checks, CancellationToken cancellationToken = default);
}