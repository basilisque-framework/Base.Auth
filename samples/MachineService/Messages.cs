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

using System.Globalization;
using System.Resources;

namespace Basilisque.Base.Auth.MachineService;

internal static class Messages
{
    private static readonly ResourceManager _resources = new(typeof(Messages));

    internal static string AudienceRequired => get(nameof(AudienceRequired));

    internal static string CertificatePathRequired(string value)
        => string.Format(CultureInfo.CurrentCulture, get(nameof(CertificatePathRequired)), value);

    internal static string ClientAdministrationCompleted => get(nameof(ClientAdministrationCompleted));

    internal static string CommandOptionRequired(string value)
        => string.Format(CultureInfo.CurrentCulture, get(nameof(CommandOptionRequired)), value);

    internal static string ExpiredReplayMarkersRemoved(int value)
        => string.Format(CultureInfo.CurrentCulture, get(nameof(ExpiredReplayMarkersRemoved)), value);

    internal static string InvalidRedirectUri => get(nameof(InvalidRedirectUri));

    internal static string IssuerRequired => get(nameof(IssuerRequired));

    internal static string LocalAdministrationCompleted => get(nameof(LocalAdministrationCompleted));

    internal static string LocalUserPasswordRequired => get(nameof(LocalUserPasswordRequired));

    internal static string MigrateBeforeNativeAot => get(nameof(MigrateBeforeNativeAot));

    internal static string MyAccount => get(nameof(MyAccount));

    internal static string OneAdministrationOperation => get(nameof(OneAdministrationOperation));

    internal static string OneClientAdministrationOperation => get(nameof(OneClientAdministrationOperation));

    internal static string PublicJwksRequired => get(nameof(PublicJwksRequired));

    internal static string SignOut => get(nameof(SignOut));

    internal static string UserNotFound => get(nameof(UserNotFound));

    private static string get(string key) => _resources.GetString(key, CultureInfo.CurrentUICulture)!;
}