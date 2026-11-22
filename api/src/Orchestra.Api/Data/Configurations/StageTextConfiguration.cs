using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Data.Configurations;

public sealed class StageTextConfiguration : IEntityTypeConfiguration<StageTextEntity>
{
    public void Configure(EntityTypeBuilder<StageTextEntity> builder)
    {
        builder.ToTable("StageText");
        builder.HasKey(text => text.Id);

        builder.Property(text => text.Stage).HasMaxLength(40).IsRequired();
        builder.Property(text => text.Name).HasMaxLength(120).IsRequired();
        builder.Property(text => text.Text).HasMaxLength(StageTextEntity.MaxTextLength).IsRequired();

        builder.HasOne(text => text.Run)
            .WithMany(run => run.Texts)
            .HasForeignKey(text => text.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
