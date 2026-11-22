using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Orchestra.Api.Modules.Events;
using Orchestra.Api.Modules.Import;
using Orchestra.Api.Modules.Library;
using Orchestra.Api.Modules.Profiles;
using Orchestra.Api.Modules.Projects;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Data;

public sealed class OrchestraDbContext(DbContextOptions<OrchestraDbContext> options) : DbContext(options)
{
    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();

    public DbSet<RunEntity> Runs => Set<RunEntity>();

    public DbSet<StageTextEntity> StageTexts => Set<StageTextEntity>();

    public DbSet<StageResultEntity> StageResults => Set<StageResultEntity>();

    public DbSet<SessionUsageEntity> SessionUsages => Set<SessionUsageEntity>();

    public DbSet<StageUsageEntity> StageUsages => Set<StageUsageEntity>();

    public DbSet<ImportLogEntity> ImportLogs => Set<ImportLogEntity>();

    public DbSet<ImportErrorEntity> ImportErrors => Set<ImportErrorEntity>();

    public DbSet<LibraryItemEntity> LibraryItems => Set<LibraryItemEntity>();

    public DbSet<RunEventEntity> RunEvents => Set<RunEventEntity>();

    public DbSet<ProfileEntity> Profiles => Set<ProfileEntity>();

    public DbSet<ProfileVersionEntity> ProfileVersions => Set<ProfileVersionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrchestraDbContext).Assembly);

        // SQLite stores no offset and no kind: everything comes back as UTC.
        var utc = new ValueConverter<DateTime, DateTime>(
            value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        var nullableUtc = new ValueConverter<DateTime?, DateTime?>(
            value => value.HasValue ? value.Value.Kind == DateTimeKind.Utc ? value : value.Value.ToUniversalTime() : value,
            value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utc);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableUtc);
                }
            }
        }
    }
}
