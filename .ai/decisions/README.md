# Architecture decision records

One decision per file. An ADR is never rewritten; it is **amended** (a dated note at the top) or
**superseded** by a new one that links back. Template: Status · Date · Decided by · Context · Decision ·
Consequences.

| # | Decision |
|---|---|
| [0001](0001-one-repo-dotnet-nextjs-sqlite.md) | One repo; .NET 10 API + SQLite; Next.js static export served by the API; one container |
| [0002](0002-runner-reports-dashboard-never-commands.md) | The runner reports to the dashboard, fire-and-forget; the dashboard never commands the runner |
| [0003](0003-read-only-dashboard-first.md) | Read-only first: the dashboard shows and compares, it does not approve or start runs |
| [0004](0004-persian-and-english.md) | Persian and English from day one; locale routing in a static export; browser time zone |
| [0005](0005-ai-is-knowledge-orchestra-is-work.md) | `.ai/` is the knowledge, `.orchestra/` is the work; files are the truth; the dashboard never writes into a project |
| [0006](0006-events-are-the-dashboards-own-record.md) | Events reported by the runner are the dashboard's own record: never derived, never deleted by an import |
| [0007](0007-profiles-exported-to-a-file.md) | Profiles are the dashboard's own data, versioned and exported to `<data>/profiles/<id>.json`; the engine merges that file, never the API |
