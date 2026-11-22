using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestra.Api.Modules.Profiles;

namespace Orchestra.Api.Data.Configurations;

public sealed class ProfileConfiguration : IEntityTypeConfiguration<ProfileEntity>
{
    public void Configure(EntityTypeBuilder<ProfileEntity> builder)
    {
        builder.ToTable("Profile");
        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Id).HasMaxLength(40);
        builder.Property(profile => profile.Name).HasMaxLength(200).IsRequired();
    }
}

public sealed class ProfileVersionConfiguration : IEntityTypeConfiguration<ProfileVersionEntity>
{
    public void Configure(EntityTypeBuilder<ProfileVersionEntity> builder)
    {
        builder.ToTable("ProfileVersion");
        builder.HasKey(version => version.Id);

        builder.Property(version => version.ProfileId).HasMaxLength(40).IsRequired();
        builder.Property(version => version.Json).IsRequired();
        builder.Property(version => version.Reason).HasMaxLength(300).IsRequired();

        builder.HasOne(version => version.Profile)
            .WithMany(profile => profile.Versions)
            .HasForeignKey(version => version.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // D4: a version number is unique per profile, and the history reads newest first.
        builder.HasIndex(version => new { version.ProfileId, version.Version }).IsUnique();
    }
}
