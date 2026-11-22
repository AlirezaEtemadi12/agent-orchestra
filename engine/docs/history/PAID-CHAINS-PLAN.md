# Paid chains — OpenCode Go, session reuse, quota exhaustion, a generic manual hop

Status: **Planned (2026-11-14).** Written by the conductor session (Opus) after the owner bought OpenCode Go.
Executor: one agent session working **only inside `orchestrator/`**. Nothing in `Agent/`, `Web/`, `Ui/` changes.

## فهم من از درخواست

اشتراک OpenCode Go خریده شده. ارکستر از این به بعد به‌جای مدل‌های رایگان (که مدام به سقف می‌خوردند) از مدل‌های Go
استفاده می‌کند. هدف اصلی «مصرف کمتر و کنترل‌شدهٔ توکن» است، به‌خصوص در رفت‌وبرگشت‌های پلنر و مرورکننده. فهم من از
تصمیم‌ها:

- مرور ← GLM-5.3، بعد MiMo-v2.6-flash، بعد Sonnet (فقط اگر نویسندهٔ آخرین نسخهٔ پلن Sonnet نباشد)، بعد حالت دستی.
- read-back و پیاده‌سازی ← DeepSeek v4.1 Flash روی Go، بعد codebuddy، بعد حالت دستی. پیاده‌سازی **همان سشن**
  read-back را ادامه می‌دهد، به شرطی که پلن تأییدشده همان نسخه‌ای باشد که read-back دیده.
- verify ← **همان سشن** مرورکننده ادامه پیدا می‌کند؛ اگر نشد، همان زنجیرهٔ مرور.
- دور دوم به بعدِ مرور در همان سشن مرورکننده و فقط با diff پلن.
- read-back همچنان **بعد** از PASS مرور اجرا می‌شود، نه هم‌زمان (تا ایرادهای تکراری دوبار پول نگیرند).
- مرورکننده برای هر یافته یک خط «اصلاح پیشنهادی» می‌دهد؛ تصمیم با پلنر است.
- سقف دورهای revise: ۳.
- سقف ۵ساعته/هفتگی Go مال کل اکانت است؛ پس وقتی یک مدل Go پیام «سقف مصرف» داد، **همهٔ** hopهای Go آن زنجیره بدون
  تلاش دوباره رد می‌شوند و نوبت hop بعدی غیر Go (Sonnet یا codebuddy) و در آخر حالت دستی است.
- حالت دستی عمومی است: هیچ‌جا اسم ابزار خاصی (Freebuff) نمی‌آید. پرامپت در inbox نوشته می‌شود؛ مالک آن را به هر
  ایجنتی که بخواهد می‌دهد، یا صبر می‌کند تا سقف تمام شود و همان دستور را با `-Retry` دوباره می‌زند.
- پیاده‌سازی هم حالت دستی دارد (قبلاً ممنوع بود).

جایی که دو برداشت ممکن بود: «Sonnet اگر پلنر Sonnet نباشد» را این‌طور خواندم: خانوادهٔ **نویسندهٔ آخرین نسخهٔ پلن**
ملاک است. اگر هماهنگ‌کنندهٔ Sonnet نسخه‌ای را revise کند، آن نسخه نوشتهٔ Sonnet است و Sonnet مرورش نمی‌کند.

## جریان کار

```
Session A  Opus (desktop)   discuss -> preview -> plan (+ "Decisions & why")  -> hand off
Session B  Sonnet (fresh)   run.ps1 new -PlanFile ...
   review v1   GLM (Go)  ......................... new opencode session R
   PASS? -> read-back v1  DeepSeek (Go) .......... new opencode session I
   findings -> B triages: mechanical=apply | decision=owner/Session A | wrong=reject in one line
   revise v2 -> review v2 in session R (diff only) -> read-back v2 in session I (diff only)   (max 3 rounds)
   owner approves -> approve
   implement phase 1 .......................... continues session I (no re-reading)
   gates (build/test, no model)
   verify ..................................... continues session R
Go quota exhausted -> skip every Go hop -> Sonnet / codebuddy -> manual inbox (any agent) or wait + -Retry
```

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

