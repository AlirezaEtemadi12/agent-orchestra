# Quota pools (free vs paid; one agent, one call at a time) + the plan-writing rules in DrivingScore

Status: **draft — awaiting the owner's approval** (written 2026-10-30 by the conductor session, Claude).
Executor: Freebuff (DeepSeek), in two sessions. Part A runs from the Project B workspace; Part B runs from the
DrivingScore workspace, **after Part A is done** (B1 ports A's code).

---

## فهم من از درخواست

دیروز چند اجرای ارکستر را هم‌زمان راه انداختم (مرور ۰۱۰۵ تا ۰۱۰۸ و ۰۰۳۱). همه به یک حساب رایگان خوردند و با
«too many requests» (۴۲۹) رد شدند. ارکستر امروز هیچ مفهومی از «سهمیه» ندارد. هر `run.ps1` یک پردازش جداست و
هیچ‌کدام از دیگری خبر ندارد. تنها دفاعش این است که بعد از ۴۲۹ یک بار، ۲۰ ثانیه بعد، دوباره تلاش می‌کند.

برداشت من از خواستهٔ شما:

۱. **هر عامل (و در صورت نیاز هر مدل) برچسب «رایگان» یا «پولی» داشته باشد.** اجراهایی که به یک عامل (یک سهمیه)
   می‌رسند **پشت سر هم** اجرا شوند، نه با هم، چه رایگان باشد چه پولی. دومی صبر می‌کند تا اولی تمام شود و در این مدت می‌گوید منتظر کیست.

۲. **«کلی‌تر»:** چون رفتار عامل‌ها فرق دارد، فقط یک کلید رایگان/پولی کافی نیست. چند چیز باید برای هر عامل
   قابل تنظیم باشد:
   - چند اجرای هم‌زمان مجاز است؛
   - بین دو درخواست چقدر فاصله بماند؛
   - بعد از ۴۲۹ چند بار و با چه فاصله‌ای دوباره تلاش کند؛
   - اگر سهمیه مشغول بود، منتظر بماند یا سراغ عامل بعدی زنجیره برود.

   «رایگان» و «پولی» دو **پیش‌فرض** برای این مقادیرند که هر عامل یا مدل می‌تواند بازنویسی‌شان کند.

۳. **کدام رایگان است، و قاعدهٔ «یک عامل، پشت سر هم».** opencode، codebuddy و qwen رایگان‌اند. **claude پولی
   است** (اشتراک Pro؛ نسخهٔ رایگان ندارد). فری‌باف دستی اجرا می‌شود (inbox) و ارکستر خودش صدایش نمی‌زند، پس صف
   برایش معنی ندارد. پاسخ شما (۲۰۲۶-۱۰-۳۰) قاعدهٔ اصلی را روشن کرد: **درخواست هم‌زمان به عامل‌های مختلف
   مجاز است، ولی به یک عامل همیشه پشت سر هم.** پس هر دو پیش‌فرض، رایگان و پولی، یک جای هم‌زمان برای هر عامل
   دارند. claude هم دیروز ۴۲۹ داد، پس تفاوت پولی با رایگان در *نحوهٔ درخواست* است، نه در صف:
   - پولی فاصلهٔ اجباری بین درخواست‌ها ندارد؛
   - بعد از ۴۲۹ یک بار با فاصلهٔ کوتاه‌تر دوباره تلاش می‌کند و بعد سراغ عامل بعدی زنجیره می‌رود، چون سقف
     مصرف اشتراک با صبر چنددقیقه‌ای باز نمی‌شود.

   اجرای واقعاً هم‌زمان روی یک عامل فقط وقتی ممکن است که در تنظیمات همان عامل صریحاً خواسته شود.

۴. **«سهمیه» یعنی حساب، نه مدل.** مثلاً همهٔ مدل‌های رایگان opencode یک سهمیه را مصرف می‌کنند. پس صف پیش‌فرض
   برای هر عامل یکی است و یک مدل فقط وقتی صف جدا می‌گیرد که در تنظیمات گفته شود.

۵. **تغییرات پلن‌نویسی برای DrivingScore** همان سه بخشی است که در Project B گذاشتیم:
   - «فهم من از درخواست» (فهم خود برنامه‌ریز، نه کپی حرف شما)؛
   - «جریان کار»؛
   - «نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید».

   به‌علاوهٔ ترتیب «فهم ← پلن ← نمایش ← مرور ← تأیید ← پیاده‌سازی» در نقش planner، گیت ۲ و CONDUCTOR.
   همچنین صف سهمیه را به ارکستر DrivingScore هم منتقل می‌کنم، چون همان ارکستر است.

**کارهایی که این پلن نمی‌کند:**
- پولی/رایگان را از خود سرویس‌ها نمی‌پرسد؛ برچسب دستی در تنظیمات است.
- مصرف یا هزینه را نمی‌شمارد.
- به فری‌باف دست نمی‌زند.
- پلن‌های در جریان (۰۱۰۵ تا ۰۱۰۸) را عوض نمی‌کند.

## جریان کار

۱. شما (یا من) چند اجرای ارکستر را با هم راه می‌اندازید، مثلاً مرور دو پلن. اجراهایی که به عامل‌های مختلف
   می‌رسند واقعاً با هم جلو می‌روند؛ فقط آن‌هایی که به یک عامل می‌رسند در صف می‌ایستند.

۲. هر اجرا پیش از صدا زدن یک عامل خودکار، از «صف» آن عامل یک جای خالی می‌گیرد. صف رایگان یک جا دارد.
   - **جا خالی است:** می‌گیرد و اجرا می‌کند.
   - **جا پر است:** در ترمینال و در `progress.json` می‌نویسد «منتظر صف opencode، در اختیار T-AGENT-024 از ساعت ۱۰:۰۲».
     هر چند ثانیه دوباره نگاه می‌کند.

۳. وقتی اجرای اول تمام شد (یا بسته یا کشته شد)، جا خودبه‌خود آزاد می‌شود و اجرای دوم شروع می‌کند. اگر برای آن
   عامل «فاصلهٔ حداقل بین درخواست‌ها» تعریف شده باشد، اول آن فاصله را صبر می‌کند.

۴. اگر جواب ۴۲۹ آمد:
   - با فاصلهٔ فزاینده (مثلاً ۶۰ و بعد ۱۲۰ ثانیه) دوباره تلاش می‌کند؛
   - برای همان صف «زمان خنک شدن» ثبت می‌کند، تا اجراهای منتظر هم تا آن زمان به همان سهمیهٔ تمام‌شده نکوبند؛
   - بعد از تلاش‌های مجاز، مثل امروز سراغ عامل بعدی زنجیره می‌رود.

۵. اگر برای یک عامل «مشغول بود، برو بعدی» تنظیم شده باشد، اجرا منتظر نمی‌ماند و سراغ عامل بعدی می‌رود. دلیل
   این رد شدن در گزارش اجرا ثبت می‌شود.

۶. دستور جدید `run.ps1 pools` همهٔ صف‌ها را نشان می‌دهد: رایگان/پولی، چند جا، چه کسی جا را گرفته، و تا کی خنک
   می‌شود.

**فازها:**
- **A1:** صف و تنظیمات در ارکستر Project B، با تست‌های آفلاین.
- **A2:** مستندات (CONDUCTOR، DESIGN).
- **B1:** انتقال همان کد به ارکستر DrivingScore.
- **B2:** سه بخش پلن‌نویسی و ترتیب کار در DrivingScore.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

اجراهای هم‌زمان ارکستر دیگر به یک سهمیهٔ رایگان با هم نمی‌خورند: پشت سر هم اجرا می‌شوند و منتظر بودنشان را
می‌گویند. هر عامل در `orchestrator.json` برچسب و حدود خودش را دارد. پلن‌های DrivingScore هم از این به بعد با فهم
برنامه‌ریز، جریان کار و چک‌لیست تست شروع می‌شوند.

- [ ] `powershell -NoProfile -File orchestrator/run.ps1 pools` (در هر دو workspace): opencode و codebuddy با
      برچسب `free`، claude با برچسب `paid`، هرکدام با ۱ جا، همه خالی.
- [ ] دو مرور را با هم راه بیندازید: دومی «منتظر صف …» می‌نویسد و فقط بعد از تمام شدن اولی شروع می‌کند. `pools`
      در همان لحظه نشان می‌دهد صف در اختیار کیست.
- [ ] اجرای اول را وسط کار ببندید (Ctrl+C یا بستن پنجره): دومی ظرف چند ثانیه شروع می‌کند.
- [ ] دو اجرا که اولین عاملشان فرق دارد (مثلاً مرور با codebuddy و برنامه‌ریزی با claude): هر دو با هم شروع
      می‌کنند و هیچ‌کدام منتظر نمی‌ماند.
- [ ] در `orchestrator.json` برای یک عامل `"maxConcurrent": 0` بگذارید: دو اجرا روی همان عامل با هم شروع می‌شوند.
- [ ] در DrivingScore، `Web/.ai/plans/TEMPLATE.md` (و Mobile و Ui) سه بخش فارسی را پیش از بخش ۰ دارند، و
      `CONDUCTOR.md` گام 3b «اول نمایش، بعد مرور» را دارد.

---

## 0. Before the first edit (both parts)

- **No run in flight.** Check both of these:
  - `Get-CimInstance Win32_Process -Filter "Name='powershell.exe'" | ? CommandLine -match 'run\.ps1'` returns
    nothing;
  - no `orchestrator/runs/*/.lock` exists.

  Otherwise STOP and say so. The Agent implementation of 0105–0108 is a Freebuff session, not a `run.ps1`
  process, so it does not block this — but **never touch `Agent/`, `Web/`, `Ui/`** in Part A.
- `orchestrator/` is a git repo in both workspaces. Record where each one stands:
  - Project B's is on `main`, with one uncommitted change in `CONDUCTOR.md` (the earlier plan-rule edit, not
    yours — keep it).
  - DrivingScore's is on `port/project-b`.

  Run `git status ; git branch --show-current`, then `git switch -c alireza/quota-pools` from the current
  `HEAD`. Uncommitted changes travel with the switch; never stash or revert them.
