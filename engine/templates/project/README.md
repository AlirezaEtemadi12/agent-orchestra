# .orchestra/ — the orchestrated work

This folder is the orchestrator's own workspace: the four gates, the plans, and the evidence they
leave. The engine that runs it lives in Orchestra; this folder is what moves with the project.

| Folder | Holds |
|---|---|
| `plans/` | One folder per plan, `NNNN-topic/`: `plan.md` with its status and dates, its `preview.html`, its git-ignored `evidence/`. `TEMPLATE.md` is the shape. |
| `ideas/` | One file per idea that is not yet a plan, with a status. |
| `research/` | What an agent read for a plan: links, quotes, notes. |
| `prompts/` | The stage prompts each role receives, one file per role. |
| `roles/` | What each role is for, as `## <role>` sections. |
| `skills/` | One folder per skill, `<name>/SKILL.md`: orchestrate, close-plan, pick-plan. Setup also writes a three-line pointer to each in the project's `.claude/skills/<name>/SKILL.md`, from the template's `claude-skills/` folder. |
| `runs/` | The engine's live runs. Git-ignored. |

`workflow.md` is the four gates. `orchestrator.json` is what the engine reads. `orchestrator.local.json`
holds this machine's paths and the report token: it is git-ignored and written by Orchestra.
