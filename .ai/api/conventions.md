# API conventions

> **AUTHORITATIVE** for `api/`. Trimmed from Project B's `Web/.ai/coding/conventions.md`; where this
> file and that one differ, this file wins.

## Shape

- `Program.cs` is four lines: `CreateBuilder(args).ConfigureServices().Build().ConfigureApp().Run()`,
  inside `try/catch/finally`. The `catch` writes the exception to `Console.Error` **and rethrows**:
  startup must fail loudly.
- One module = one folder under `Modules/<Module>/`, one `IModuleInstaller` with a sparse `Order`
  (Data 10, Projects 20, Import 30, Runs 40, Usage 50, Library 60). Installers are found by reflection, so a new
  module needs no registration line.
- A controller only routes: it validates query parameters, calls one handler and returns its result.
  Handlers hold the query; DTOs live beside their feature.
- Routes come from `ApiRoutes` (`Base = "api"`, `Controller = Base + "/[controller]"`). A literal
  `"api"` in a route is a bug.

## Errors

- Errors are `record Error(string Code, int Status)`; codes are `<Module>.<Thing>.<Reason>`.
- Every code has an English and a Persian text in `ErrorMessages`, and the response is
  `ProblemDetails` from `ProblemResults.From`, carrying `extensions.code`.
- The language is chosen from `Accept-Language`: Persian only when the request prefers `fa`.
- Unhandled exceptions go through `GlobalExceptionMiddleware` → `Api.Unexpected`. There is **no**
  `UseExceptionHandler` (its default response contradicts the localized `ProblemDetails`).

## Data and time

- `.ai/architecture/data-model.md` is the data model. The importer implements it, not a variant.
- Times are `DateTime` with `Kind = Utc`. `DateTimeOffset` is not used: EF's SQLite provider cannot
  order or compare it in SQL.
- Money is `long CostMicros`; DTOs carry `decimal` USD. A missing cost is `null`, never `0`.
- Every clock goes through the registered `TimeProvider`; `DateTime.Now`/`DateTime.UtcNow` outside
  `SourceClock` and the import log stamping is a bug (there is a test for it).

## Files

- Every project folder is read-only: `FileAccess.Read` with
  `FileShare.ReadWrite | FileShare.Delete`, because the orchestrator may be writing.
- Nothing is written into a project folder, ever.
- The dashboard's own files (`projects.json`, beside the database) are written atomically: a temp
  file, then a replace. A file that does not parse is reported and never overwritten.

## Dependencies

- No MediatR, no FluentValidation, no Serilog, no AutoMapper, no Swagger page. Package versions are
  central (`Directory.Packages.props`) and exact.
- A `--` inside an XML comment voids the file; use `—`.