1. `run.ps1 agents` ← زنجیره‌ها همان جدول D3 هستند و هیچ‌جا اسم Freebuff نیست.
2. `run.ps1 health` (با تأیید شما، چون مدل صدا می‌زند) ← هر نقش یک مدل سالم دارد.
3. یک تسک کوچک واقعی با `new -PlanFile` ← در summary مرور، یافته‌ها خط `FIX:` دارند.
4. بعد از یک `revise`، در `runs/<id>/transcript/plan-reviewer.v2.json` همان session id دور اول دیده می‌شود.
5. بعد از `approve` و `implement 1`، transcript پیاده‌سازی session id همان read-back را دارد.
6. `verify` ← session id همان مرورکننده.
7. `run.ps1 pools` ← اگر سقف Go پر شده باشد، می‌گوید `opencode-go: quota exhausted until HH:mm`.

---

## 0. Before the first edit (STOP if any fails)

- **Owner action, not yours:** in the OpenCode console, Providers → *Manage privacy* → model regions → **Global**.
  Without it every Go DeepSeek/GLM/MiMo call fails with `This OpenCode Go model is hosted outside Europe and the
  United States` (seen 2026-11-14). You do not need it: you never call a real model.
- `orchestrator/` is a git repo. `git -C orchestrator status --short` must be clean **except untracked plan
  documents at the folder root (`*-PLAN.md`, this file among them)** — those are records, not work in progress.
  Any other entry (a modified or untracked code, config, test or doc file) is a STOP: the owner commits it first.
  The quota-pools work was committed as `f5642e4`; run `git status` yourself rather than assuming the tree is dirty.
  Then `git -C orchestrator switch -c alireza/paid-chains`. Never commit.
- No run in flight: no `powershell … run.ps1` process and no `runs/*/.lock`.
- Baseline: `powershell -NoProfile -File orchestrator/tests/offline/run-offline.ps1`. Record the count. Red = STOP.
- **Never call a real model** (`opencode`, `claude`, `codebuddy`), never `run.ps1 health`, never touch `runs/`
  or the three product repos. Every test goes through `tests/offline/mock-agent.ps1`.
- Facts already verified on 2026-11-14 (do not re-probe):
  - `opencode run --session <id>` continues a session; a self-made id is refused (`Expected a string starting
    with "ses"`).
  - `opencode run --title <t>` sets the title; `opencode session list --format json -n <N>` (run in the **same cwd**,
    sessions are per project directory) returns `[{ "id": "ses_…", "title": "<t>", … }]`, newest first.
  - Continuing with the same model kept the earlier turn (asked a remembered word, got it back).
  - Continuing a `--agent plan` session without `--agent` uses the default (build) agent.
  - `opencode models` lists `opencode-go/glm-5.3`, `opencode-go/mimo-v2.6-flash` and
    `opencode-go/deepseek-v4.1-flash` verbatim, so S15's live name check resolves all D3 hops. If it ever does not,
    report it in the hand-over as an environment fact — do not probe opencode, rename a model or weaken the check.

## 1. Files

| File | Change |
|---|---|
| `orchestrator.json` | D3 chains, `opencode-go` pool, `quota`, `sessions`, `revise.maxAutoRounds: 3`, generic `manual` note, version `0.7.0` |
| `lib/agents.ps1` | families, quota class, pool-exhausted skip, session capture/continue, generic + implementer manual hop, pending-request routing, `FIX:` in protocol |
| `lib/pool.ps1` | `Set-PoolExhausted`, `Get-PoolExhaustedUntil`, `Get-PoolBusyReason` mentions it |
| `run.ps1` | `-PlanFamily` default + honoured on revise, `-Retry`, follow-up prompts, session bookkeeping, manual implement resume |
| `tests/offline/mock-agent.ps1` | `MOCK_QUOTA_TIMES`, session support |
| `tests/offline/run-offline.ps1` | the tests in §5 |
| `lib/gates.ps1` | D11: `Get-GitPorcelain` filtering helper |
| `CONDUCTOR.md`, `WORKING-MODEL.md` | D10, D11 |

