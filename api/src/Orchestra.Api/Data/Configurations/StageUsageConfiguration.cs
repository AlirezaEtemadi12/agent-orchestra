using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Data.Configurations;

public sealed class StageUsageConfiguration : IEntityTypeConfiguration<StageUsageEntity>
{
    public void Configure(EntityTypeBuilder<StageUsageEntity> builder)
    {
        builder.ToTable("StageUsage");
        builder.HasKey(usage => usage.Id);

        builder.Property(usage => usage.Stage).HasMaxLength(40).IsRequired();
        builder.Property(usage => usage.Model).HasMaxLength(120);

        builder.HasOne(usage => usage.Run)
            .WithMany(run => run.StageUsage)
            .HasForeignKey(usage => usage.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
