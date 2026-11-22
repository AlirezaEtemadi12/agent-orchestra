using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestra.Api.Modules.Projects;

namespace Orchestra.Api.Data.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<ProjectEntity>
{
    public void Configure(EntityTypeBuilder<ProjectEntity> builder)
    {
        builder.ToTable("Project");
        builder.HasKey(project => project.Id);

        builder.Property(project => project.Id).HasMaxLength(40);
        builder.Property(project => project.Name).HasMaxLength(120).IsRequired();
        builder.Property(project => project.RunsPath).HasMaxLength(500).IsRequired();
    }
}
