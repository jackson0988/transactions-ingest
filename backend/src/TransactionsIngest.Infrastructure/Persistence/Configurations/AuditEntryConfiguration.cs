using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionsIngest.Domain.Constants;
using TransactionsIngest.Domain.Entities;

namespace TransactionsIngest.Infrastructure.Persistence.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChangeType).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.FieldName).HasMaxLength(64);
        builder.Property(x => x.OldValue).HasMaxLength(FieldLimits.MaxAuditValueLength);
        builder.Property(x => x.NewValue).HasMaxLength(FieldLimits.MaxAuditValueLength);

        builder.HasOne<ImportBatch>()
            .WithMany()
            .HasForeignKey(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TransactionRecordId, x.ChangedAtUtc });
    }
}
