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

namespace Basilisque.Base.Auth.Authorization.Abstractions;

/// <summary>Evaluates business permissions for an authenticated product actor.</summary>
public interface IPermissionEvaluator
{
    /// <summary>Returns whether the actor may perform the requested action.</summary>
    ValueTask<bool> IsAllowed(Actor actor, PermissionCheck check, CancellationToken cancellationToken = default);

    /// <summary>Returns one decision for each supplied check, keyed by its caller-provided key.</summary>
    /// <remarks>All checks use the same evaluation time. Evaluation failures are reported as errors.</remarks>
    ValueTask<IReadOnlyDictionary<string, bool>> AreAllowed(Actor actor,
        IReadOnlyDictionary<string, PermissionCheck> checks, CancellationToken cancellationToken = default);
}