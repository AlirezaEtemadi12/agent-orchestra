using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestra.Api.Modules.Library;

namespace Orchestra.Api.Data.Configurations;

public sealed class LibraryItemConfiguration : IEntityTypeConfiguration<LibraryItemEntity>
{
    public void Configure(EntityTypeBuilder<LibraryItemEntity> builder)
    {
        builder.ToTable("LibraryItem");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.ProjectId).HasMaxLength(40).IsRequired();
        builder.Property(item => item.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(item => item.RelPath).HasMaxLength(500).IsRequired();
        builder.Property(item => item.Title).HasMaxLength(300).IsRequired();
        builder.Property(item => item.Number).HasMaxLength(10);
        builder.Property(item => item.Slug).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Repo).HasMaxLength(200);
        builder.Property(item => item.Status).HasMaxLength(20);
        builder.Property(item => item.PlannedBy).HasMaxLength(120);
        builder.Property(item => item.PreviewRelPath).HasMaxLength(500);
        builder.Property(item => item.LinkedPlanNumber).HasMaxLength(10);
        builder.Property(item => item.RunKey).HasMaxLength(80);
        builder.Property(item => item.Group).HasMaxLength(300);

        // A project removed from the configuration takes its library with it — the rows are a read
        // model rebuilt from files (plan 0001 D3).
        builder.HasOne(item => item.Project)
            .WithMany()
            .HasForeignKey(item => item.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => new { item.ProjectId, item.Category, item.RelPath }).IsUnique();
        builder.HasIndex(item => new { item.ProjectId, item.Category });
    }
}
