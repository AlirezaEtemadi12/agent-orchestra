using Microsoft.EntityFrameworkCore;
using Orchestra.Api.Data;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Modules.Usage.Features.ModelQuality;

public sealed class ModelQualityHandler(OrchestraDbContext database)
{
    public async Task<ModelQualityDto> HandleAsync(string? project, CancellationToken cancellationToken)
    {
        var query = database.Runs
            .AsNoTracking()
            .Include(run => run.Stages)
            .Include(run => run.Sessions)
            .Include(run => run.StageUsage)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(project))
        {
            query = query.Where(run => run.ProjectId == project);
        }

        var runs = await query.ToListAsync(cancellationToken);

        var reviews = runs
            .SelectMany(run => run.Stages
                .Where(stage => stage.Stage == StageKind.Review)
                .Select(stage => (Run: run, Stage: stage)))
            .ToList();

        var readBacks = runs
            .SelectMany(run => run.Stages
                .Where(stage => stage.Stage == StageKind.ReadBack)
                .Select(stage => (Run: run, Stage: stage)))
            .ToList();

        return new ModelQualityDto(
            Reviewers(reviews),
            ReadBacks(readBacks),
            Implementers(runs),
            StageTimes(runs),
            Cache(runs));
    }

    private static IReadOnlyList<ReviewerQualityDto> Reviewers(
        IReadOnlyList<(RunEntity Run, StageResultEntity Stage)> rows)
    {
        return rows
            .Where(row => row.Stage.Model is not null)
            .GroupBy(row => row.Stage.Model!, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var finals = group.Where(row => row.Stage.IsFinal).ToList();
                var rounds = group.Where(row => !row.Stage.IsFinal).ToList();
                var runIds = finals.Select(row => row.Run.Id).Distinct().ToList();
                var runRounds = finals
                    .GroupBy(row => row.Run.Id)
                    .Select(byRun => byRun.First().Run.Rounds)
                    .Where(value => value is not null)
                    .Select(value => value!.Value)
                    .ToList();

                return new ReviewerQualityDto(
                    group.Key,
                    runIds.Count,
                    runRounds.Count == 0 ? null : runRounds.Average(),
                    runRounds.Count == 0 ? null : (double)runRounds.Count(value => value == 1) / runRounds.Count,
                    rounds.Count == 0
                        ? null
                        : (double)rounds.Count(row => row.Stage.Verdict == "FINDINGS") / rounds.Count,
                    finals.Sum(row => (long?)row.Stage.Blockers) ?? 0,
                    finals.Sum(row => (long?)row.Stage.Should) ?? 0,
                    finals.Sum(row => (long?)row.Stage.Nits) ?? 0);
            })
            .ToList();
    }

    private static IReadOnlyList<ReadBackQualityDto> ReadBacks(
        IReadOnlyList<(RunEntity Run, StageResultEntity Stage)> rows)
    {
        return rows
            .Where(row => row.Stage.Model is not null)
            .GroupBy(row => row.Stage.Model!, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var finals = group.Where(row => row.Stage.IsFinal).ToList();
                var runs = finals.Select(row => row.Run.Id).Distinct().Count();
                var findings = finals.Select(row => row.Run.Id).Distinct()
                    .Count(runId => finals.First(row => row.Run.Id == runId).Stage.Verdict != "PASS");

                return new ReadBackQualityDto(
                    group.Key,
                    runs,
                    runs == 0 ? null : (double)findings / runs,
                    finals.Sum(row => (long?)row.Stage.Blockers) ?? 0,
                    finals.Sum(row => (long?)row.Stage.Should) ?? 0,
                    finals.Sum(row => (long?)row.Stage.Nits) ?? 0);
            })
            .ToList();
    }

    private static IReadOnlyList<ImplementerQualityDto> Implementers(IReadOnlyList<RunEntity> runs)
    {
        var phases = runs
            .SelectMany(run => run.Stages
                .Where(stage => stage.Stage == StageKind.Implement && stage.Model is not null)
                .Select(stage => stage))
            .ToList();

        var verifies = runs
            .SelectMany(run => run.Stages
                .Where(stage => stage.Stage == StageKind.Verify && stage.Model is not null)
                .Select(stage => stage))
            .ToList();

        var models = phases.Select(stage => stage.Model!)
            .Concat(verifies.Select(stage => stage.Model!))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(model => model, StringComparer.Ordinal);

        return models.Select(model =>
        {
            var own = phases.Where(stage => stage.Model == model).ToList();
            var verify = verifies.Where(stage => stage.Model == model).ToList();

            return new ImplementerQualityDto(
                model,
                own.Count,
                own.Count(stage => stage.Verdict == "NEED_INPUT"),
                own.Count == 0 ? null : own.Average(stage => stage.Changed ?? 0),
                own.Sum(stage => (long?)stage.OutsidePlan) ?? 0,
                own.Sum(stage => (long?)stage.Violations) ?? 0,
                verify.Count,
                verify.Count == 0 ? null : (double)verify.Count(stage => stage.Verdict == "FAIL") / verify.Count);
        }).ToList();
    }

    private static IReadOnlyList<StageTimeDto> StageTimes(IReadOnlyList<RunEntity> runs)
    {
        return runs
            .SelectMany(run => run.StageUsage)
            .Where(usage => usage.Model is not null && usage.Seconds is not null)
            .GroupBy(usage => (Stage: usage.Stage, Model: usage.Model!), new StageModelComparer())
            .OrderBy(group => group.Key.Stage, StringComparer.Ordinal)
            .ThenBy(group => group.Key.Model, StringComparer.Ordinal)
            .Select(group => new StageTimeDto(
                group.Key.Stage,
                group.Key.Model,
                group.LongCount(),
                group.Average(usage => usage.Seconds ?? 0)))
            .ToList();
    }

    private static IReadOnlyList<CacheQualityDto> Cache(IReadOnlyList<RunEntity> runs)
    {
        return runs
            .SelectMany(run => run.Sessions)
            .Where(session => session.Model is not null)
            .GroupBy(session => session.Model!, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var input = group.Sum(session => session.Input) ?? 0;
                var cacheRead = group.Sum(session => session.CacheRead) ?? 0;
                var total = input + cacheRead;

                return new CacheQualityDto(
                    group.Key,
                    input,
                    cacheRead,
                    total == 0 ? null : (double)cacheRead / total);
            })
            .ToList();
    }

    private sealed class StageModelComparer : IEqualityComparer<(string Stage, string Model)>
    {
        public bool Equals((string Stage, string Model) x, (string Stage, string Model) y)
        {
            return x.Stage == y.Stage && x.Model == y.Model;
        }

        public int GetHashCode((string Stage, string Model) obj)
        {
            return HashCode.Combine(obj.Stage, obj.Model);
        }
    }
}
