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

using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;
using Basilisque.DependencyInjection.Registration.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.SQLite;

[RegisterServiceSingleton(As = typeof(ILocalIdentityProvider), ImplementsITypeName = false, Key = LocalIdentityProvider.Key)]
internal sealed class LocalIdentityProvider : ILocalIdentityProvider
{
    internal const string Key = "SQLite";

    string ILocalIdentityProvider.ProviderKey => Key;

    public void Configure(DbContextOptionsBuilder options, string connectionString, string migrationAssembly, string historyTable)
        => options.UseSqlite(connectionString, provider => provider
            .MigrationsAssembly(migrationAssembly)
            .MigrationsHistoryTable(historyTable));
}