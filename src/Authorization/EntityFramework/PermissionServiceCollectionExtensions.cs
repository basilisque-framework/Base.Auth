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

using Basilisque.Base.Auth.Authorization.Abstractions;
using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Basilisque.Base.Auth.Authorization.EntityFramework;

internal static class PermissionServiceCollectionExtensions
{
    internal static void AddPermissionServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddScoped<PermissionDbContext>();
        services.TryAddScoped<IDbContextFactory<PermissionDbContext>, PermissionContextFactory>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IPermissionManagement, EfPermissionManagement>();
        services.TryAddScoped<IPermissionEvaluator, EfPermissionEvaluator>();
        services.TryAddScoped<IPermissionDiagnostics>(provider =>
            provider.GetRequiredService<IPermissionEvaluator>() as IPermissionDiagnostics
            ?? throw new InvalidOperationException(Messages.DiagnosticsUnavailable));
    }

    private sealed class PermissionContextFactory : IDbContextFactory<PermissionDbContext>
    {
        private readonly IDbProviderServiceProvider _provider;

        public PermissionContextFactory(IDbProviderServiceProvider provider)
        {
            _provider = provider;
        }

        public PermissionDbContext CreateDbContext() => new(_provider);
    }
}