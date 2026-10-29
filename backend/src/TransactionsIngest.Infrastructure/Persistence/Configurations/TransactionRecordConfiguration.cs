using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionsIngest.Domain.Constants;
using TransactionsIngest.Domain.Entities;

namespace TransactionsIngest.Infrastructure.Persistence.Configurations;

internal sealed class TransactionRecordConfiguration : IEntityTypeConfiguration<TransactionRecord>
{
    public void Configure(EntityTypeBuilder<TransactionRecord> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceSystem).HasMaxLength(FieldLimits.MaxStringLength).IsRequired();
        builder.Property(x => x.CardNumber).HasMaxLength(FieldLimits.MaxStringLength);
        builder.Property(x => x.LocationCode).HasMaxLength(FieldLimits.MaxStringLength).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(FieldLimits.MaxStringLength).IsRequired();

        // SQLite can't ORDER BY / compare decimals (EF stores them as TEXT), so keep cents as INTEGER.
        // Values are rounded to 2 decimals before they get here, so no precision is lost.
        builder.Property(x => x.Amount)
            .HasConversion(v => (long)(v * 100m), v => v / 100m)
            .HasColumnName("AmountCents");

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);

        builder.Property(x => x.RevisionNumber).IsConcurrencyToken();

        builder.HasIndex(x => new { x.SourceSystem, x.TransactionId }).IsUnique();
        builder.HasIndex(x => new { x.Status, x.TransactionTimeUtc });

        builder.HasMany(x => x.AuditEntries)
            .WithOne(a => a.Transaction)
            .HasForeignKey(a => a.TransactionRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
