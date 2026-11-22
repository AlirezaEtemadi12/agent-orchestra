using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Data.Configurations;

public sealed class SessionUsageConfiguration : IEntityTypeConfiguration<SessionUsageEntity>
{
    public void Configure(EntityTypeBuilder<SessionUsageEntity> builder)
    {
        builder.ToTable("SessionUsage");
        builder.HasKey(session => session.Id);

        builder.Property(session => session.SessionId).HasMaxLength(80).IsRequired();
        builder.Property(session => session.FirstRole).HasMaxLength(40);
        builder.Property(session => session.Model).HasMaxLength(120);

        builder.HasOne(session => session.Run)
            .WithMany(run => run.Sessions)
            .HasForeignKey(session => session.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