## 2. Decisions (decide nothing new; STOP if the code contradicts one)

**D1 — Families are per model for Claude.** `Get-HopFamily`: an explicit `family` wins (unchanged). For adapter
`claude` without one: strip a leading `claude-` from the model name, then `claude-` + its first `[-_.]` token
(`opus` → `claude-opus`, `sonnet` → `claude-sonnet`, `claude-haiku-5-5` → `claude-haiku`). `run.ps1 -PlanFamily` default becomes `claude-opus`. Record at script top
`$script:PlanFamilyGiven = $PSBoundParameters.ContainsKey('PlanFamily')`. `Cmd-Revise -PlanFile`: family =
`$PlanFamily.ToLower()` when `$script:PlanFamilyGiven`, else the previous conductor family (today's behaviour).
Exclusion stays a plain string compare (`Test-Gate2ModelDiffer` unchanged).

**D2 — Quota exhaustion is its own failure class.** New config block:
```json
"quota": {
  "patterns": ["usage limit", "limit reached", "quota exceeded", "exceeded your", "insufficient balance", "insufficient credit", "free usage"],
  "exhaustedSec": 1800,
  "note": "A hop whose output matches one of these is out of quota for its whole pool: no retry, the pool is marked exhausted for exhaustedSec, and every other hop of that pool is skipped until then."
}
```
`Get-FailureClass` gets `$Ctx` (or the pattern list) and checks, **before** the rate-limit test, whether the output
matches any pattern (case-insensitive, literal) → `'quota-exhausted'`. Remove `usage limit|quota` from the
rate-limit regex. In `Invoke-Role`: on `quota-exhausted` call `Set-PoolExhausted -Pool $limits.Pool -Seconds
exhaustedSec`, record the attempt, **no retry**, next hop. Before `Enter-PoolSlot` (non-inbox hops only): if
`Get-PoolExhaustedUntil` is in the future, skip with reason `skipped: pool <p> quota exhausted until HH:mm` and a
DarkYellow host line. The marker is a file `runs/_pool/<pool>/exhausted-until.txt` holding **local-time** ticks,
written, read and compared exactly like `cooldown-until.txt` (`Set-PoolCooldown`, lib/pool.ps1: `(Get-Date).AddSeconds(n).Ticks`). `run.ps1 pools` prints `quota exhausted until HH:mm` for such a pool.

**D3 — Chains** (`orchestrator.json`, every Go hop carries `"pool": "opencode-go", "tier": "paid"`):

| Role | Chain |
|---|---|
| planner | `claude opus` [claude-opus] → `claude sonnet` [claude-sonnet] |
| plan-reviewer | `opencode opencode-go/glm-5.3` [glm] → `opencode opencode-go/mimo-v2.6-flash` [mimo] → `claude sonnet` [claude-sonnet] → `manual manual/any-agent` [manual] |
| readback | `opencode opencode-go/deepseek-v4.1-flash` [deepseek] → `codebuddy deepseek-v4.1-flash` [deepseek] → `manual manual/any-agent` [manual] |
| implementer | `opencode opencode-go/deepseek-v4.1-flash` [deepseek] → `codebuddy deepseek-v4.1-flash` [deepseek] → `manual manual/any-agent` [manual] |
| verifier | `opencode opencode-go/glm-5.3` [glm] → `opencode opencode-go/mimo-v2.6-flash` [mimo] → `claude sonnet` [claude-sonnet] → `manual manual/any-agent` [manual] |

`revise.maxAutoRounds: 3`. The `readback` role's `note` and the `manual` adapter's `note` are rewritten without any
tool name: *"a human-driven agent of the owner's choice: the prompt is written to runs/<id>/inbox/<key>.md, any
agent that can read and write files answers it, or the owner waits for the quota to reset and reruns with -Retry"*.
The free `opencode/*-free` models and `big-pickle` leave every chain.

**D4 — Reviewer and read-back suggest a fix.** In `Get-RoleProtocol`:
- plan-reviewer finding line becomes
  `FINDING [blocker|should|nit] <plan section>: <finding> (<doc that proves it>) — FIX: <the smallest change to the plan that resolves it, one line>`
  plus the line `- FIX is a suggestion for the plan's author, who decides. You still do not edit the plan.`
- readback finding line gets the same suffix: `— FIX: <what the plan must say so you need not guess>`.
`Get-Findings` already captures the rest of the line; verify a `— FIX:` line still counts (test T6).

**D5 — Session capture.** A hop "supports sessions" when its adapter is `opencode`, or an `exec` adapter whose spec
has `"sessions": true` (the mock). Invoke-Agent → adapter gets two new optional params, `-SessionId` (continue) and
`-SessionTitle` (new). The result object gains `SessionId` (`$null` when unknown).
- `Invoke-OpenCode`: with `-SessionId` add `--session <id>`; otherwise add `--title <SessionTitle>` and, after a
  successful exit, run `opencode session list --format json -n 20` with the **same `-Cwd`** (through
  `Invoke-External`, 60 s cap), parse with `ConvertFrom-Json`, take the first entry whose `title` equals the title.
  A failed lookup leaves `SessionId = $null` (never fails the hop).
- `Invoke-ExecAdapter` with `sessions: true`: pass `-Session <id>` when continuing; read the id from a stderr line
  `ORCH-SESSION: <id>`.
- Title: `orch <TaskId> <ManualKey> <yyyyMMddHHmmss>`. `Invoke-Role` passes `-SessionTitle`; it already has both.
- `Invoke-Role`'s return object gains `SessionId`.

**D6 — Continuing a session.** `Invoke-Role` gets `-Continue` (hashtable `Adapter, Model, SessionId, Prompt`, or
`$null`). When given, before walking the chain it looks for the chain hop with the same adapter **and** model; if
found, enabled, its family not excluded, and its pool not exhausted, it runs that hop once with `-SessionId` and
`$Continue.Prompt` (same pool slot rules, same read-only/changed-repo guards, same rate-limit retries). Success →
return it (`SessionId` = the continued id). Any failure **without** a changed target repo → record the attempt
(reason prefixed `continue: `) and walk the normal chain with the full prompt (a fresh session). A failure that
changed the repo throws, as today.

Bookkeeping in the task state: `state.sessions` is an object keyed by session id → `{ adapter, model, role, turns }`
(turns = calls made in it). A continuation is allowed only while `turns < sessions.maxFollowUps` (new config
`"sessions": { "maxFollowUps": 6 }`). Where each id is kept:

| Stored on | Set by | Continued by |
|---|---|---|
| `state.review.session` | `Invoke-ReviewStep` | the next `Invoke-ReviewStep` (round ≥ 2), then `Cmd-Verify` |
| `state.readback.session` | `Invoke-ReadBack` | the next `Invoke-ReadBack`, then `Cmd-Implement` |
| `state.implementSession` | `Cmd-Implement` | later phases of `Cmd-Implement` |
| `state.verifySession` | `Cmd-Verify` | later `Cmd-Verify` calls |

**D7 — Follow-up prompts** (short; no repo docs, workflow, role or stage prompt — the session already has them). Each
starts with `# ORCHESTRATOR CALL - role: <role> (follow-up in the same session)` and ends with
`Get-RoleProtocol` for that role. Bodies:
- **review, round n ≥ 2** — when `state.review.session` exists, `state.review.round -eq n-1` and that session may
  continue: "The plan was revised to version n." + a unified diff of `plan.v<n-1>.md` → `plan.v<n>.md`
  (`git diff --no-index --no-color -U3 <old> <new>`; exit 1 is normal; drop the `diff --git …` and `index …` lines, keep `---`/`+++` and the hunks) + "Check each of
  your previous findings is resolved; look for NEW blockers only in the changed parts; do not re-litigate settled
  points." + the user's notes when present. If the diff is longer than 60 % of the new plan, send the full new plan
  instead of the diff.
- **read-back, round n ≥ 2** — same rule with `state.readback.session`, wording for the read-back.
- **implement** — phase P: continue `state.implementSession` if set, else `state.readback.session` **only if**
  `state.readback.round -eq state.approved.version`. Body: "The plan you read back (version v) is now approved."
  + the APPROVED AMENDMENTS block when the approved file has one (the text after the
  `## APPROVED AMENDMENTS` heading) + `Phase to execute`, branch and repo-root lines exactly as today's `$extra`.
- **verify** — continue `state.verifySession` if set, else `state.review.session` **only if**
  `state.review.round -eq state.approved.version` **and** the review family differs from the last phase's
  implementer family. For that, `Cmd-Implement`'s phase record gains `family = $impl.Family` (Invoke-Role already
  returns it). Body: today's `$scope`, `$checks`, gate results and diff — without the plan text (it is in the
  session) — plus one line: "The plan you reviewed (version v) was approved" and the amendments block when present.

