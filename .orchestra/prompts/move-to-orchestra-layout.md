# Companion — a project moves to `.ai/` + `.orchestra/` (plan 0007, phases B and C)

Paste this into a session **opened in that project's workspace root**. It is not run from Orchestra: the
change is made in that workspace, in each of its repos' own history. The workspace holds several repos
and is not itself a repo; `<this project>` and `<Repo>` below stand for its own folder and repo names.
Nothing here names a project, a run or a plan number, and nothing in Orchestra's public repository may.

## Stop first

1. **Dirty code:** for every repo in the workspace, `git status --porcelain` must show no `*.ps1`,
   `*.json`, source or test change. Markdown may be uncommitted; it does not block (workflow.md,
   "Dirty means code").
2. **A run in flight:** `powershell -NoProfile -File orchestrator/run.ps1 queue` must list nothing, and no
   `runs/T-*/task.json` may say `Implementing`. Ask the owner before going on.
3. **The owner said go**, in this session, and the dashboard's counts for this project (runs, plans,
   previews, ideas) are written down — step 9 compares against them.

## 1. Rename the folder

Close every terminal and agent session that uses `orchestrator/`, then rename the folder itself to
`.orchestra/` at the workspace root. It is the root of its own repo, so git sees nothing; inside it
`orchestrator.json`, the git-ignored `orchestrator.local.json`, `CLOSE-PLAN.md` and `runs/` keep their
names.

## 2. Copy what belongs to the orchestrator, per repo

A `git mv` cannot cross repos. For every file below: **copy** it into `.orchestra/` and `git add` it
there, then `git rm` it in the product repo. No content changes in this step.

- `workflow.md` — the repos' copies. Identical copies become one file. If they differ, **stop** and show
  the owner the diff: merging them is a rule change the owner approves in this session, and the merged
  file is what `.orchestra/workflow.md` holds.
- `roles/<Repo>/` — from `<Repo>/.ai/agents/*.md` except `workflow.md`; that folder's own `README.md`
  stays `roles/<Repo>/README.md`. Roles are **not** merged across repos.
- `prompts/<Repo>/` — from `<Repo>/.ai/prompts/`.
- `plans/<Repo>/` — from `<Repo>/.ai/plans/*.md` and `<Repo>/.ai/plans/done/*.md`, except `TEMPLATE.md`;
  each repo's `.ai/plans/README.md` is kept as `plans/<Repo>/README.md` (the dashboard never indexes a
  `README.md`, so counts do not change). A finished plan with no `Status:` line gets
  `**Status:** Done` as its second line; a Status line in another form (`Status: **Done**`, unbolded
  `Status: Done`) is rewritten to `**Status:** Done …` keeping the rest of the line. Nothing else in a
  plan changes.
- `plans/TEMPLATE.md` — one file at the root. Identical copies become one; different copies: **stop** and
  ask the owner which one.
- `previews/` — the workspace root's `*.html`, and `runs/_drafts/*.html` for drafts that are still wanted.
- `ideas/` — the root `ideas.md` becomes `ideas/README.md`; `research/` as it is.
- `evidence/` — the root `screenshots/`.

`.orchestra/.gitignore` keeps what it already ignores and lists `runs/` and `evidence/`: both stay out of
git (ADR 0005).

## 3. Point the config at the new layout

In `.orchestra/orchestrator.json`:

- `gatesPath: ".orchestra/workflow.md"`;
- `rolesPath: ".orchestra/roles/{repo}"`, `rolesIndex: ".orchestra/roles/{repo}/README.md"` — set the
  index only for a repo that had `.ai/agents/README.md`; otherwise leave it out;
- `promptsPath: ".orchestra/prompts/{repo}"`;
- `planTemplate: ".orchestra/plans/TEMPLATE.md"`;
- `agentsDoc` and `indexDoc` stay `{repo}/AGENTS.md` and `{repo}/.ai/README.md`;
- a top-level `plansDir: ".orchestra/plans/{repo}"`;
- `cleanTree.ignore`: drop `.ai/plans/`, keep `.ai/context/`.

The `default` profile is not touched: these are project keys.

## 4. Fix the links

In each product repo: `AGENTS.md` and `.ai/README.md` point at `../.orchestra/…` instead of the moved
files. Under the workspace root, every skill in `.claude/skills/` and the close-plan checklist point at
`.orchestra/…`, and say where a plan is written: gate 1 writes
`.orchestra/plans/<Repo>/NNNN-topic.md` and the preview to `.orchestra/previews/NNNN-topic/index.html`,
and closing a plan means setting its `Status: Done` line.

## 5. Prove it

```powershell
powershell -NoProfile -File .orchestra/run.ps1 agents
powershell -NoProfile -File .orchestra/run.ps1 health
```

Both must pass. `agents` prints `config:`, `profile:` (when the project has one), `engine:` and `root:`.
Then `run.ps1 close` on the next run must find a plan whose `Status:` says `Done` in
`.orchestra/plans/<Repo>/`.

## 6. Commit, only on the owner's word

One commit per repo, nothing pushed: `Move orchestrator docs to .orchestra/ (no content change)`.

## 7. The owner's last step

The owner changes the project's `RunsPath` in Orchestra to `<root>/.orchestra/runs`, removes any
`Library` globs for the project, re-imports and compares the counts with the ones written down in the
stop block: runs and plans must be unchanged, and a plan must still link to its run.

## What the session log records

"Moves only" is checked per region:

- **tracked product-repo files** — the product repos show only deletions plus the link edits;
  `.orchestra/` shows additions whose content equals the deleted files (a hash list), except the Status
  lines above;
- **untracked workspace-root files** (root `*.html`, `ideas.md`, `research/`, `screenshots/`) — a hash
  list before and after; there is no deletion side;
- **`.claude/skills/`** — outside every repo, so a local edit listed for the owner to accept; no commit.
