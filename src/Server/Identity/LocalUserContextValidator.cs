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

internal sealed class LocalUserContextValidator : IUserValidator<LocalUser>
{
    /// <inheritdoc />
    public Task<IdentityResult> ValidateAsync(UserManager<LocalUser> manager, LocalUser user)
    {
        return Task.FromResult(user.ContextId is null || (!string.IsNullOrWhiteSpace(user.ContextId) && user.ContextId.Length <= 256)
            ? IdentityResult.Success
            : IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidLocalUserContext",
                Description = Messages.InvalidLocalUserContext
            }));
    }
}