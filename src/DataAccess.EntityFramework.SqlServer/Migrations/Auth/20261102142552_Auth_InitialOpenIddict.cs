using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.SqlServer.Migrations.Auth;

/// <inheritdoc />
public partial class Auth_InitialOpenIddict : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Auth_Applications",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                ApplicationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ClientSecret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ClientType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                ConsentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                DisplayNames = table.Column<string>(type: "nvarchar(max)", nullable: true),
                JsonWebKeySet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Permissions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                PostLogoutRedirectUris = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                RedirectUris = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Requirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Settings = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Auth_Applications", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Auth_AssertionReplays",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ExpiresAtUtcTicks = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Auth_AssertionReplays", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Auth_Scopes",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Descriptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                DisplayNames = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Resources = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Auth_Scopes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Auth_Authorizations",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                ApplicationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                CreationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Scopes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Subject = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
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
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                ApplicationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                AuthorizationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                CreationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                RedemptionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                ReferenceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Subject = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                Type = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
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
            unique: true,
            filter: "[ClientId] IS NOT NULL");

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
            unique: true,
            filter: "[Name] IS NOT NULL");

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
            unique: true,
            filter: "[ReferenceId] IS NOT NULL");
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
