using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionsIngest.Domain.Constants;
using TransactionsIngest.Domain.Entities;

namespace TransactionsIngest.Infrastructure.Persistence.Configurations;

internal sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("ImportBatches");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Trigger).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.Error).HasMaxLength(FieldLimits.MaxImportErrorLength);

        builder.HasIndex(x => x.StartedAtUtc);
    }
}
