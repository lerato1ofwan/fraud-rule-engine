using FraudRuleEngine.Transactions.Api.Domain.Entities;
using FraudRuleEngine.Transactions.Api.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FraudRuleEngine.Transactions.Api.Data;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<TransactionIngestAudit> TransactionIngestAudits { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionId).IsRequired();
            entity.HasIndex(e => e.TransactionId).IsUnique();
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.Property(e => e.AccountId).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.MerchantId).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.ExternalId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.OwnsOne(e => e.Metadata, metadata =>
            {
                metadata.Property(m => m.Data)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new())
                    .HasColumnName("metadata");
            });
        });

        modelBuilder.Entity<TransactionIngestAudit>(entity =>
        {
            entity.ToTable("transaction_ingest_audit");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionId).IsRequired();
            entity.Property(e => e.ExternalId).IsRequired();
            entity.Property(e => e.IngestedAt).IsRequired();
            entity.HasIndex(e => e.ExternalId);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired();
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ProcessedAt).IsRequired(false);
            entity.HasIndex(e => e.ProcessedAt);
        });
    }
}

