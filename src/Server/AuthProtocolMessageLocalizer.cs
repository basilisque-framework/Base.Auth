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

using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Basilisque.Base.Auth.Server;

internal sealed class AuthProtocolMessageLocalizer : IStringLocalizer<AuthProtocolMessages>
{
    public LocalizedString this[string name] => getString(name, includeParents: true);

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var message = this[name];
            return message.ResourceNotFound ? message : new LocalizedString(name,
                string.Format(CultureInfo.CurrentCulture, message.Value, arguments));
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => AuthProtocolMessages.Keys.Select(key => getString(key, includeParentCultures)).Where(message => !message.ResourceNotFound);

    internal static string Resolve(IStringLocalizer<AuthProtocolMessages> localizer, string key)
    {
        var message = localizer[key];
        return message.ResourceNotFound ? Messages.GetProtocolMessage(key, includeParents: true)! : message.Value;
    }

    private static LocalizedString getString(string key, bool includeParents)
    {
        var message = AuthProtocolMessages.Keys.Contains(key, StringComparer.Ordinal)
            ? Messages.GetProtocolMessage(key, includeParents)
            : null;
        return new LocalizedString(key, message ?? key, resourceNotFound: message is null);
    }
}