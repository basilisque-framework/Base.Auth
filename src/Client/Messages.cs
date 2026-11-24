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

namespace Basilisque.Base.Auth.Client;

internal static class Messages
{
    private static readonly ResourceManager _resources = new(typeof(Messages));

    internal static string CertificateHasNoPrivateKey => get(nameof(CertificateHasNoPrivateKey));

    internal static string ExpiredTokenReturned => get(nameof(ExpiredTokenReturned));

    internal static string FixedMachineClientNameExists => get(nameof(FixedMachineClientNameExists));

    internal static string InvalidMachineClientOptions => get(nameof(InvalidMachineClientOptions));

    internal static string KeyIdentifierRequired => get(nameof(KeyIdentifierRequired));

    internal static string MachineClientNotConfigured => get(nameof(MachineClientNotConfigured));

    internal static string MachineClientRegistrationExists => get(nameof(MachineClientRegistrationExists));

    internal static string MultipleMatchingSigningCertificates => get(nameof(MultipleMatchingSigningCertificates));

    internal static string NoValidSigningCertificate => get(nameof(NoValidSigningCertificate));

    internal static string RegistrationNameRequired => get(nameof(RegistrationNameRequired));

    internal static string RsaCertificateTooShort => get(nameof(RsaCertificateTooShort));

    internal static string RsaKeyTooShort => get(nameof(RsaKeyTooShort));

    internal static string SigningCredentialMissing => get(nameof(SigningCredentialMissing));

    internal static string ThumbprintRequired => get(nameof(ThumbprintRequired));

    internal static string TokenRequestFailed => get(nameof(TokenRequestFailed));

    internal static string UnsupportedCertificateKey => get(nameof(UnsupportedCertificateKey));

    internal static string UnsupportedNamedCurve => get(nameof(UnsupportedNamedCurve));

    internal static string UnsupportedSigningPublicKey => get(nameof(UnsupportedSigningPublicKey));

    internal static string ValidRegistrationNameRequired => get(nameof(ValidRegistrationNameRequired));

    private static string get(string key) => _resources.GetString(key, CultureInfo.CurrentUICulture)!;
}