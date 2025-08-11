using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiDeptReportingTool.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceFingerprints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fingerprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ScreenResolution = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Timezone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Plugins = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CookiesEnabled = table.Column<bool>(type: "bit", nullable: false),
                    JavaEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ColorDepth = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FirstSeen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsTrusted = table.Column<bool>(type: "bit", nullable: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceFingerprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceFingerprints_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MaxConcurrentSessions = table.Column<int>(type: "int", nullable: false),
                    SessionTimeoutMinutes = table.Column<int>(type: "int", nullable: false),
                    ExtendedSessionTimeoutMinutes = table.Column<int>(type: "int", nullable: false),
                    IdleTimeoutMinutes = table.Column<int>(type: "int", nullable: false),
                    RequireDeviceVerification = table.Column<bool>(type: "bit", nullable: false),
                    EnableConcurrentSessionControl = table.Column<bool>(type: "bit", nullable: false),
                    LogAllSessionActivity = table.Column<bool>(type: "bit", nullable: false),
                    AllowRememberMe = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionConfigurations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevocationReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSuspicious = table.Column<bool>(type: "bit", nullable: false),
                    SuspiciousReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FailedAccessAttempts = table.Column<int>(type: "int", nullable: false),
                    IsRememberMe = table.Column<bool>(type: "bit", nullable: false),
                    RequiresMfaVerification = table.Column<bool>(type: "bit", nullable: false),
                    LastMfaVerification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceFingerprintId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_UserSessions_DeviceFingerprints_DeviceFingerprintId",
                        column: x => x.DeviceFingerprintId,
                        principalTable: "DeviceFingerprints",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Activity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RiskReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionActivities_UserSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "UserSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceFingerprints_Fingerprint_UserId",
                table: "DeviceFingerprints",
                columns: new[] { "Fingerprint", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceFingerprints_IsBlocked",
                table: "DeviceFingerprints",
                column: "IsBlocked");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceFingerprints_IsTrusted",
                table: "DeviceFingerprints",
                column: "IsTrusted");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceFingerprints_UserId",
                table: "DeviceFingerprints",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionActivities_Activity",
                table: "SessionActivities",
                column: "Activity");

            migrationBuilder.CreateIndex(
                name: "IX_SessionActivities_RiskLevel",
                table: "SessionActivities",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_SessionActivities_SessionId",
                table: "SessionActivities",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionActivities_Timestamp",
                table: "SessionActivities",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SessionConfigurations_UserId",
                table: "SessionConfigurations",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_CreatedAt",
                table: "UserSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_DeviceFingerprint",
                table: "UserSessions",
                column: "DeviceFingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_DeviceFingerprintId",
                table: "UserSessions",
                column: "DeviceFingerprintId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_ExpiresAt",
                table: "UserSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_IpAddress",
                table: "UserSessions",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_IsActive",
                table: "UserSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionActivities");

            migrationBuilder.DropTable(
                name: "SessionConfigurations");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "DeviceFingerprints");
        }
    }
}
