using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Data.Configurations;

public sealed class RunConfiguration : IEntityTypeConfiguration<RunEntity>
{
    public void Configure(EntityTypeBuilder<RunEntity> builder)
    {
        builder.ToTable("Run");
        builder.HasKey(run => run.Id);

        builder.Property(run => run.ProjectId).HasMaxLength(40).IsRequired();
        builder.Property(run => run.RunKey).HasMaxLength(80).IsRequired();
        builder.Property(run => run.TaskId).HasMaxLength(80).IsRequired();
        builder.Property(run => run.Repo).HasMaxLength(200);
        builder.Property(run => run.Task).HasMaxLength(1000);
        builder.Property(run => run.Status).HasMaxLength(40).IsRequired();
        builder.Property(run => run.Branch).HasMaxLength(200);
        builder.Property(run => run.PlanNumber).HasMaxLength(10);
        builder.Property(run => run.PlanTitle).HasMaxLength(300);
        builder.Property(run => run.Source).HasConversion<string>().HasMaxLength(20);
        builder.Property(run => run.SourceRef).HasMaxLength(500);

        // The merged config is a whole profile plus overrides; TEXT, never truncated.
        builder.Property(run => run.EffectiveConfigJson);

        builder.HasOne(run => run.Project)
            .WithMany(project => project.Runs)
            .HasForeignKey(run => run.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(run => new { run.ProjectId, run.RunKey }).IsUnique();
        builder.HasIndex(run => new { run.ProjectId, run.Status });
    }
}
