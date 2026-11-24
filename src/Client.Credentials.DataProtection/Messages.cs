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

namespace Basilisque.Base.Auth.Client.Credentials.DataProtection;

internal static class Messages
{
    private static readonly ResourceManager _resources = new(typeof(Messages));

    internal static string InvalidKeyOrIdentifier => get(nameof(InvalidKeyOrIdentifier));

    internal static string InvalidPrivateKeyPackage => get(nameof(InvalidPrivateKeyPackage));

    internal static string KeyIdentifierRequired => get(nameof(KeyIdentifierRequired));

    internal static string ProtectionContextRequired => get(nameof(ProtectionContextRequired));

    internal static string UnsupportedPrivateKeyPackage => get(nameof(UnsupportedPrivateKeyPackage));

    private static string get(string key) => _resources.GetString(key, CultureInfo.CurrentUICulture)!;
}