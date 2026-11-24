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

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Basilisque.Base.Auth.Server.Identity;

internal sealed class LocalUserSignInManager : SignInManager<LocalUser>
{
    /// <summary>
    /// Creates the Identity sign-in manager for provisioned accounts.
    /// </summary>
    public LocalUserSignInManager(UserManager<LocalUser> users, IHttpContextAccessor context,
        IUserClaimsPrincipalFactory<LocalUser> principals, IOptions<IdentityOptions> options,
        ILogger<SignInManager<LocalUser>> logger, IAuthenticationSchemeProvider schemes, IUserConfirmation<LocalUser> confirmation)
        : base(users, context, principals, options, logger, schemes, confirmation)
    {
    }

    public override async Task<bool> CanSignInAsync(LocalUser user)
        => user.Enabled && await base.CanSignInAsync(user);

    public override Task<SignInResult> PasswordSignInAsync(LocalUser user, string password, bool isPersistent, bool lockoutOnFailure)
        => base.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure: true);
}