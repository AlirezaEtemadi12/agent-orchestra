using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestra.Api.Modules.Import;

namespace Orchestra.Api.Data.Configurations;

public sealed class ImportErrorConfiguration : IEntityTypeConfiguration<ImportErrorEntity>
{
    public void Configure(EntityTypeBuilder<ImportErrorEntity> builder)
    {
        builder.ToTable("ImportError");
        builder.HasKey(error => error.Id);

        builder.Property(error => error.File).HasMaxLength(500).IsRequired();
        builder.Property(error => error.Location).HasMaxLength(200);
        builder.Property(error => error.Message).HasMaxLength(2000).IsRequired();

        builder.HasOne(error => error.ImportLog)
            .WithMany(log => log.Errors)
            .HasForeignKey(error => error.ImportLogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
