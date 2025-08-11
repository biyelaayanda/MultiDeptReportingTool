using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiDeptReportingTool.Migrations
{
    /// <inheritdoc />
    public partial class AddMfaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "BackupCodes",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceFingerprint",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMfaEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChangeAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxConcurrentSessions",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MfaFailedAttempts",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "MfaLockedUntil",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MfaSecret",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MfaSetupAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequirePasswordChange",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UsersId",
                table: "UserPermissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsersId",
                table: "SecurityAuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsersId",
                table: "SecurityAlerts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsersId1",
                table: "SecurityAlerts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UsersId",
                table: "UserPermissions",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_UsersId",
                table: "SecurityAuditLogs",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_UsersId",
                table: "SecurityAlerts",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_UsersId1",
                table: "SecurityAlerts",
                column: "UsersId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SecurityAlerts_Users_UsersId",
                table: "SecurityAlerts",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SecurityAlerts_Users_UsersId1",
                table: "SecurityAlerts",
                column: "UsersId1",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SecurityAuditLogs_Users_UsersId",
                table: "SecurityAuditLogs",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserPermissions_Users_UsersId",
                table: "UserPermissions",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SecurityAlerts_Users_UsersId",
                table: "SecurityAlerts");

            migrationBuilder.DropForeignKey(
                name: "FK_SecurityAlerts_Users_UsersId1",
                table: "SecurityAlerts");

            migrationBuilder.DropForeignKey(
                name: "FK_SecurityAuditLogs_Users_UsersId",
                table: "SecurityAuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPermissions_Users_UsersId",
                table: "UserPermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserPermissions_UsersId",
                table: "UserPermissions");

            migrationBuilder.DropIndex(
                name: "IX_SecurityAuditLogs_UsersId",
                table: "SecurityAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_SecurityAlerts_UsersId",
                table: "SecurityAlerts");

            migrationBuilder.DropIndex(
                name: "IX_SecurityAlerts_UsersId1",
                table: "SecurityAlerts");

            migrationBuilder.DropColumn(
                name: "BackupCodes",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeviceFingerprint",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsMfaEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastPasswordChangeAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MaxConcurrentSessions",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MfaFailedAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MfaLockedUntil",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MfaSecret",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MfaSetupAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RequirePasswordChange",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UsersId",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "UsersId",
                table: "SecurityAuditLogs");

            migrationBuilder.DropColumn(
                name: "UsersId",
                table: "SecurityAlerts");

            migrationBuilder.DropColumn(
                name: "UsersId1",
                table: "SecurityAlerts");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id");
        }
    }
}
