using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeraPrompt.Api.Migrations
{
    public partial class AddPromptGeneratorTelemetryAndHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyQuotaUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyQuotaUsages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelPerformanceStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Purpose = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ModelId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TotalRequests = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    TotalLatencyMs = table.Column<long>(type: "bigint", nullable: false),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false),
                    LastStatusCode = table.Column<int>(type: "int", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelPerformanceStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptGenerationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ParentRecordId = table.Column<int>(type: "int", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RequestedModel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FinalModel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Brief = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExtraContext = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Style = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Duration = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AspectRatio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    GeneratedPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UsedFallback = table.Column<bool>(type: "bit", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    AttemptedModelsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ClientSessionId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RequestIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptGenerationRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyQuotaUsages_DateUtc_IpAddress",
                table: "DailyQuotaUsages",
                columns: new[] { "DateUtc", "IpAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyQuotaUsages_UpdatedAt",
                table: "DailyQuotaUsages",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ModelPerformanceStats_LastUsedAt",
                table: "ModelPerformanceStats",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ModelPerformanceStats_Purpose_ModelId",
                table: "ModelPerformanceStats",
                columns: new[] { "Purpose", "ModelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromptGenerationRecords_ClientSessionId",
                table: "PromptGenerationRecords",
                column: "ClientSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptGenerationRecords_CreatedAt",
                table: "PromptGenerationRecords",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PromptGenerationRecords_ParentRecordId_Version",
                table: "PromptGenerationRecords",
                columns: new[] { "ParentRecordId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_PromptGenerationRecords_PublicId",
                table: "PromptGenerationRecords",
                column: "PublicId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DailyQuotaUsages");
            migrationBuilder.DropTable(name: "ModelPerformanceStats");
            migrationBuilder.DropTable(name: "PromptGenerationRecords");
        }
    }
}
