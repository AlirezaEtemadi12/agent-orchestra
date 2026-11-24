# `.ai/` — knowledge base for Orchestra

The single source of truth for **how** this repository is built and **why**, for any agent. Every rule
lives in exactly one doc; `AGENTS.md` and `README.md` only point at it. If two docs disagree, the one
marked **AUTHORITATIVE** for that topic wins — fix the other.

The orchestrated way of working — the gates, ideas, research, previews, plans, prompts — is in
[`../.orchestra/`](../.orchestra/README.md) ([ADR 0005](decisions/0005-ai-is-knowledge-orchestra-is-work.md)).

## Start here

1. This index.
2. The newest [`context/session-*.md`](context/) — what the last session left open.
3. [`../.orchestra/workflow.md`](../.orchestra/workflow.md) — the four gates (**AUTHORITATIVE**).
4. The docs your task touches (table below). A plan in [`../.orchestra/plans/`](../.orchestra/plans/README.md) lists exactly which.

## Doing X → read Y

| Doing | Read |
|---|---|
| Anything at all, first time here | [`vision/roadmap.md`](vision/roadmap.md), [`architecture/overview.md`](architecture/overview.md) |
| How the pieces talk (runner, the desktop window, API, UI, security) | [`architecture/overview.md`](architecture/overview.md) — **AUTHORITATIVE** |
| Tables, what is stored, what is imported | [`architecture/data-model.md`](architecture/data-model.md) |
| Why something is the way it is | [`decisions/`](decisions/README.md) |
| Code in `api/` | [`api/conventions.md`](api/conventions.md), [`api/naming.md`](api/naming.md), [`api/testing.md`](api/testing.md) |
| Code in `ui/` | [`ui/conventions.md`](ui/conventions.md), [`ui/vertical-slice.md`](ui/vertical-slice.md), [`ui/i18n.md`](ui/i18n.md), [`ui/design.md`](ui/design.md), [`ui/testing.md`](ui/testing.md) |
| Code in `engine/` (PowerShell) | [`engine/conventions.md`](engine/conventions.md), [`../engine/README.md`](../engine/README.md) |
| The commands that must be green | [`ci.md`](ci.md) |
| Adding an idea, a plan, a preview | [`../.orchestra/`](../.orchestra/README.md) |
| Ending a session | [`context/README.md`](context/README.md) |

The docs above were trimmed from Project B's equivalents; each names its source. That project is
read-only from here (`../Project ProjectB/Web/.ai/` for the API, `../Project ProjectB/Ui/.ai/`
for the UI) and its copies here win where they differ.

## Folder map

```
.ai/
├── README.md          ← this index
├── vision/            ← roadmap.md (phases)
├── architecture/      ← overview.md and data-model.md
├── decisions/         ← ADRs — one decision each, amended or superseded, never rewritten
├── api/               ← rules for api/ code
├── ui/                ← rules for ui/ code
├── engine/            ← rules for engine/ code (PowerShell 5.1)
├── ci.md              ← the gates for both
└── context/           ← per-session logs (a log, not a rule book)
```
