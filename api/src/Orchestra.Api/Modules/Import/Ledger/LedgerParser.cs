using System.Globalization;
using System.Text.RegularExpressions;
using Orchestra.Api.Modules.Runs.Entities;

namespace Orchestra.Api.Modules.Import.Ledger;

public static partial class LedgerParser
{
    public static LedgerParseResult Parse(string text, string file)
    {
        var result = new LedgerParseResult();
        var lines = Split(text);

        LedgerBlock? block = null;
        string? failure = null;
        var failureLine = 0;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            var heading = Header().Match(line);

            if (heading.Success)
            {
                Finish(block, failure, failureLine, result);

                block = new LedgerBlock
                {
                    File = file,
                    Heading = line,
                    LocalTime = DateTime.ParseExact(
                        heading.Groups[1].Value,
                        "yyyy-MM-dd HH:mm",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None),
                    TaskId = heading.Groups[2].Value,
                    Kind = heading.Groups[3].Value,
                };

                failure = null;
                continue;
            }

            if (block is null || line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                index = SkipFence(lines, index);
                continue;
            }

            if (line.StartsWith('|'))
            {
                index = ReadTable(lines, index, block, ref failure, ref failureLine);
                continue;
            }

            var repo = RepoLine().Match(line);

            if (repo.Success)
            {
                block.Repo = repo.Groups[1].Value;
                block.Status = repo.Groups[2].Value;
                block.PlanVersion = int.Parse(repo.Groups[3].Value, CultureInfo.InvariantCulture);
                block.Branch = repo.Groups[4].Value;
                continue;
            }

            var review = ReviewLine().Match(line);

            if (review.Success)
            {
                var (adapter, model) = ModelName.Split(review.Groups[6].Value);

                block.Stages.Add(new StageRow
                {
                    Stage = review.Groups[1].Value == "review" ? StageKind.Review : StageKind.ReadBack,
                    Verdict = review.Groups[2].Value,
                    Blockers = int.Parse(review.Groups[3].Value, CultureInfo.InvariantCulture),
                    Should = int.Parse(review.Groups[4].Value, CultureInfo.InvariantCulture),
                    Nits = int.Parse(review.Groups[5].Value, CultureInfo.InvariantCulture),
                    Adapter = adapter,
                    Model = model,
                    Round = int.Parse(review.Groups[7].Value, CultureInfo.InvariantCulture),
                });

                continue;
            }

            var phase = PhaseLine().Match(line);

            if (phase.Success)
            {
                var (adapter, model) = ModelName.Split(phase.Groups[3].Value);

                block.Stages.Add(new StageRow
                {
                    Stage = StageKind.Implement,
                    Phase = int.Parse(phase.Groups[1].Value, CultureInfo.InvariantCulture),
                    Verdict = phase.Groups[2].Value,
                    Adapter = adapter,
                    Model = model,
                    Changed = int.Parse(phase.Groups[4].Value, CultureInfo.InvariantCulture),
                    OutsidePlan = int.Parse(phase.Groups[5].Value, CultureInfo.InvariantCulture),
                    Violations = int.Parse(phase.Groups[6].Value, CultureInfo.InvariantCulture),
                });

                continue;
            }

            var verify = VerifyLine().Match(line);

            if (verify.Success)
            {
                var (adapter, model) = ModelName.Split(verify.Groups[5].Value);

                block.Stages.Add(new StageRow
                {
                    Stage = StageKind.Verify,
                    Verdict = verify.Groups[1].Value,
                    GatesGreen = verify.Groups[2].Value == "green",
                    GatesNote = verify.Groups[3].Success ? verify.Groups[3].Value.Trim() : null,
                    ModelVerdict = verify.Groups[4].Value,
                    Adapter = adapter,
                    Model = model,
                });
            }
        }

        Finish(block, failure, failureLine, result);

