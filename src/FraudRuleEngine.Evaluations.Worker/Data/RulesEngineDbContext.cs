using FraudRuleEngine.Evaluations.Worker.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FraudRuleEngine.Evaluations.Worker.Data;

public class RulesEngineDbContext : DbContext
{
    public RulesEngineDbContext(DbContextOptions<RulesEngineDbContext> options) : base(options)
    {
    }

    public DbSet<FraudCheck> FraudChecks { get; set; } = null!;
    public DbSet<FraudRuleMetadata> FraudRulesMetadata { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FraudCheck>(entity =>
        {
            entity.ToTable("fraud_checks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FraudCheckId).IsRequired();
            entity.HasIndex(e => e.FraudCheckId).IsUnique();
            entity.HasIndex(e => new { e.AccountId, e.EvaluatedAt }).IsUnique();
            entity.HasIndex(e => e.TransactionId);
            entity.Property(e => e.TransactionId).IsRequired();
            entity.Property(e => e.IsFlagged).IsRequired();
            entity.Property(e => e.OverallRiskScore).HasPrecision(5, 2).IsRequired();
            entity.Property(e => e.EvaluatedAt).IsRequired();

            entity.OwnsMany(e => e.RuleResults, rr =>
            {
                rr.ToTable("fraud_rule_results");
                rr.WithOwner().HasForeignKey("FraudCheckId");
                rr.Property<int>("Id");
                rr.HasKey("Id");
                rr.Property(r => r.RuleName).IsRequired().HasMaxLength(100);
                rr.Property(r => r.Triggered).IsRequired();
                rr.Property(r => r.RiskScore).HasPrecision(5, 2).IsRequired();
                rr.Property(r => r.Reason).HasMaxLength(500);
            });
        });

        modelBuilder.Entity<FraudRuleMetadata>(entity =>
        {
            entity.ToTable("fraud_rules_metadata");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleName).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.RuleName).IsUnique();
            entity.Property(e => e.IsEnabled).IsRequired();
            entity.Property(e => e.Configuration).HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new())
                .HasColumnName("configuration");
        });
    }
}
