using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.SQLite.Migrations.Auth;

/// <inheritdoc />
public partial class InitialOpenIddict : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Auth_Applications",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                ApplicationType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                ClientId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                ClientSecret = table.Column<string>(type: "TEXT", nullable: true),
                ClientType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                ConcurrencyToken = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                ConsentType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                DisplayNames = table.Column<string>(type: "TEXT", nullable: true),
                JsonWebKeySet = table.Column<string>(type: "TEXT", nullable: true),
                Permissions = table.Column<string>(type: "TEXT", nullable: true),
                PostLogoutRedirectUris = table.Column<string>(type: "TEXT", nullable: true),
                Properties = table.Column<string>(type: "TEXT", nullable: true),
                RedirectUris = table.Column<string>(type: "TEXT", nullable: true),
                Requirements = table.Column<string>(type: "TEXT", nullable: true),
                Settings = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Auth_Applications", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Auth_AssertionReplays",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                ExpiresAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Auth_AssertionReplays", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Auth_Scopes",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                ConcurrencyToken = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                Description = table.Column<string>(type: "TEXT", nullable: true),
                Descriptions = table.Column<string>(type: "TEXT", nullable: true),
                DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                DisplayNames = table.Column<string>(type: "TEXT", nullable: true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Properties = table.Column<string>(type: "TEXT", nullable: true),
                Resources = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Auth_Scopes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Auth_Authorizations",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                ApplicationId = table.Column<string>(type: "TEXT", nullable: true),
                ConcurrencyToken = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                CreationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                Properties = table.Column<string>(type: "TEXT", nullable: true),
                Scopes = table.Column<string>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                Subject = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Auth_Authorizations", x => x.Id);
                table.ForeignKey(
                    name: "FK_Auth_Authorizations_Auth_Applications_ApplicationId",
                    column: x => x.ApplicationId,
                    principalTable: "Auth_Applications",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "Auth_Tokens",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                ApplicationId = table.Column<string>(type: "TEXT", nullable: true),
                AuthorizationId = table.Column<string>(type: "TEXT", nullable: true),
                ConcurrencyToken = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                CreationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                Payload = table.Column<string>(type: "TEXT", nullable: true),
                Properties = table.Column<string>(type: "TEXT", nullable: true),
                RedemptionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                ReferenceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                Subject = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                Type = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Auth_Tokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_Auth_Tokens_Auth_Applications_ApplicationId",
                    column: x => x.ApplicationId,
                    principalTable: "Auth_Applications",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_Auth_Tokens_Auth_Authorizations_AuthorizationId",
                    column: x => x.AuthorizationId,
                    principalTable: "Auth_Authorizations",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_Auth_Applications_ClientId",
            table: "Auth_Applications",
            column: "ClientId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Auth_AssertionReplays_ExpiresAtUtcTicks",
            table: "Auth_AssertionReplays",
            column: "ExpiresAtUtcTicks");

        migrationBuilder.CreateIndex(
            name: "IX_Auth_Authorizations_ApplicationId_Status_Subject_Type",
            table: "Auth_Authorizations",
            columns: new[] { "ApplicationId", "Status", "Subject", "Type" });

        migrationBuilder.CreateIndex(
            name: "IX_Auth_Scopes_Name",
            table: "Auth_Scopes",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Auth_Tokens_ApplicationId_Status_Subject_Type",
            table: "Auth_Tokens",
            columns: new[] { "ApplicationId", "Status", "Subject", "Type" });

        migrationBuilder.CreateIndex(
            name: "IX_Auth_Tokens_AuthorizationId",
            table: "Auth_Tokens",
            column: "AuthorizationId");

        migrationBuilder.CreateIndex(
            name: "IX_Auth_Tokens_ReferenceId",
            table: "Auth_Tokens",
            column: "ReferenceId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Auth_AssertionReplays");

        migrationBuilder.DropTable(
            name: "Auth_Scopes");

        migrationBuilder.DropTable(
            name: "Auth_Tokens");

        migrationBuilder.DropTable(
            name: "Auth_Authorizations");

        migrationBuilder.DropTable(
            name: "Auth_Applications");
    }
}
