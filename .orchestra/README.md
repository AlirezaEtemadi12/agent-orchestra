# `.orchestra/` — the orchestrator's working folder

Everything the orchestrated way of working produces or needs for **this** project. The project's
knowledge (architecture, decisions, conventions, session logs) is in [`../.ai/`](../.ai/README.md),
which any agent reads, orchestrated or not. Why two folders:
[ADR 0005](../.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md).

| Path | What | In git |
|---|---|---|
| [`workflow.md`](workflow.md) | the four gates — **AUTHORITATIVE** | yes |
| [`ideas/`](ideas/README.md) | one file per raw idea, until it becomes a plan | yes |
| [`research/`](research/) | `YYYY-MM-DD-<topic>.md`: sources looked at, comparisons, conclusion | yes |
| [`plans/`](plans/README.md) | `NNNN-<topic>/`: `plan.md`, its `preview.html`, and its git-ignored `evidence/` | yes |
| [`prompts/`](prompts/) | paste-blocks that start a session or a role | yes |
| [`skills/`](skills/) | `<name>/SKILL.md`: orchestrate, close-plan, pick-plan (pointers in `.claude/skills/`) | yes |
| `plans/NNNN-<topic>/evidence/` | `phase-<X>/`: an agent's screenshots and check reports | **no** |
| `runs/` | the orchestrator's run folders and ledger (phase 2, when the engine moves here) | **no** |

Not yet here: `project.json` (this project's overrides of the shared profile) and `roles/` — they
arrive with the engine in phase 2 ([`../.ai/vision/roadmap.md`](../.ai/vision/roadmap.md)).
