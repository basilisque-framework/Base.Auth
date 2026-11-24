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