using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraudRuleEngine.Evaluations.Worker.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fraud_checks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FraudCheckId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsFlagged = table.Column<bool>(type: "boolean", nullable: false),
                    OverallRiskScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fraud_checks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fraud_rules_metadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RuleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    configuration = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fraud_rules_metadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fraud_rule_results",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RuleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Triggered = table.Column<bool>(type: "boolean", nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FraudCheckId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fraud_rule_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fraud_rule_results_fraud_checks_FraudCheckId",
                        column: x => x.FraudCheckId,
                        principalTable: "fraud_checks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fraud_checks_FraudCheckId",
                table: "fraud_checks",
                column: "FraudCheckId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fraud_checks_TransactionId",
                table: "fraud_checks",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_fraud_rule_results_FraudCheckId",
                table: "fraud_rule_results",
                column: "FraudCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_fraud_rules_metadata_RuleName",
                table: "fraud_rules_metadata",
                column: "RuleName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fraud_rule_results");

            migrationBuilder.DropTable(
                name: "fraud_rules_metadata");

            migrationBuilder.DropTable(
                name: "fraud_checks");
        }
    }
}
