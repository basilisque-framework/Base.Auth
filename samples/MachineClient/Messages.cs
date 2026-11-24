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

namespace Basilisque.Base.Auth.MachineClient;

internal static class Messages
{
    private static readonly ResourceManager _resources = new(typeof(Messages));

    internal static string AuthorityAndApiRequireHttps => get(nameof(AuthorityAndApiRequireHttps));

    internal static string ClientCertificatePassword => get(nameof(ClientCertificatePassword));

    internal static string ExpectedCachedToken => get(nameof(ExpectedCachedToken));

    internal static string ExpectedServiceActor => get(nameof(ExpectedServiceActor));

    internal static string MachineClientFailed => get(nameof(MachineClientFailed));

    internal static string MachineClientUsage => get(nameof(MachineClientUsage));

    internal static string ServiceAuthenticated => get(nameof(ServiceAuthenticated));

    private static string get(string key) => _resources.GetString(key, CultureInfo.CurrentUICulture)!;
}