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
using System.Globalization;

namespace Basilisque.Base.Auth.Abstractions.Unit.Tests.Identity;

/// <summary>Tests the product identity contract.</summary>
[Category("Unit")]
public sealed class ActorTests
{
    /// <summary>Verifies German satellite lookup and the neutral English fallback.</summary>
    [Test]
    [Arguments("de-DE", "Eine Subjekt-ID ist erforderlich.")]
    [Arguments("en-US", "A subject is required.")]
    [Arguments("fr-FR", "A subject is required.")]
    public async Task Constructor_ShouldUseLocalizedMessage_WhenSubjectIsMissing(string cultureName, string expected)
    {
        // Arrange
        var previousCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
        try
        {
            // Act
            var exception = await Assert.That(() => new Actor("", BaseAuthClaims.User)).Throws<ArgumentException>();

            // Assert
            await Assert.That(exception?.Message.StartsWith(expected, StringComparison.Ordinal)).IsTrue();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    /// <summary>Rejects explicitly assigned contexts that are blank.</summary>
    [Test]
    public async Task Constructor_ShouldRejectEmptyContext_InServiceIdentity()
    {
        // Arrange
        var context = "";
        // Act / Assert
        await Assert.That(() => new Actor("service-id", BaseAuthClaims.Service, context)).Throws<ArgumentException>();
    }
}