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
using Basilisque.DataAccess.EntityFramework.Base.Stamping;
using Basilisque.DataAccess.EntityFramework.Relational.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Basilisque.Base.Auth.Authorization.EntityFramework;

/// <summary>
/// Stores the optional permission catalog, roles, and scoped assignments.
/// </summary>
public sealed class PermissionDbContext : BaseDbContext<PermissionDbContext>
{
    internal const int MaxStringKeyLength = 128;
    internal const int MaxDisplayNameLength = 256;

    internal DbSet<StoredObjectType> ObjectTypes => Set<StoredObjectType>();
    internal DbSet<StoredAction> Actions => Set<StoredAction>();
    internal DbSet<StoredObject> Objects => Set<StoredObject>();
    internal DbSet<StoredRole> Roles => Set<StoredRole>();
    internal DbSet<StoredRule> Rules => Set<StoredRule>();
    internal DbSet<StoredAssignment> Assignments => Set<StoredAssignment>();

    /// <summary>
    /// Creates a context using the configured Basilisque database provider.
    /// </summary>
    public PermissionDbContext(IDbProviderServiceProvider provider)
        : base(provider)
    { }

    /// <inheritdoc />
    protected override string? GetAppAreaPrefix() => "Auth";

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        var relationalOptions = RelationalOptionsExtension.Extract(optionsBuilder.Options);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(
            relationalOptions.WithMigrationsHistoryTableName("__BASAuthMigrationsHistory"));
    }

    /// <inheritdoc />
    protected override void OnBeforeModelCreating(ModelBuilder modelBuilder)
    {
        var types = modelBuilder.Entity<StoredObjectType>().ToTable("PermissionObjectTypes");
        configureCatalogEntry(types);
        configureCatalogEntry(modelBuilder.Entity<StoredAction>().ToTable("PermissionActions"));
        types.HasMany(type => type.Actions).WithMany().UsingEntity("PermissionTypeActions");

        var objects = modelBuilder.Entity<StoredObject>().ToTable("PermissionObjects");
        configureCatalogEntry(objects);
        objects.HasOne(permissionObject => permissionObject.Type).WithMany().HasForeignKey(permissionObject => permissionObject.TypeId).OnDelete(DeleteBehavior.Restrict);
        objects.HasOne(permissionObject => permissionObject.Parent).WithMany().HasForeignKey(permissionObject => permissionObject.ParentId).OnDelete(DeleteBehavior.Restrict);

        var roles = modelBuilder.Entity<StoredRole>().ToTable("PermissionRoles");
        configureKey(roles);
        roles.UseShadowCreationStamp<StoredRole, string>().UseShadowModificationStamp<StoredRole, string>();

        var rules = modelBuilder.Entity<StoredRule>().ToTable("PermissionRules");
        rules.HasKey(rule => new { rule.RoleId, rule.ObjectId, rule.ActionId });
        rules.UseShadowCreationStamp<StoredRule, string>().UseShadowModificationStamp<StoredRule, string>();
        rules.HasOne<StoredRole>().WithMany(role => role.Rules).HasForeignKey(rule => rule.RoleId);
        rules.HasOne(rule => rule.Object).WithMany().HasForeignKey(rule => rule.ObjectId);
        rules.HasOne(rule => rule.Action).WithMany().HasForeignKey(rule => rule.ActionId).OnDelete(DeleteBehavior.Restrict);

        var assignments = modelBuilder.Entity<StoredAssignment>().ToTable("PermissionRoleAssignments");
        assignments.HasKey(assignment => assignment.Id);
        assignments.UseShadowCreationStamp<StoredAssignment, string>().UseShadowModificationStamp<StoredAssignment, string>();
        assignments.Property(assignment => assignment.Id).ValueGeneratedNever();
        assignments.Property(assignment => assignment.ActorKind).HasMaxLength(MaxStringKeyLength);
        assignments.Property(assignment => assignment.SubjectId).HasMaxLength(MaxStringKeyLength);
        assignments.Property(assignment => assignment.ScopeId).HasMaxLength(MaxStringKeyLength);
        assignments.HasIndex(assignment => new { assignment.ActorKind, assignment.SubjectId, assignment.ScopeId });
        assignments.HasOne(assignment => assignment.Role).WithMany().HasForeignKey(assignment => assignment.RoleId);
    }

    private static void configureCatalogEntry<TEntity>(EntityTypeBuilder<TEntity> entity) where TEntity : class, IStoredCatalogEntry
    {
        configureKey(entity);
        entity.Property<string?>(nameof(IStoredCatalogEntry.SourceKey)).HasMaxLength(MaxStringKeyLength);
    }

    private static void configureKey<TEntity>(EntityTypeBuilder<TEntity> entity) where TEntity : class, IStoredPermissionKey
    {
        entity.HasKey(nameof(IStoredPermissionKey.Id));
        entity.Property<Guid>(nameof(IStoredPermissionKey.Id)).ValueGeneratedNever();
        entity.Property<string>(nameof(IStoredPermissionKey.Key)).IsRequired().HasMaxLength(MaxStringKeyLength);
        entity.HasIndex(nameof(IStoredPermissionKey.Key)).IsUnique();
        entity.Property<string?>(nameof(IStoredPermissionKey.DisplayName)).HasMaxLength(MaxDisplayNameLength);
    }
}