- Baseline: `powershell -NoProfile -File orchestrator/tests/offline/run-offline.ps1`. Record the real
  passed/total. Red is a STOP.
- **Never call a real model.** No `run.ps1 health`, no `new`/`review`. Every test goes through
  `tests/offline/mock-agent.ps1`.
- Never commit, push, merge or stash, and never `git add -A`. Hand over uncommitted.

## 1. Design decisions

| # | Decision |
|---|---|
| D1 | **A pool is a shared quota**, named by `pool`. Resolution: the hop's `pool`, else the adapter's `pool`, else the adapter's name. So every free opencode model shares `opencode`. |
| D2 | **Limits resolve hop → adapter → `tiers[<tier>]` → built-in.** Keys: `tier` (`free`\|`paid`), `maxConcurrent` (0 = unlimited), `minGapSec`, `rateLimitRetries`, `rateLimitBackoffSec`, `whenBusy` (`wait`\|`next`), `queueWaitSec`. The built-in values (no `tier`, no `tiers` block) are exactly today's behaviour: `maxConcurrent 0`, `minGapSec 0`, `rateLimitRetries 1`, `rateLimitBackoffSec = defaults.retryDelaySec` (20), `whenBusy wait`, `queueWaitSec 7200`. An old config therefore behaves as it does now. |
| D3 | **Shipped tiers:**<br>• `free` — `maxConcurrent 1`, `minGapSec 15`, `rateLimitRetries 2`, `rateLimitBackoffSec 60`, `whenBusy wait`, `queueWaitSec 7200`.<br>• `paid` — `maxConcurrent 1`, `minGapSec 0`, `rateLimitRetries 1`, `rateLimitBackoffSec 30`, `whenBusy wait`, `queueWaitSec 7200`.<br>**Both tiers run one hop at a time per pool.** The owner's rule (2026-10-30): different agents may be called at once, but one agent is always called one after another. The tiers differ only in *how* they call: no forced gap for paid; for paid, one short retry after a 429 and then the next hop, because a subscription's usage cap does not reopen within minutes. `maxConcurrent 0` (unlimited) or `>1` exists only as an explicit per-adapter or per-hop override.<br>Adapters: `opencode`, `codebuddy`, `qwen` → `"tier": "free"`; `claude` → `"tier": "paid"` (Pro subscription — it has no free tier, but it still returned 429 on 2026-10-29, hence one slot). `manual` gets no tier. |
| D4 | **Inbox hops never take a slot** (`Get-AdapterTransport` = `inbox`): a human answers them, and they do not wait. |
| D5 | **A slot is an OS file lock, so it is cross-process and dies with its holder.**<br>• Slots live in `orchestrator/runs/_pool/<pool>/slot-<n>.lock`, `n = 1..maxConcurrent`. `<pool>` has every character outside `[A-Za-z0-9._-]` replaced by `_`.<br>• To take a slot: `[System.IO.File]::Open(path, OpenOrCreate, ReadWrite, FileShare.None)`, catching `IOException`.<br>• The slot is held for the whole hop, including its rate-limit retries and their sleeps, and released in a `finally`.<br>• A killed process frees its slot because the OS closes its handle. That is why there is no stale-lock clean-up and no PID check.<br>• `runs/` is already git-ignored. |
| D6 | **Who holds a slot** is written beside it as `slot-<n>.json`: `taskId`, `role`, `adapter`, `model`, `pid`, `since` (ISO, local). It is written after the lock is taken and deleted on release. A reader trusts the `.json` only while its `.lock` cannot be opened, so a leftover `.json` from a killed process is ignored (and overwritten by the next holder). |
| D7 | **Gap.** `runs/_pool/<pool>/last-start.txt` holds the last hop start (UTC ticks). After taking a slot, sleep `minGapSec − elapsed` if positive, then write the new start. |
| D8 | **Cool-down.** After a rate-limited attempt, write `cooldown-until.txt` (UTC ticks), set to now + the backoff about to be slept. `Enter-PoolSlot` treats a pool whose cool-down is still running as busy, in both modes, and the wait message says «cooling down until HH:mm:ss». |
| D9 | **Retries.** Today's `if ($fail -eq 'rate-limit' -and $tries -lt 2)` becomes `$tries -le $limits.RateLimitRetries`. The sleep is `RateLimitBackoffSec * 2^(tries-1)` (free: 60, then 120). |
| D10 | **Busy handling.**<br>• `wait`: poll every 5 s. Print the wait line once, and again only when the holder changes. Write `progress.json` with `phase: "queued"`, `pool`, `heldBy`. After `queueWaitSec`, the hop fails with class `queue-timeout` and the chain moves on.<br>• `next`: no wait; the attempt is recorded as `skipped: pool <p> busy (held by <taskId> <role>)` and the chain moves on.<br>Neither counts as a failure of the model. |
| D11 | **`health`** never waits. A pool that is busy reports `busy (held by …)` for that adapter instead of pinging. A free pool is taken for the ping and released after it. |
| D12 | **New command `run.ps1 pools`.** For every pool that the config's chains name, one line: `pool  tier  slots used/max  holder(s)  cooldown`. Slots are read live (try-open, then release at once). |
| D13 | **All new logic lives in one new file, `lib/pool.ps1`**, so that B1 is a file copy plus the same few hook lines. It is dot-sourced wherever `lib/agents.ps1` is. Its functions:<br>• `Get-HopLimits -Ctx -Hop` → `[pscustomobject]` with `Tier, Pool, MaxConcurrent, MinGapSec, RateLimitRetries, RateLimitBackoffSec, WhenBusy, QueueWaitSec`;<br>• `Get-PoolDir -Ctx -Pool`;<br>• `Enter-PoolSlot -Ctx -Limits -Meta -ProgressFile -NoWait` → `@{ Status = 'acquired'\|'unlimited'\|'busy'\|'timeout'; Handle; HeldBy }`;<br>• `Exit-PoolSlot -Handle`;<br>• `Set-PoolCooldown -Ctx -Pool -Seconds`;<br>• `Get-PoolStatus -Ctx`. |
| D14 | **Test seam.** `$env:ORCH_POOL_POLL_MS` overrides the 5 s poll, and `$env:ORCH_POOL_BACKOFF_SCALE` (a double, default 1) multiplies every backoff and gap sleep, so the offline suite runs in seconds. Production never sets them. |
| D15 | Version: `orchestrator.json` `version` → `0.6.0` in both workspaces, with the `note` gaining "quota pools (QUOTA-POOLS-PLAN.md)". |

