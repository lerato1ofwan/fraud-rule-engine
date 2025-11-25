using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraudRuleEngine.Reporting.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fraud_rule_heatmap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RuleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TriggerCount = table.Column<int>(type: "integer", nullable: false),
                    AverageRiskScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fraud_rule_heatmap", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fraud_summary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FraudCheckId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsFlagged = table.Column<bool>(type: "boolean", nullable: false),
                    OverallRiskScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fraud_summary", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fraud_rule_heatmap_RuleName_Date",
                table: "fraud_rule_heatmap",
                columns: new[] { "RuleName", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fraud_rule_heatmap_RuleName_TriggerCount_AverageRiskScore_D~",
                table: "fraud_rule_heatmap",
                columns: new[] { "RuleName", "TriggerCount", "AverageRiskScore", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_fraud_summary_EvaluatedAt",
                table: "fraud_summary",
                column: "EvaluatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_fraud_summary_EvaluatedAt_IsFlagged_OverallRiskScore",
                table: "fraud_summary",
                columns: new[] { "EvaluatedAt", "IsFlagged", "OverallRiskScore" });

            migrationBuilder.CreateIndex(
                name: "IX_fraud_summary_FraudCheckId",
                table: "fraud_summary",
                column: "FraudCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_fraud_summary_TransactionId",
                table: "fraud_summary",
                column: "TransactionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fraud_rule_heatmap");

            migrationBuilder.DropTable(
                name: "fraud_summary");
        }
    }
}
