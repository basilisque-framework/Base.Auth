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

using System.Security.Cryptography;

namespace Basilisque.Base.Auth.Client.Credentials.DataProtection;

/// <summary>
/// Protects private-key packages using deployment-managed protection keys.
/// </summary>
public interface IPrivateKeyPackageProtector
{
    /// <summary>
    /// Protects an RSA private key for a stable application/client-specific protection context.
    /// </summary>
    string Protect(RSA privateKey, string keyId, string protectionContext);

    /// <summary>
    /// Loads a credential from a protected package. The caller owns the returned credential.
    /// </summary>
    ClientSigningCredential Load(string package, string protectionContext);
}