# Orchestrator port — DrivingScore → Project B

Status: **Implemented and tested 2026-10-20** — this file is kept as the record of what was approved;
results are in the session report and in [`REVIEW-BRIEF.md`](REVIEW-BRIEF.md).
Source (read-only): `F:\Projects\@Source Codes\@Personal\Project DrivingScore\orchestrator\`
Target: `F:\Projects\@Source Codes\@Personal\Project ProjectB\orchestrator\` (this folder)

Nothing is copied yet. This document is the whole plan; step 3 implements it, step 4 tests it
without a single paid model call.

---

## 0. Step 1 — Inventory (facts this plan is built on)

### 0.1 Graphify

| Question | Answer |
|---|---|
| CLI installed | **yes** — `graphify 0.9.71` (`C:\Users\user\.local\bin\graphify.exe`) |
| Per-repo graph | `Agent/graphify-out/graph.json` (19 MB, mtime **2026-10-19 10:41**), `Ui/…` (1.8 MB, **2026-10-04 20:59**), `Web/…` (7.3 MB, **2026-10-01 10:12**) |
| Repo HEAD | Agent `2026-10-19 23:47` (13 h newer than its graph), Ui `2026-10-19 23:51` (**15 days** newer), Web `2026-10-15 23:33` (**14 days** newer) |
| Root `graphify-out/` | **absent** — no merged graph exists in this workspace |
| LLM-extracted nodes | **yes, in all three.** `_origin: "semantic"`: Agent **75**, Ui **15**, Web **48** (plus 2 nodes with no `_origin`), all `file_type: "concept"` with an empty `source_file` |
| Graphify rules for agents | root `AGENTS.md`: **no mention**. Per repo: `Agent/.ai/tools/graphify.md`, `Web/.ai/tools/graphify.md`, `Ui/.ai/coding/graphify.md`; each repo's `AGENTS.md` points at its doc from the *session protocol* (Writing a plan), and the planner/plan-reviewer role files link it |

Consequences used below:

- `graphify update <repo>` is safe for those semantic nodes: it re-extracts only changed code files
  and **replaces nodes whose `source_file` was re-extracted**; concept nodes carry an empty
  `source_file`, so they are not in the replacement set. (Read from graphify's own incremental
  runbook — `build_merge(..., prune_sources=deleted)` — not yet tested on these graphs.)
- A **full rebuild** (`graphify extract . --code-only ; cluster-only`) **would drop them**, and that is
  the documented fallback in all three repos when `update` asks for a rebuild. The ported refresh
  script must therefore never do a full rebuild on its own, and must back the graph up before any
  update when semantic nodes are present.
- `merge-graphs` rebuilds the root from its inputs, so a root-only doc node would be lost. There is no
  root graph and no root corpus here → **we do not build a merged root graph** (see §4).
- Read-only workers cannot refresh the graph themselves (claude read-only disallows `Bash`, opencode
  read-only runs `--agent plan`, which denies bash) — the orchestrator's own refresh step is the only
  thing that makes a fresh graph available. That is why refresh stays on (target repo only), §3.4.

### 0.2 Workflow docs (source → target)

| Source | Target equivalent |
|---|---|
| `Skills/plan-workflow/SKILL.md` (shared four gates) | `{repo}/.ai/agents/workflow.md` — **AUTHORITATIVE** in all three repos (Ui's is a port of Web's) |
| `Skills/plan-workflow/roles/{planner,plan-reviewer,implementer,verifier}.md` | `Agent/.ai/agents/*.md`, `Web/.ai/agents/*.md` are per-role files. **Ui has no per-role files** — the roles are `## planner` … `## verifier` sections in `Ui/.ai/agents/README.md` |
| `Skills/plan-workflow/prompts/{plan,check-plan,implement,verify}.md` | `{repo}/.ai/prompts/{plan,check-plan,implement,verify}.md` — present in all three repos (Agent also has `add-feature.md`, `refactor.md`, `review.md`) |
| Plan template | `{repo}/.ai/plans/TEMPLATE.md` (+ `plans/README.md`) — all three |
| Gate doc with the exact commands | `Ui/.ai/coding/ci.md`, `Web/.ai/coding/ci.md`, `Agent/.ai/coding/ci.md` — all three exist, same relative path |

Exact commands, read from those docs (not guessed):

| Repo | Gate commands |
|---|---|
| `Ui` | `npm run typecheck` · `npm run lint` · `npm run test` · `npm run build` |
| `Web` | `dotnet build ProjectB.Web.sln` · `dotnet test ProjectB.Web.sln` |
| `Agent` | `dotnet build` · `dotnet test` (ci.md; `planner.md` names `ProjectB.Agent.sln`, which the bare commands resolve to — one solution at the repo root) |

### 0.3 Agent CLIs

| CLI | Version | Config model names |
|---|---|---|
| `claude` | 2.1.302 (Claude Code) | `opus`, `sonnet` — not listable offline; unchanged |
| `opencode` | 1.19.3 | `opencode models` (free, 10 listed): `deepseek-v4.1-flash-free`, `glm-5.3-flash-free`, `big-pickle` all **present** |
| `codebuddy` | 2.168.3 | `deepseek-v4.1-flash`, `hy3` — not listable offline; unchanged |
| PowerShell | 5.1 only (`pwsh` absent) | source scripts are 5.1-compatible; keep it that way |

Repo state at planning time: `Ui` and `Web` clean; `Agent` has plan 0081 in progress
(` M .ai/plans/README.md`, two untracked files). We touch none of it.

---

## 1. Copy list

Copied from the source, then edited as described. `runs/` is **not** copied.

| Target file | Change |
|---|---|
| `orchestrator/run.ps1` | copy + §3.1 (usage text), §3.4 (refresh call), §3.5 (prompt header wording only) |
| `orchestrator/lib/agents.ps1` | copy + `New-RolePrompt` per-repo resolution and Ui role-section fallback (§3.2) |
| `orchestrator/lib/bus.ps1` | copy verbatim (VERDICT/FINDING parsing, sentinels, lock, state) |
| `orchestrator/lib/gates.ps1` | copy + `Get-RepoGateDoc` candidates (§3.3); everything else verbatim |
| `orchestrator/refresh-graph.ps1` | rewrite (short): repos from config, target-repo scope, semantic guard, no root merge by default (§4) |
| `orchestrator/orchestrator.json` | rewritten for this workspace (§2) |
| `orchestrator/WORKING-MODEL.md` | copy, re-pointed: repo names, skill path → `CONDUCTOR.md`, gate doc paths; **no decision changed** |
| `orchestrator/REVIEW-BRIEF.md` | copy, same re-pointing; changelog 0.3.0/0.4.0 kept, plus a short 0.5.0 "ported" entry |
| `orchestrator/DESIGN.md` | copy as **historical** design (its Persian text documents the original environment findings — claude disabled, qwen auth, etc. — which are history, not current config). One front-note says so |
| `orchestrator/CONDUCTOR.md` | **new** — the conductor procedure, ported from `.claude/skills/orchestrate/SKILL.md`, vendor-neutral (§4.4) |
| `.claude/skills/project-b-orchestrate/SKILL.md` | **new, approved** — thin pointer only: front-matter (`name`, `description` naming the trigger phrases) + ~5 lines saying "read `orchestrator/CONDUCTOR.md` and act as the conductor; if your prompt starts with `# ORCHESTRATOR CALL` you are a worker". No rules, no paths to repo docs. The only file outside `orchestrator/` |
| `orchestrator/.gitignore` | **new**, one line: `runs/` (the root is not a repo, but this keeps a future `git init`/copy clean) |
| `orchestrator/tests/offline/mock-agent.ps1` | **new** — the offline test agent (§6) |
| `orchestrator/tests/offline/run-offline.ps1` | **new** — builds the throwaway workspace, runs the 15 checks, prints a table |

Not copied: `runs/` (old task runs), any `*.bak`, keys, transcripts.

---

## 2. `orchestrator.json`

Full replacement, same schema plus the `{repo}` placeholders and the two new workflow keys:

```jsonc
{
  "version": "0.5.0",
  "note": "Ported from Project DrivingScore 0.4.0. Behaviour is unchanged; paths, repos and gate commands are Project B's.",
  "root": "..",
  "workflow": {
    "gatesPath":   "{repo}/.ai/agents/workflow.md",
    "rolesPath":   "{repo}/.ai/agents",
    "rolesIndex":  "{repo}/.ai/agents/README.md",
    "promptsPath": "{repo}/.ai/prompts",
    "agentsDoc":   "{repo}/AGENTS.md",
    "indexDoc":    "{repo}/.ai/README.md",
    "planTemplate":"{repo}/.ai/plans/TEMPLATE.md"
  },
  "repos": { "Agent": "Agent", "Ui": "Ui", "Web": "Web" },
  "roles": { … four roles, unchanged chains … },
  "adapters": { … opencode, claude, codebuddy enabled; qwen disabled; manual … },
  "rules": { "reviewerModelMustDifferFromPlanner": true, "neverCommit": true, "gateCommandsFromRepoDoc": true },
  "preflight": { "enabled": true, "pingPrompt": "Reply with exactly one word: PONG", "timeoutSec": 120 },
  "defaults": {
    "timeoutSec": 900,
    "agentCwd": "repo",
    "retryDelaySec": 20,
    "maxDiffChars": 60000,
    "refreshGraph": true
  },
  "verifyCommands": {
    "Ui":    ["npm run typecheck", "npm run lint", "npm run test", "npm run build"],
    "Web":   ["dotnet build ProjectB.Web.sln", "dotnet test ProjectB.Web.sln"],
    "Agent": ["dotnet build", "dotnet test"]
  },
  "revise": { "maxAutoRounds": 2, "note": "…unchanged…" },
  "readback": { "enabled": true, "note": "…unchanged…" }
}
```

Roles/chains/adapters are kept byte-for-byte from the source (`planner` claude/opus → deepseek → big-pickle;
`plan-reviewer` glm → big-pickle → claude/sonnet; `implementer` codebuddy deepseek → deepseek-free → big-pickle;
`verifier` codebuddy hy3 → glm → big-pickle), because every named model exists or is unverifiable-but-unchanged
(§0.3), and the `family` values that gate 2 depends on do not change.

`verifyCommands` note: the source ran only typecheck/lint/test for Ui. Ui's own gate doc says **four**
commands are the definition of done, so `npm run build` is included — **confirmed by the user**. It is
the slow one (~1 min); the 900 s per-command timeout covers it.

---

## 3. Code changes (only these; everything else is a verbatim copy)

### 3.1 `run.ps1` — usage text

- title: `orchestrator / run.ps1 - multi-agent, four-gate driver for Project B`
- repo placeholders `new <taskId> <Ui|Web|Mobile>` → `<Agent|Ui|Web>`
- the conductor line points at `orchestrator/CONDUCTOR.md`, not `.claude/skills/…`
- every other line (states, summary block, notes) unchanged.

### 3.2 `lib/agents.ps1` — `New-RolePrompt` reads the target repo

New helper, and the three path lookups go through it:

```powershell
function Resolve-WorkflowPath {
    param($Ctx, [string]$Template, [string]$Repo)
    $rel = $Template -replace '\{repo\}', $Repo
    Join-Path $Ctx.Root $rel
}
```

- `gatesPath`, `promptsPath/{promptFile}` — as today, through the resolver.
- `rolesPath/{Role}.md` — if the file is absent (Ui), extract the role's section from
  `rolesIndex`: from a line matching `^##\s+<role>\s*$` down to the next `^##\s`. If neither
  resolves, the role block is empty, exactly as today (a missing file was already non-fatal).
- Embed, in the same order as today: gates doc → role → stage prompt → orchestrator-mode protocol.
- Add one short block after the repo-state block listing the repo docs a role must open itself
  (`agentsDoc`, `indexDoc`, `planTemplate`, plus `../Web/.ai/architecture/api-surface-v3.md` when
  the repo is `Agent` or `Ui`). No rules restated; paths only.
- The "Workspace mode" paragraph becomes conditional on `defaults.agentCwd`:
  - `repo`: *"Your working directory is the target repo root `<path>`. Read `../Web`, `../Agent`,
    `../Ui` freely — they are siblings on disk — but write only inside this repo."*
  - `root`: today's wording, unchanged.

Nothing else in the file changes: `Invoke-External`, adapters, `Invoke-Role` chain walking, failure
classes, fingerprints, `Get-RoleProtocol`, `Test-ModelNames`, `Test-AgentHealth`.

### 3.3 `lib/gates.ps1` — `Get-RepoGateDoc`

Current candidates are `.ai/coding/ci.md`, `.ai/coding/testing.md`, `.ai/mobile/build-apk.md`,
`.ai/coding/build.md`; the first already matches all three target repos. Change: reorder to put
`{.ai/coding/ci.md, .ai/coding/testing.md, .ai/coding/build.md, docs/ci.md}` and return the path **and**
its first command block? No — keep the function's shape (path or `$null`); it is currently unused by
`run.ps1`, and the plan does not wire it in (the exact commands live in `verifyCommands`).

### 3.4 `refresh-graph.ps1` — rewrite (also fixes source weakness #11)

```powershell
param(
    [string]$Config,              # default: orchestrator.json next to this script
    [string[]]$Repos = @(),       # default: the target repo of the run
    [switch]$All,                 # every repo in the config
    [switch]$MergeRoot            # off by default; see §4
)
```

Behaviour:

1. Load `orchestrator.json` (`Import-OrchConfig`), take `repos` from it — **no hardcoded list**.
2. Select repos: `-Repos`, else all when `-All`, else the one passed by `run.ps1 new`.
3. For each repo: if `graphify-out/graph.json` contains `"_origin": "semantic"`, copy it to
   `graphify-out/graph.json.bak-<yyyyMMdd-HHmmss>` **first**.
4. Run `graphify update <repoPath>`; report a non-zero exit with the last 3 lines, never fatal.
   **Never** run `graphify extract` / a full rebuild — that is what would drop the concept nodes.
5. `-MergeRoot` only: refuse when `graphify-out/graph.json` at the root already holds semantic nodes
   unless `-ForceRoot`; otherwise `graphify merge-graphs <graphs> --out graphify-out/graph.json`.
   Without the switch, the root graph is not touched or created.

### 3.5 `run.ps1` — the two call sites

- refresh call becomes
  `& (Join-Path $OrchDir 'refresh-graph.ps1') -Config $Ctx.ConfigPath -Repos $State.repo` (or `$Repo`
  in `Cmd-New` before the state exists) — target repo only, per §4.
- `Show-Usage` line above.

---

## 4. Decisions

### 4.1 Worker cwd — `defaults.agentCwd` becomes `"repo"` (was `"root"`)

Reason, specific to this workspace:

- The knowledge base is **per repo** (`Agent/.ai`, `Ui/.ai`, `Web/.ai`, each with its own
  `AGENTS.md` + `workflow.md`). With cwd at the root, an agent that looks for `AGENTS.md` or `.ai/`
  in its working directory finds the **router** — which explicitly says "this folder is a container,
  pick a repo first". With cwd at the repo it finds the real rules.
- The root collects unrelated, in-progress material. It held a plan runbook, database backups,
  scratch files and a stray `ui-now.xml` when this was decided (all removed on 2026-10-20); a worker
  with a root cwd can stumble into whatever lands there next, a repo cwd cannot. The rule is about the
  habit, not about those particular files.
- Git is simpler: the implementer/verifier run `git status`, `git diff` without `-C`. The protocol
  still documents `git -C "<repo root>"`, which keeps working.
- Cross-repo reading still works: `../Web`, `../Agent`, `../Ui` are ordinary sibling paths. The
  prompts say so explicitly, because reading the other repo is expected here (the route surface is
  the contract).
- The workspace fingerprint still guards every hop, so a write escaping the repo is caught exactly
  as before. `agentCwd: "root"` remains a supported mode if this turns out to be wrong.

### 4.2 No merged root graph

Do not create or rebuild `graphify-out/graph.json` at the root. There is none today; the three graphs
are already 13 h / 14 d / 15 d stale, so a merge would only advertise the worst of them; the conductor
queries the target repo's graph directly; and a root merge is where root-only doc nodes would be lost.
`-MergeRoot` exists for later, opt-in.

### 4.3 Refresh scope

`refreshGraph` stays `true` (source behaviour: a plan never starts on a stale graph) but refreshes
**only the target repo**. Refreshing all three cost three AST passes per task for two graphs nobody
reads in that run, and it writes into sibling checkouts. It writes only `graphify-out/`, which all
three repos gitignore (`.gitignore: Agent 256, Ui 47, Web 57`), so it never dirties a repo or trips
the fingerprint (which uses `git status --porcelain`, and ignored files are excluded).

### 4.4 Conductor procedure — `orchestrator/CONDUCTOR.md`

`CONDUCTOR.md` is the ported `.claude/skills/orchestrate/SKILL.md`, with:

- the worker-detection preamble kept (`# ORCHESTRATOR CALL` ⇒ not the conductor),
- the guard "never write a conductor rule into a root `AGENTS.md`" kept,
- task ids `T-<REPO>-NNN`, drafts in `orchestrator/runs/_drafts/<taskId>.plan.md`,
- repo list `<Agent|Ui|Web>`, `-PlanFamily` guidance, summaries over full logs, graphify-before-grep,
- the "using another harness" caveat moved here from `WORKING-MODEL.md` (same text, new path).

**Decided (user, this session):** `CONDUCTOR.md` is the procedure; a **thin pointer skill** at
`.claude/skills/project-b-orchestrate/SKILL.md` is allowed, on the strict condition that it holds no
rule and no repo knowledge — only the trigger description and "read `orchestrator/CONDUCTOR.md`".
It is the only file this port creates outside `orchestrator/`. The root `AGENTS.md` itself is not
touched, and no conductor rule goes into it (every worker reads it).

---

## 5. What must not change

Kept exactly: `VERDICT:` parsing (`Get-Verdict`, last line wins, missing = not passed), `FINDING
[blocker|should|nit]`, model-family gate 2 (`Get-HopFamily`, same-family hops skipped, approve
refuses), read-back (gate 2.5) and `-NoReadBack`, `-PlanFile` conductor mode and `revise -PlanFile`,
auto-revise cap, `plan.approved.md` + sha256 freeze, fingerprint guards, branch creation by the
orchestrator, no commits, sibling/commit/branch violations, `=== SUMMARY ===`, per-run lock,
failure classes and the single rate-limit retry, `exec` adapter for offline tests.

---

## 6. Step 4 — tests, no paid model calls

### 6.1 Parse/load (free)

| # | Check | Pass condition |
|---|---|---|
| P1 | `run.ps1 help` | usage prints, title says Project B, repos `Agent/Ui/Web`, exits 0 |
| P2 | `run.ps1 agents` | four roles, gates 1–4, families printed; adapters list |
| P3 | `run.ps1 new T-DRY-001 Ui "dry run" -DryRun` | three prompt artifacts written; each contains the **Ui** `workflow.md` text, the Ui role section, and `## Orchestrator mode`; nothing under `Ui/` changed (`git status` identical before/after) |
| P4 | delete `runs/T-DRY-001` | folder gone |

P3 is also run once for `Web` and `Agent` (different role-file shapes: file vs README section) — cheap,
no models.

### 6.2 Offline end-to-end — temp workspace, mock agents

`orchestrator/tests/offline/run-offline.ps1` creates `%TEMP%\project-b-orch-<rand>\` with three git repos
`Agent`, `Ui`, `Web` (one commit each, `user.name` set), copies `mock-agent.ps1`, writes
`config.json` (`root: "."`, `refreshGraph: false`, `agentCwd: "repo"`, all four roles on the generic
`exec` adapter pointing at `powershell -NoProfile -File mock-agent.ps1 <model>`, `verifyCommands`
for each repo reading a marker file). The mock reads the prompt on stdin, detects the role from the
`role: <name>` header (and `READ-BACK ONLY` for gate 2.5), and prints canned replies ending in
`VERDICT: …`. Behaviour is driven by `MOCK_MODE` (env) and by files the test writes.

| # | Scenario | Expected |
|---|---|---|
| 1 | conductor mode: `new … -PlanFile plan.md` | plan v1 by `conductor:chat`, gate 2 runs, reviewer reply with `FINDING [should]` ⇒ status `AwaitingApproval`, summary `review: FINDINGS` |
| 2 | `revise … -PlanFile plan.v2.md` | plan v2 recorded, reviewer PASS, read-back runs (gate 2.5) and returns `PASS` ⇒ summary shows `readback: PASS` |
| 3 | `approve` with an open read-back finding | exit 1, message names `open findings`, status unchanged |
| 4 | `approve "notes"` | `plan.approved.md` written with `APPROVED AMENDMENTS`, sha256 recorded, status `Approved` |
| 5 | `implement 1` | orchestrator creates branch `alireza/t-…` (base recorded), mock writes one file **named in the plan** ⇒ no outside-plan warning, status `Implemented` |
| 6 | `test-config` gate green ⇒ `verify` | `gates green`, verifier PASS ⇒ status `Verified` |
| 7 | `implement 2` after Verified | allowed, second phase recorded, still on the same branch |
| 8 | dirty tree | fresh run, dirty `Ui` before `implement` ⇒ exit 1, message `gate 3: … uncommitted changes`, no branch created |
| 9 | read-only role writes | `MOCK_MODE=write-on-review` ⇒ orchestrator throws *read-only role … changed the workspace*, no fallback, no status advance |
| 10 | write role fails after an edit | `MOCK_MODE=edit-then-fail` ⇒ *No fallback: the next agent would start on a half-edited tree*, chain stops (second hop never invoked: transcript count = 1) |
| 11 | sibling repo changed | `MOCK_MODE=touch-sibling` ⇒ status `Violation`, summary `changed other repos: Web` |
| 12 | commit made | `MOCK_MODE=commit` ⇒ status `Violation`, summary `1 commit(s) made` |
| 13 | tampered approval | append a line to `plan.approved.md`, `implement` again ⇒ exit 1, `plan.approved.md changed after approval` |
| 14 | `verify` with a red gate command | gate exits 1 while the mock says PASS ⇒ final `FAIL`, status `VerifyFailed` |
| 15 | `Test-ModelNames` against the real config | table lists every hop; the three `opencode` names `ok`; claude/codebuddy `unchecked`; no `UNKNOWN MODEL` |

Each run's `runs/<id>/task.json` is asserted on `status`, verdicts, `branch`, `baseSha`, `phases`
count and `violations` — not on stdout prose.

### 6.3 What is **not** tested (stated up front)

- Any real model, adapter flag or auth path (`claude -p`, `codebuddy`, `opencode run`) — no paid
  calls, per your rule. Those were proven in the source workspace and are unchanged code.
- Real `npm`/`dotnet` gate runs inside the three repos (slow, and step 4 forbids touching them).
  The gate wiring is tested with trivial commands in the temp workspace.
- `dotnet`/`npm` correctness of `verifyCommands` beyond "the string is what the gate doc says".
- `refresh-graph.ps1` against the real graphs (it would write inside the repos this session must not
  touch). It is exercised in the temp workspace only for argument/selection logic; the semantic
  backup branch is covered by a unit-style call on a copy of a small synthetic graph.
- `health` (pings models) — not run.

---

## 7. Optional graphify fixes (separate from the port; say yes/no per item)

1. **Refresh the stale graphs** (`Ui` 15 d, `Web` 14 d, `Agent` 13 h) with `graphify update` per repo —
   free, AST only. Do it after the port, in a session where writing `graphify-out/` is acceptable.
2. **Never full-rebuild these repos** while semantic nodes exist. If a rebuild is ever needed, first
   `graphify extract . --code-only`, then re-add doc nodes (or accept losing 15–75 concept nodes).
   Optional: add that warning to each repo's graphify doc — it currently recommends the rebuild path
   without mentioning the loss.
3. **`graphify check-update <repo>`** in `run.ps1 health` (free) to warn when a semantic re-extraction
   is pending, instead of silently planning on a stale graph.
4. **Root `AGENTS.md`** never mentions graphify; one router line pointing at the three repo docs would
   help agents that start at the root. This plan does **not** touch it (hard rule: leave root files
   alone) — listed only as a recommendation.

---

## 8. Done criteria for this port

- `orchestrator/` contains exactly the files in §1; nothing outside it is created or modified.
- `run.ps1 help`, `agents`, `new … -DryRun` pass, and the dry-run prompts contain the target repo's
  workflow doc and the orchestrator-mode section.
- The 15 offline checks pass, results reported as a table with the untested list above.
- `WORKING-MODEL.md` / `REVIEW-BRIEF.md` / `DESIGN.md` read in this workspace's terms (repo names,
  paths) with no decision changed.
- `CONDUCTOR.md` exists and is complete enough that another harness can drive the tool from it, and
  the `.claude` pointer contains no rule of its own.

**Awaiting approval to implement §1–§6.**