        return result;
    }

    private static void Finish(LedgerBlock? block, string? failure, int failureLine, LedgerParseResult result)
    {
        if (block is null)
        {
            return;
        }

        if (failure is not null)
        {
            result.Errors.Add(new LedgerError(block.File, block.Heading, failureLine, failure));
            return;
        }

        if (block.Repo is null)
        {
            result.Errors.Add(new LedgerError(block.File, block.Heading, 0, "The block has no repo line."));
            return;
        }

        result.Blocks.Add(block);
    }

    private static int ReadTable(
        string[] lines,
        int start,
        LedgerBlock block,
        ref string? failure,
        ref int failureLine)
    {
        var header = Cells(lines[start]);

        if (header.Count == 0)
        {
            return start;
        }

        var first = header[0].ToLowerInvariant();

        if (first is not ("session" or "stage"))
        {
            return start;
        }

        var isSession = first == "session";
        var index = start + 1;

        if (index < lines.Length && IsSeparator(lines[index]))
        {
            index++;
        }

        for (; index < lines.Length; index++)
        {
            var line = lines[index].Trim();

            if (line.Length == 0 || !line.StartsWith('|'))
            {
                break;
            }

            var cells = Cells(line);

            if (cells.Count != header.Count)
            {
                failure = $"A table row has {cells.Count} cells where its header has {header.Count}.";
                failureLine = index + 1;
                return index;
            }

            if (cells[0].Trim('*', ' ').Equals("total", StringComparison.OrdinalIgnoreCase))
            {
                block.TotalCostMicros ??= Money(header, cells, "cost");
                continue;
            }

            if (isSession)
            {
                block.Sessions.Add(new SessionUsageEntity
                {
                    Id = Guid.NewGuid(),
                    SessionId = cells[0],
                    FirstRole = Text(header, cells, "first role"),
                    Model = ShortModel(Text(header, cells, "model")),
                    Calls = Count(header, cells, "calls"),
                    Requests = Count(header, cells, "requests"),
                    Input = Tokens(header, cells, "input"),
                    Output = Tokens(header, cells, "output"),
                    Reasoning = Tokens(header, cells, "reasoning"),
                    CacheRead = Tokens(header, cells, "cache read"),
                    CostMicros = Money(header, cells, "cost"),
                });
            }
            else
            {
                block.StageUsage.Add(new StageUsageEntity
                {
                    Id = Guid.NewGuid(),
                    Stage = cells[0],
                    Model = ShortModel(Text(header, cells, "model")),
                    Sessions = Count(header, cells, "sessions"),
                    Calls = Count(header, cells, "calls"),
                    Requests = Count(header, cells, "requests"),
                    Input = Tokens(header, cells, "input"),
                    Output = Tokens(header, cells, "output"),
                    Reasoning = Tokens(header, cells, "reasoning"),
                    CacheRead = Tokens(header, cells, "cache read"),
                    Seconds = Duration(header, cells, "time"),
                    CostMicros = Money(header, cells, "cost"),
                });
            }
        }

        return index - 1;
    }

    private static string? Text(IReadOnlyList<string> header, IReadOnlyList<string> cells, string column)
    {
        var position = IndexOf(header, column);

        return position < 0 ? null : cells[position];
    }

    private static int? Count(IReadOnlyList<string> header, IReadOnlyList<string> cells, string column)
    {
        return LedgerNumbers.Count(Text(header, cells, column));
    }

    private static long? Tokens(IReadOnlyList<string> header, IReadOnlyList<string> cells, string column)
    {
        return LedgerNumbers.Tokens(Text(header, cells, column));
    }

    private static long? Money(IReadOnlyList<string> header, IReadOnlyList<string> cells, string column)
    {
        return LedgerNumbers.Money(Text(header, cells, column));
    }

    private static int? Duration(IReadOnlyList<string> header, IReadOnlyList<string> cells, string column)
    {
        return LedgerNumbers.Duration(Text(header, cells, column));
    }

    private static string? ShortModel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return ModelName.Split(raw).Model;
    }

    private static int IndexOf(IReadOnlyList<string> header, string column)
    {
        for (var index = 0; index < header.Count; index++)
        {
            if (header[index].Equals(column, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsSeparator(string line)
    {
        var trimmed = line.Trim();

        return trimmed.StartsWith('|')
            && trimmed.All(character => character is '|' or '-' or ':' or ' ');
    }

    private static List<string> Cells(string line)
    {
        var trimmed = line.Trim();

        if (trimmed.StartsWith('|'))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.EndsWith('|'))
        {
            trimmed = trimmed[..^1];
        }

        return [.. trimmed.Split('|').Select(cell => cell.Trim())];
    }

    private static int SkipFence(string[] lines, int start)
    {
        for (var index = start + 1; index < lines.Length; index++)
        {
            if (lines[index].Trim().StartsWith("```", StringComparison.Ordinal))
            {
                return index;
            }
        }

        return lines.Length;
    }

    private static string[] Split(string text)
    {
        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            text = text[1..];
        }

        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
    }

    [GeneratedRegex(@"^## (\d{4}-\d{2}-\d{2} \d{2}:\d{2}) - ([A-Z]+-[A-Z0-9]+-\d+) - (verify|close)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex Header();

    [GeneratedRegex(@"^repo (\S+) \| status (\S+) \| plan v(\d+) \| branch (\S+)$", RegexOptions.CultureInvariant)]
    private static partial Regex RepoLine();

    [GeneratedRegex(@"^- (review|read-back): (\w+) \(blocker (\d+), should (\d+), nit (\d+)\) by (\S+) \(round (\d+)\)$", RegexOptions.CultureInvariant)]
    private static partial Regex ReviewLine();

    [GeneratedRegex(@"^- phase (\d+): (\w+) by (\S+) \| changed (\d+) \| not in plan (\d+) \| violations (\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex PhaseLine();

    [GeneratedRegex(@"^- verify: (\w+) \(gates (green|red)(?:: ([^,]+))?, model said (\w+)\) by (\S+)$", RegexOptions.CultureInvariant)]
    private static partial Regex VerifyLine();
}
