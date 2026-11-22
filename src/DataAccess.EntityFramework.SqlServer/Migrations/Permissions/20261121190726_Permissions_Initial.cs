using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.SqlServer.Migrations.Permissions
{
    /// <inheritdoc />
    public partial class Permissions_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Auth_PermissionActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SourceKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Hidden = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auth_PermissionActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Auth_PermissionObjectTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SourceKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Hidden = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auth_PermissionObjectTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Auth_PermissionRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auth_PermissionRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Auth_PermissionObjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SourceKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Hidden = table.Column<bool>(type: "bit", nullable: false),
                    TypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auth_PermissionObjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Auth_PermissionObjects_Auth_PermissionObjectTypes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "Auth_PermissionObjectTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Auth_PermissionObjects_Auth_PermissionObjects_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Auth_PermissionObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Auth_PermissionTypeActions",
                columns: table => new
                {
                    ActionsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoredObjectTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auth_PermissionTypeActions", x => new { x.ActionsId, x.StoredObjectTypeId });
                    table.ForeignKey(
                        name: "FK_Auth_PermissionTypeActions_Auth_PermissionActions_ActionsId",
                        column: x => x.ActionsId,
                        principalTable: "Auth_PermissionActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Auth_PermissionTypeActions_Auth_PermissionObjectTypes_StoredObjectTypeId",
                        column: x => x.StoredObjectTypeId,
                        principalTable: "Auth_PermissionObjectTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Auth_PermissionRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorKind = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SubjectId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ScopeId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auth_PermissionRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Auth_PermissionRoleAssignments_Auth_PermissionRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Auth_PermissionRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Auth_PermissionRules",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Effect = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auth_PermissionRules", x => new { x.RoleId, x.ObjectId, x.ActionId });
                    table.ForeignKey(
                        name: "FK_Auth_PermissionRules_Auth_PermissionActions_ActionId",
                        column: x => x.ActionId,
                        principalTable: "Auth_PermissionActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Auth_PermissionRules_Auth_PermissionObjects_ObjectId",
                        column: x => x.ObjectId,
                        principalTable: "Auth_PermissionObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Auth_PermissionRules_Auth_PermissionRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Auth_PermissionRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Auth_PermissionActions_Key",
                table: "Auth_PermissionActions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auth_PermissionObjects_Key",
                table: "Auth_PermissionObjects",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auth_PermissionObjects_ParentId",
                table: "Auth_PermissionObjects",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Auth_PermissionObjects_TypeId",
                table: "Auth_PermissionObjects",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Auth_PermissionObjectTypes_Key",
                table: "Auth_PermissionObjectTypes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auth_PermissionRoleAssignments_ActorKind_SubjectId_ScopeId",
                table: "Auth_PermissionRoleAssignments",
                columns: new[] { "ActorKind", "SubjectId", "ScopeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Auth_PermissionRoleAssignments_RoleId",
                table: "Auth_PermissionRoleAssignments",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Auth_PermissionRoles_Key",
                table: "Auth_PermissionRoles",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auth_PermissionRules_ActionId",
                table: "Auth_PermissionRules",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Auth_PermissionRules_ObjectId",
                table: "Auth_PermissionRules",
                column: "ObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Auth_PermissionTypeActions_StoredObjectTypeId",
                table: "Auth_PermissionTypeActions",
                column: "StoredObjectTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Auth_PermissionRoleAssignments");

            migrationBuilder.DropTable(
                name: "Auth_PermissionRules");

            migrationBuilder.DropTable(
                name: "Auth_PermissionTypeActions");

            migrationBuilder.DropTable(
                name: "Auth_PermissionObjects");

            migrationBuilder.DropTable(
                name: "Auth_PermissionRoles");

            migrationBuilder.DropTable(
                name: "Auth_PermissionActions");

            migrationBuilder.DropTable(
                name: "Auth_PermissionObjectTypes");
        }
    }
}
