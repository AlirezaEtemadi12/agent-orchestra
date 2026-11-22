using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestra.Api.Modules.Import;

namespace Orchestra.Api.Data.Configurations;

public sealed class ImportLogConfiguration : IEntityTypeConfiguration<ImportLogEntity>
{
    public void Configure(EntityTypeBuilder<ImportLogEntity> builder)
    {
        builder.ToTable("ImportLog");
        builder.HasKey(log => log.Id);

        // No foreign key on purpose: the log outlives a project removed from the configuration.
        builder.Property(log => log.ProjectId).HasMaxLength(40).IsRequired();

        builder.HasIndex(log => new { log.ProjectId, log.StartedAt });
    }
}
