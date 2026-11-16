using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.SQLite.Migrations.Identity;

/// <inheritdoc />
public partial class InitialLocalIdentity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Auth_Users",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                ContextId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Auth_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Auth_UserClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Auth_UserClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_Auth_UserClaims_Auth_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Auth_Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Auth_UserLogins",
            columns: table => new
            {
                LoginProvider = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                ProviderKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Auth_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey(
                    name: "FK_Auth_UserLogins_Auth_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Auth_Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Auth_UserPasskeys",
            columns: table => new
            {
                CredentialId = table.Column<byte[]>(type: "BLOB", maxLength: 1024, nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                Data = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Auth_UserPasskeys", x => x.CredentialId);
                table.ForeignKey(
                    name: "FK_Auth_UserPasskeys_Auth_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Auth_Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Auth_UserTokens",
            columns: table => new
            {
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                LoginProvider = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                Value = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Auth_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey(
                    name: "FK_Auth_UserTokens_Auth_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Auth_Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Auth_UserClaims_UserId",
            table: "Auth_UserClaims",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Auth_UserLogins_UserId",
            table: "Auth_UserLogins",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Auth_UserPasskeys_UserId",
            table: "Auth_UserPasskeys",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "Auth_Users",
            column: "NormalizedEmail",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            table: "Auth_Users",
            column: "NormalizedUserName",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Auth_UserClaims");

        migrationBuilder.DropTable(
            name: "Auth_UserLogins");

        migrationBuilder.DropTable(
            name: "Auth_UserPasskeys");

        migrationBuilder.DropTable(
            name: "Auth_UserTokens");

        migrationBuilder.DropTable(
            name: "Auth_Users");
    }
}
