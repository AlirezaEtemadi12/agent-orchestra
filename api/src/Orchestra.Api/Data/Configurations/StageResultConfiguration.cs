using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Data.Configurations;

public sealed class StageResultConfiguration : IEntityTypeConfiguration<StageResultEntity>
{
    public void Configure(EntityTypeBuilder<StageResultEntity> builder)
    {
        builder.ToTable("StageResult");
        builder.HasKey(stage => stage.Id);

        builder.Property(stage => stage.Stage).HasConversion<string>().HasMaxLength(20);
        builder.Property(stage => stage.Adapter).HasMaxLength(40);
        builder.Property(stage => stage.Model).HasMaxLength(120);
        builder.Property(stage => stage.Verdict).HasMaxLength(40);
        builder.Property(stage => stage.GatesNote).HasMaxLength(2000);
        builder.Property(stage => stage.ModelVerdict).HasMaxLength(40);

        builder.HasOne(stage => stage.Run)
            .WithMany(run => run.Stages)
            .HasForeignKey(stage => stage.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(stage => new { stage.RunId, stage.Stage });
    }
}
