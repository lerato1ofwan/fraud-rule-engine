using FraudRuleEngine.Reporting.Api.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FraudRuleEngine.Reporting.Api.Data;

public class FraudReportingDbContext : DbContext
{
    public FraudReportingDbContext(DbContextOptions<FraudReportingDbContext> options) : base(options)
    {
    }

    public DbSet<FraudSummary> FraudSummaries { get; set; }
    public DbSet<FraudRuleHeatmap> FraudRuleHeatmaps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FraudSummary>(entity =>
        {
            entity.ToTable("fraud_summary");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TransactionId).IsUnique();
            entity.HasIndex(e => e.FraudCheckId);
            entity.Property(e => e.TransactionId).IsRequired();
            entity.Property(e => e.FraudCheckId).IsRequired();
            entity.Property(e => e.IsFlagged).IsRequired();
            entity.Property(e => e.OverallRiskScore).HasPrecision(5, 2).IsRequired();
            entity.HasIndex(e => e.EvaluatedAt);
            entity.Property(e => e.EvaluatedAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => new { e.EvaluatedAt, e.IsFlagged, e.OverallRiskScore });
        });

        modelBuilder.Entity<FraudRuleHeatmap>(entity =>
        {
            entity.ToTable("fraud_rule_heatmap");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RuleName, e.Date }).IsUnique();
            entity.Property(e => e.RuleName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.TriggerCount).IsRequired();
            entity.Property(e => e.AverageRiskScore).HasPrecision(5, 2).IsRequired();
            entity.Property(e => e.LastUpdated).IsRequired();
            entity.HasIndex(e => new { e.RuleName, e.TriggerCount, e.AverageRiskScore, e.Date });
        });
    }
}