**D8 — The manual hop is generic and may implement.**
- `Import-OrchConfig`: a manual hop stays forbidden only for `planner`; the error text says so.
- `Invoke-Manual` gets `-AvoidFamilies` (the excluded families, from `Invoke-Role`). Its header, for every key:
  "Any agent can answer this. Do not answer with a model of family: <list>" (line omitted when the list is empty),
  and "If you would rather wait for the quota to reset, rerun the same command with -Retry instead."
- For keys starting `implementer.phase-` the header instead says: implement that phase in the repo as the request
  says; do not commit, do not create/switch/delete branches, write only inside the target repo; when done, write the
  report (last line `VERDICT: …`) into the answer file. For those keys the repo-fingerprint refusal is **skipped**
  (the repo is meant to change); instead `Invoke-Manual` saves `Get-WorkspaceFingerprint` (a hashtable of strings)
  as `inbox/<key>.workspace.json` when it writes the prompt.
- `Cmd-Implement` passes `-ManualKey "implementer.phase-$Phase"`. When the winning hop is an inbox hop and that
  `.workspace.json` exists, `$before` for the post-checks is loaded from it (so a sibling repo the human agent
  touched is still a violation).
- `Get-ResumeCommand`: `implementer` → `implement`; `Cmd-Watch -ThenResume`, `Cmd-Queue`'s `then:` line and the
  DEFERRED catch in `run.ps1` print/run `implement <id> <phase>` (phase = the key's suffix).

**D9 — A pending request is answered, not overtaken.** In `Invoke-Role`, when `-ManualKey` is set and
`runs/<id>/inbox/<key>.md` exists: without `-Retry`, every non-inbox hop is skipped (reason
`skipped: manual request <key> is pending`). New switch `-Retry` on `run.ps1` (passed to `review`, `readback`,
`implement`, `verify`, `revise` and through `Invoke-Role`): deletes `inbox/<key>.md`, `.fingerprint`,
`.workspace.json` (not the answer) and walks the chain normally.

**D10 — Docs.**
- `CONDUCTOR.md`: no tool name for the manual hop anywhere (today 5 mentions); step 5 says **three** rounds; a new
  section **"Two sessions"**: Session A (strong model, desktop app) discusses, previews and writes the plan with a
  «تصمیم‌ها و دلیل‌ها» section so nothing lives only in its chat, then hands off; Session B (Sonnet, fresh) runs the
  loop, reads only `=== SUMMARY ===`, triages findings (mechanical → apply; touches a decision → stop and give the
  owner a short note for Session A; wrong → reject in one line), passes `-PlanFamily claude-sonnet` to `revise` for
  versions it wrote, and runs `approve` only on the owner's word. A paragraph on quota: an exhausted Go pool skips to
  Sonnet/codebuddy, then the manual inbox; the owner answers it with any agent or waits and reruns with `-Retry`.
- `WORKING-MODEL.md`: the flow block shows the read-back session continuing into implementation and the review
  session continuing into verify; read-back still runs only after a review PASS.
- `lib/agents.ps1`, `run.ps1`, `orchestrator.json`: no "Freebuff" left in comments or notes
  (`grep -i freebuff` over those files and `CONDUCTOR.md` returns nothing).
- `CONDUCTOR.md` and `WORKING-MODEL.md` name the D11 ignore list in one sentence where gate 3 is described.
- Align stale text: every `-PlanFamily claude` / "default `claude`" becomes `claude-opus` (CONDUCTOR.md step 4
  command and the line after it, the "Set `-PlanFamily`" caveat; the usage example in `run.ps1` `Show-Usage`);
  "Two revision rounds" (CONDUCTOR.md step 5), "max ~2 rounds" (WORKING-MODEL.md flow) and "one or two revisions"
  (WORKING-MODEL.md Decision 5) become three.

**D12 — Existing checks that these decisions reverse** (update them in the phase that breaks them; the count stays):
- `S1` (conductor plan enters gate 2): expected family `claude` → `claude-opus`. Phase 1.
- `S15` (free model-name check): expected opencode hop count 8 → 6. Phase 1.
- `T-M6` and `T-M8` (a manual / inbox hop in an editing chain fails to load): repoint both at the **planner** chain
  and assert the planner-only error text. T1 adds the new "implementer may be manual" check. Phase 1 (see §3).

**D11 — Gate 3's clean tree ignores plan records and session logs.** New config key
`"cleanTree": { "ignore": [".ai/plans/", ".ai/context/"], "note": "Uncommitted paths under these prefixes (repo-relative, forward slashes) never block gate 3: they are records (plans, session logs), not code. Other .ai/ files are rules the implementer reads, so they still block." }`.
- `lib/gates.ps1`: new `Get-DirtyPaths -RepoPath -Ignore` — the paths from `Get-GitPorcelain` (strip the two status
  characters and the space; for a rename take the part after ` -> `), returned as two lists: `Blocking` (no prefix
  matches) and `Ignored`.
- `Cmd-Implement`, first phase only: the gate-3 check throws only on `Blocking`, with the same message as today
  listing those paths. `Ignored` is stored as `state.preexistingDirty` (an empty array when none) and printed as one
  DarkGray line.
- Post-checks: paths in `state.preexistingDirty` are removed from `$changed` and `$outside`, so a plan file that was
  already there is never reported as "changed" or "not in plan".
- `Cmd-Verify`: when `state.preexistingDirty` is non-empty, the verifier prompt gets one line before the diff:
  `Pre-existing uncommitted paths (not this run's work; ignore them in the diff): <list>`.

## 3. Phases (one gate each: the offline suite is green and the count never drops; tests in §4)

1. **Config + families** — D1, D3, D12 (S1, S15), plus from D8 **only** the `Import-OrchConfig` relaxation (a manual
   hop is forbidden only for `planner`, with the planner-only error text) and from D12 the T-M6/T-M8 repoint: S15 loads
   the real config, so D3's manual implementer hop, the validation that accepts it and the tests that assert it land
   together. Tests T1–T3.
2. **Quota exhaustion** — D2. Tests T4–T5.
3. **Suggested fix** — D4. Test T6.
4. **Session capture and continuation** — D5–D7. Tests T7–T12.
5. **Manual hop: generic, implementer, pending routing** — the rest of D8, and D9. Tests T13–T16.
6. **Clean-tree ignore list** — D11. Tests T18–T19.
7. **Docs** — D10. Test T17.

## 4. Offline tests to add

- **T1** config with a manual implementer hop loads; with a manual planner hop it throws the planner-only message.
- **T2** `Get-HopFamily`: `claude/opus` → `claude-opus`, `claude/sonnet` → `claude-sonnet`, explicit family wins;
  a review with `-PlanFamily claude-sonnet` skips the `claude-sonnet` hop, with `claude-opus` it may use it.
- **T3** `revise -PlanFile x -PlanFamily claude-sonnet` records `claude-sonnet`; without the flag keeps the previous.
- **T4** `MOCK_QUOTA_TIMES=1` (mock prints `Usage limit reached` to stderr, exit 1): class `quota-exhausted`, the hop
  is called **once**, the pool's `exhausted-until.txt` file exists, the next hop of the same pool is skipped **without a call**
  (MOCK_LOG), the next hop of another pool is called; `run.ps1 pools` prints `quota exhausted until`.
- **T5** the existing 429 tests still pass unchanged (rate limit still retried).
- **T6** the reviewer prompt contains `— FIX:`; `Get-Findings` on `1. FINDING [blocker] §3: x (doc) — FIX: y`
  returns one blocker.
- **T7** exec adapter with `sessions: true`: the review's `state.review.session` and `state.sessions.<id>.turns = 1`.
- **T8** round 2 review continues the same id; its prompt contains `follow-up in the same session` and a diff, and is
  shorter than the round-1 prompt; turns = 2.
- **T9** implement continues the read-back id when `readback.round == approved.version` and the prompt carries the
  amendments; when they differ, a fresh session and the full prompt.
- **T10** verify continues the review id; when the review was served by a hop without sessions, the full chain runs.
- **T11** `maxFollowUps` reached → fresh session, full prompt.
- **T12** a continuation that fails without repo changes falls through to the normal chain (attempt reason starts
  `continue: `).
- **T13** every implementer hop fails → `inbox/implementer.phase-1.md` + `.workspace.json`, exit 3; the test edits a
  planned file and writes the answer; `implement <id> 1` makes **no** automatic call, status `Implemented`; the same
  with an edit in a sibling repo → `Violation`.
- **T14** a pending `plan-reviewer.v<n>` request: `review` skips the automatic hops; `review -Retry` deletes the
  request and calls the first hop.
- **T15** `Get-ResumeCommand implementer.phase-2` → `implement`; the DEFERRED message prints `implement <id> 2`.
- **T16** the manual request header contains `Any agent can answer this` and the avoided family; no `Freebuff`.
- **T17** `grep -i freebuff` over `lib/*.ps1`, `run.ps1`, `orchestrator.json`, `CONDUCTOR.md` finds nothing.
- **T18** fixture repo with an untracked `.ai/plans/0001-x.md` and a modified `.ai/context/session-1.md`: `implement`
  creates the branch, `state.preexistingDirty` lists both, neither appears in the phase's `changed`/`outsidePlan`,
  and the verifier prompt carries the "Pre-existing uncommitted paths" line.
- **T19** the same fixture plus a modified `README.md` (or `.ai/coding/x.md`): `implement` refuses with the gate-3
  message naming only that file.

Mock changes: `MOCK_QUOTA_TIMES` mirrors `MOCK_RATE_LIMIT_TIMES` (own counter file `<log>.quota`). With `-Session
<id>` the mock reuses the id, else it makes `ses_mock_<8 hex>`; it prints `ORCH-SESSION: <id>` to stderr and appends
the id as a sixth MOCK_LOG column (existing readers split on tab and read fixed columns, so they are unaffected).

## 5. Hand-over

List the changed files, the suite count before/after, and anything you could not do exactly as written (with the
reason). Do not commit; the owner reviews the diff.
