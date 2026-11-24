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

using Basilisque.Base.Auth.Client.Credentials;

namespace Basilisque.Base.Auth.Client;

/// <summary>Manages outbound machine client registrations in a running service.</summary>
public interface IRuntimeMachineClientRegistry
{
    /// <summary>Adds a connection or replaces its settings, signing credential, and token cache.</summary>
    /// <param name="registrationName">The local name used to request an access token.</param>
    /// <param name="configure">Sets the issuer, client ID, scopes, and token cache settings.</param>
    /// <param name="createCredential">
    /// Creates a fresh signing credential owned by the registry. A custom crypto provider must release
    /// signing operations before the underlying key is disposed.
    /// </param>
    /// <remarks>Token requests already using the previous registration can finish with its settings.</remarks>
    void RegisterOrUpdate(string registrationName, Action<MachineClientOptions> configure,
        Func<ClientSigningCredential> createCredential);

    /// <summary>Removes a runtime registration so subsequent calls cannot acquire tokens for it.</summary>
    /// <param name="registrationName">The local name of the connection to remove.</param>
    /// <returns>Whether the runtime registration existed.</returns>
    bool Remove(string registrationName);
}