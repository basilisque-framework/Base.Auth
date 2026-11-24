
using Basilisque.DataAccess.EntityFramework.Base.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;

/// <summary>
/// Includes the Basilisque relational infrastructure in generated dependency registration.
/// </summary>
public partial class DependencyRegistrator
{
    partial void doAfterRegistration(IServiceCollection services)
    {
        // The IDbInitializer annotation also discovers the open generic adapter.
        // Persistence registration adds only the closed initializers for enabled contexts.
        var generated = services.FirstOrDefault(service => service.ServiceType == typeof(IDbInitializer) &&
            service.ImplementationType == typeof(AuthPersistenceExtensions.MigrationInitializer<>));
        if (generated is not null)
            services.Remove(generated);
    }
}