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

using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.SqlServer.Integration.Tests;

/// <summary>Checks the SQL Server package carries the required provider integration.</summary>
[Category("Integration")]
public sealed class RegistrationTests
{
    /// <summary>Resolves a SQL Server configurator through the common Basilisque provider selector.</summary>
    [Test]
    public async Task RegisterServices_ShouldResolveConfigurator_InSqlServerConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "SqlServer"
        }).Build());
        Basilisque.Base.Auth.DataAccess.EntityFramework.SqlServer.IServiceCollectionExtensions.RegisterServices(services);
        using var provider = services.BuildServiceProvider();
        // Act
        var configurator = provider.GetRequiredService<IDbProviderServiceProvider>().GetRequiredService<IDbContextOptionsConfigurator>();
        // Assert
        await Assert.That(configurator.GetType().Name).IsEqualTo("SqlServerDbContextOptionsConfigurator");
    }
}