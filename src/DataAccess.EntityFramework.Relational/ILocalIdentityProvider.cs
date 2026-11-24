using Microsoft.EntityFrameworkCore;

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;

/// <summary>Applies the selected Basilisque database provider to the standard Identity context.</summary>
public interface ILocalIdentityProvider
{
    /// <summary>Gets the Basilisque provider key used for migration and connection settings.</summary>
    string ProviderKey { get; }

    /// <summary>Configures the provider, migration assembly, and Identity migration history table.</summary>
    void Configure(DbContextOptionsBuilder options, string connectionString, string migrationAssembly, string historyTable);
}