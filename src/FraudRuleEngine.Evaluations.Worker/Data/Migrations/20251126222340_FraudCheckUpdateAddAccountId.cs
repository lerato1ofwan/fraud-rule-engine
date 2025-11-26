using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraudRuleEngine.Evaluations.Worker.Data.Migrations
{
    /// <inheritdoc />
    public partial class FraudCheckUpdateAddAccountId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "fraud_checks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_fraud_checks_AccountId_EvaluatedAt",
                table: "fraud_checks",
                columns: new[] { "AccountId", "EvaluatedAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_fraud_checks_AccountId_EvaluatedAt",
                table: "fraud_checks");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "fraud_checks");
        }
    }
}