## 2. Part A — Project B orchestrator

### A1 — code, config, tests

| File | Change |
|---|---|
| **NEW** `orchestrator/lib/pool.ps1` | D1–D14. |
| `orchestrator/lib/agents.ps1` `Invoke-Role` | At the top of each hop's loop, after the family skip and before `$tries = 0`: `$limits = Get-HopLimits $Ctx $hop`. For a non-inbox hop, call `Enter-PoolSlot`:<br>• `busy` → record the attempt (D10) and `continue`;<br>• `timeout` → record it as `queue-timeout` and `continue`.<br>Wrap the existing `while ($true) { … }` in `try { … } finally { Exit-PoolSlot $slot.Handle }`. Replace the retry condition and sleep (D9) and call `Set-PoolCooldown` before the sleep (D8). Add `pool` and `tier` to `$progressMeta` and to every attempt object. Nothing else in the function changes. |
| `orchestrator/run.ps1` | Dot-source `lib/pool.ps1` next to `lib/agents.ps1`. Add `Cmd-Pools` (D12) to the dispatcher and to `Show-Usage`. `Cmd-Health` uses `Enter-PoolSlot -NoWait` (D11). `Cmd-Status` / `Cmd-Queue` show `queued for pool <p> (held by …)` when `progress.json` says `phase: "queued"`. |
| `orchestrator/orchestrator.json` | Add the `tiers` block (D3) after `adapterTransport`, with a `note` that explains the keys in one sentence each. Add `"tier": "free"` to opencode, codebuddy and qwen, and `"tier": "paid"` to claude. Bump the version (D15). No chain changes. |
| `orchestrator/tests/offline/run-offline.ps1` | The new cases below. They use a temp `runs/` (the suite's own workspace) and the D14 seams. Two-process cases start the second caller (a `run.ps1` command, or a small `powershell -NoProfile -Command` holder that dot-sources `lib/pool.ps1`) with `Start-Process` and read the timing from the mock log. |
| `orchestrator/tests/offline/mock-agent.ps1` | Only if a case needs it: `MOCK_RATE_LIMIT_TIMES=<n>` prints `429 Too Many Requests` and exits 1 for the first n calls (count kept in a file next to `MOCK_LOG`). |

**New offline cases (the minimum; name them in the suite's style):**
1. Limits resolve hop → adapter → tier → built-in. A config with no `tiers` and no `tier` gives D2's built-in values.
2. Two callers on one `free` pool: the second's start is after the first's end, and it printed the wait line naming the holder.
3. Two pools: both start without waiting.
4. A `paid` adapter with the shipped tier: two callers on it run one after the other (one slot). The same adapter with `"maxConcurrent": 0`: two callers overlap, and no `.lock` file is created.
5. `whenBusy: next`: the busy hop is skipped with D10's reason and the next hop answers.
6. `queueWaitSec` (set to 1) runs out: `queue-timeout`, and the next hop answers.
7. Rate limit: with `rateLimitRetries 2`, `MOCK_RATE_LIMIT_TIMES=2` gives three calls and success; with `MOCK_RATE_LIMIT_TIMES=3`, the chain moves on after three calls.
8. Cool-down: while one caller is in its backoff, a second caller on that pool waits, and says «cooling down».
9. A holder process killed with `Stop-Process -Force`: the next caller gets the slot within two polls.
10. An inbox (`manual`) hop takes no slot, even in a `free`-tier chain.
11. `run.ps1 pools` lists each configured pool with tier and `0/1`, and shows the holder while a slot is held.
12. `progress.json` says `phase: "queued"` with `pool` and `heldBy` while a caller waits.

**Gate A1:** the offline suite is green at **baseline + at least 12**, and every earlier case still passes.

### A2 — docs

| File | Change |
|---|---|
| `orchestrator/CONDUCTOR.md` § Running plans in parallel | Replace the last bullet ("Each parallel run costs its own Freebuff read-back and model quota…") with:<br>• **Start parallel runs freely; the orchestrator queues them.** A `free` pool runs one hop at a time across every run, and a waiting run prints whom it is waiting for. Do not hand-stagger runs, and do not hand a run to Freebuff because another one holds the pool. Check with `run.ps1 pools`.<br>• **A 429 storm means a pool's limits are wrong**, not that runs must be started by hand: raise `minGapSec` / `rateLimitBackoffSec` for that adapter in `orchestrator.json`.<br>• Each parallel run still costs its own Freebuff read-back. |
| `orchestrator/DESIGN.md` | A new section "Quota pools" — D1–D14 in prose, short. |
| `orchestrator/run.ps1` `Show-Usage` | `pools` (done in A1). |

**Gate A2:** the suite is still green, and `run.ps1 pools` runs against the real `orchestrator.json` without
calling a model (it only opens lock files).

**Handover A:** write `orchestrator/runs/_drafts/QUOTA-POOLS.freebuff-impl.md` with:
- the baseline and final suite counts;
- the changed files;
- the `run.ps1 pools` output;
- every deviation.

## 3. Part B — DrivingScore (`<workspace>`)

### B1 — port the pools

- Read Part A's finished files in Project B (**read-only**) and its handover first.
- Copy `lib/pool.ps1` as is. Apply the same hook lines in DrivingScore's own `Invoke-Role`, `run.ps1`
  dispatcher, `Cmd-Health` and `Cmd-Status`/`Cmd-Queue`. The two copies of `run.ps1` differ a lot, so place the
  lines by meaning, not by line number.
- Make the same `orchestrator.json` edit. DrivingScore's chains end in a `manual` hop, which stays untiered (D4).
- Port the 12 offline cases, and the mock-agent switch if A added it.
- Make the same CONDUCTOR/DESIGN edits.

**Gate B1:** DrivingScore's offline suite is green at its own baseline + at least 12, and `run.ps1 pools` runs.

### B2 — the plan-writing rules

Copy the three sections, with their Persian headings and English guidance, **verbatim** from
`Project ProjectB\Agent\.ai\plans\TEMPLATE.md` (the block from `## فهم من از درخواست` through the end of the
`## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید` guidance). Then adjust only the relative links to the target
file's own location.

| File (DrivingScore) | Change |
|---|---|
| `Web/.ai/plans/TEMPLATE.md`, `Mobile/.ai/plans/TEMPLATE.md`, `Ui/.ai/plans/TEMPLATE.md` | The three sections, inserted directly **before** `## 0. Ground rules for the executor`, after the `---` above it. |
| `Skills/plan-workflow/roles/planner.md` § "The plan is reviewed before the user approves it" | Prepend Project B `Agent/.ai/agents/planner.md` § Exit's first two sentences ("Order: **understand → plan → show → review → implement.** … not an approval."), with the link to this repo's template. |
| `Mobile/.ai/agents/planner.md`, `Web/.ai/agents/planner.md` | The same sentences, in their section that hands the plan to review. Ui has no `planner.md`: put them in `Ui/.ai/agents/README.md` § `## planner`. |
| `Skills/plan-workflow/SKILL.md` § Gate 2, and `Web/`, `Mobile/`, `Ui/` `.ai/agents/workflow.md` § Gate 2 | The paragraph «**The owner checks the understanding first, then the plan is reviewed, then it is presented.** …» from Project B `Agent/.ai/agents/workflow.md` gate 2. Where a file has "The plan is reviewed before it is presented", replace that opening the way Project B did. Where it has no such paragraph, add it as the gate's first paragraph after "Present the plan…". |
| `Skills/plan-workflow/prompts/plan.md` | One line: the plan starts with the three sections, and the planner shows them to the owner before review. |
| `orchestrator/CONDUCTOR.md` step 3 + new 3b | Exactly Project B `orchestrator/CONDUCTOR.md`'s current step 3 tail and step 3b. In step 3, keep DrivingScore's own "shared shape (`Skills/plan-workflow/SKILL.md` …)" wording. |
| `Skills/plan-workflow/CHANGELOG.md` | One dated entry: 2026-10-30, the three plan sections and the show-before-review order. |

**Repo hygiene for B2.** `Web/`, `Mobile/` and `Ui/` are git repos. In each one:
- run `git status ; git branch --show-current`;
- STOP if the tree is dirty;
- otherwise run `git switch -c alireza/plan-understanding-sections` before the edit.

`Skills/` and the workspace root are not repos. Do not commit anything.

**Gate B2:**
- `grep -c "فهم من از درخواست"` is ≥ 1 in each of the three TEMPLATE.md files, in `Skills/plan-workflow/roles/planner.md`,
  in the three gate-2 docs, in SKILL.md and in CONDUCTOR.md;
- every relative link you added resolves;
- no other line changed (`git diff --stat` per repo).

**Handover B:** `orchestrator/runs/_drafts/QUOTA-POOLS-and-PLAN-RULES.freebuff-impl.md` in DrivingScore, in
Handover A's shape, plus the per-repo branches.

## 4. Stop and ask

- A run in flight, a red baseline, or a dirty DrivingScore repo.
- `Invoke-Role` in either workspace does not have the shape described in A1 (one `while ($true)` retry loop per
  hop, with a `rate-limit` retry).
- A test needs a real model, or a real network call.
- Anything that would change a chain, a role, or a gate command.
