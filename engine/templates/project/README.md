# .orchestra/ — the orchestrated work

This folder is the orchestrator's own workspace: the four gates, the plans, and the evidence they
leave. The engine that runs it lives in Orchestra; this folder is what moves with the project.

| Folder | Holds |
|---|---|
| `plans/` | One file per plan, `NNNN-topic.md`, with its status and dates. `TEMPLATE.md` is the shape. |
| `previews/` | One folder per plan: the HTML page the owner approves before the plan is written. |
| `ideas/` | One file per idea that is not yet a plan, with a status. |
| `research/` | What an agent read for a plan: links, quotes, notes. |
| `prompts/` | The stage prompts each role receives, one file per role. |
| `roles/` | What each role is for, as `## <role>` sections. |
| `runs/` | The engine's live runs. Git-ignored. |
| `evidence/` | Screenshots and reports from a run's own checks. Git-ignored. |

`workflow.md` is the four gates. `orchestrator.json` is what the engine reads. `orchestrator.local.json`
holds this machine's paths and the report token: it is git-ignored and written by Orchestra.
