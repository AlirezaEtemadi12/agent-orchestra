using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestra.Api.Modules.Events;

namespace Orchestra.Api.Data.Configurations;

public sealed class RunEventConfiguration : IEntityTypeConfiguration<RunEventEntity>
{
    public void Configure(EntityTypeBuilder<RunEventEntity> builder)
    {
        builder.ToTable("RunEvent");
        builder.HasKey(stored => stored.Id);

        builder.Property(stored => stored.ProjectId).HasMaxLength(40).IsRequired();
        builder.Property(stored => stored.TaskId).HasMaxLength(80).IsRequired();
        builder.Property(stored => stored.Branch).HasMaxLength(200);
        builder.Property(stored => stored.Kind).HasMaxLength(40).IsRequired();
        builder.Property(stored => stored.DataJson).IsRequired();

        // D11: the engine's GUID is the identity, so a resume or a second hook stores one row.
        builder.HasIndex(stored => stored.Id).IsUnique();

        // The board reads a project's events newest-last; the retention pass reads one kind by time.
        builder.HasIndex(stored => new { stored.ProjectId, stored.TaskId, stored.At });
        builder.HasIndex(stored => new { stored.Kind, stored.At });
    }
}
