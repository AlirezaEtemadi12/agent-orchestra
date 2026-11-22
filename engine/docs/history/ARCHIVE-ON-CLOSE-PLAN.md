# Plan: `close` keeps one archive file per run (ARCHIVE-ON-CLOSE)

**Status:** Draft — waiting for the owner's approval. Nothing implemented.
**Planned by:** Claude Opus 5.5, 2026-11-19. **Executor:** the same session, on the owner's go.
**Scope:** both orchestrators — `Project ProjectA/orchestrator` and `Project ProjectB/orchestrator`
(the same patch). No change to any product repo (Ui/, Web/, Agent/).
**Why now:** the dashboard (`Project Orchestra`, plan 0001) reads these archives until its phase 1
records runs live; then this archive can be removed.

## فهم من از درخواست

الان `close` بعد از ثبت بلوک ledger، کل پوشه‌ی `runs/<id>/` را پاک می‌کند. این کار عمدی بود، چون هر اجرا کلی
فایل به‌جا می‌گذاشت. ولی با این کار، جزئیات کیفیت هر run هم از بین می‌رود:
- findingهای هر دور مرور،
- متن read-back،
- و verify.

ledger فقط نتیجه‌ی آخر و هزینه را دارد. داشبورد برای بخش «کیفیت مدل‌ها» همین جزئیات را لازم دارد.

برداشت من: **پاک کردن همان‌طور که هست می‌ماند.** فقط قبل از پاک کردن، `close` **یک** فایل کوچک
JSON در `runs/_archive/<id>.json` می‌گذارد. این فایل این‌ها را دارد:
- `task.json`،
- متن مرورها، read-backها و verify (بدون نسخه‌های پلن، چون پلن نهایی در `done/` repo ثبت شده)،
- مصرف هر session که ledger همین الان حساب می‌کند.

`runs/` از قبل در gitignore هست، پس چیزی وارد git نمی‌شود. این کار موقت است. وقتی داشبورد در فاز ۱
مستقیم در SQLite ذخیره کرد، این بایگانی حذف یا خاموش می‌شود.

کارهایی که این پلن **نمی‌کند**:
- برای runهای قدیمی که قبلاً بسته شده‌اند کاری نمی‌کند. آن‌ها دیگر وجود ندارند.
- به runهای باز Project B (حدود ۳۰ پوشه) دست نمی‌زند. هر وقت بسته شوند، بایگانی می‌شوند.

## جریان کار

1. شما مثل قبل `run.ps1 close <id>` را اجرا می‌کنید.
2. `close` مثل قبل بررسی می‌کند که پلن در `done/` ثبت شده باشد، و بلوک ledger را می‌نویسد.
3. **جدید:** فایل `runs/_archive/<id>.json` نوشته می‌شود و یک خط در خروجی آن را اعلام می‌کند.
4. پوشه‌ی run و فایل‌های `_drafts` مثل قبل پاک می‌شوند.
5. اگر نوشتن بایگانی خطا بدهد، یک هشدار زرد چاپ می‌شود و **close ادامه پیدا می‌کند و پاک کردن انجام می‌شود.** دلیلش این است که بایگانی کمکی است و نباید جلوی بستن را بگیرد.

   حالت جایگزین: close متوقف شود و چیزی پاک نشود. این را در §6 می‌پرسم.
6. با `"archive": { "enabled": false }` در `orchestrator.json` کل این قابلیت خاموش می‌شود.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

بعد از هر `close`، یک فایل حدود ۵۰ تا ۱۰۰ KB در `runs/_archive/` می‌ماند و بقیه مثل قبل پاک می‌شود.

- یک run تمام‌شده را ببندید. خروجی یک خط `archived: runs/_archive/<id>.json` دارد.
- فایل را باز کنید. `task`، `reviews` (یک مورد برای هر دور)، `readbacks`، `verify` و `usage` در آن هست.
- پوشه‌ی `runs/<id>/` دیگر وجود ندارد.
- `git status` در پوشه‌ی ارکستر هیچ فایل جدیدی نشان نمی‌دهد.

## 1. Change

**`run.ps1`**
- `Write-LedgerEntry` returns the `$usage` map it already builds (session id → usage object), or
  `$null`. Callers that ignore it are unchanged.
- New `Write-RunArchive -Ctx -TaskId -State -Usage`:
  - `archive` config off → return.
  - Build `[ordered]@{ schema = 1; archivedAt; project = <workspace folder name>; task = <task.json
    object>; reviews; readbacks; verify; usage }` where `reviews` = every
    `artifacts/plan.findings.v<n>.md`, `readbacks` = every `artifacts/plan.readback.v<n>.md`, `verify`
    = `outbox/verifier/verify.md` (names checked against `run.ps1`'s `Save-Artifact` calls and a real
    Project B run, 2026-11-19) — each as `{ name; round; text }`, round parsed from the name (verify:
    none). Plan versions (`plan.v<n>.md`, `plan.approved.md`), prompts and transcripts are left out.
  - Write `runs/_archive/<id>.json` (UTF-8, no BOM) through a `.tmp` file + rename.
  - Any exception → one yellow line `[archive] not written: <message>`; never throws.
- `Cmd-Close`: `$usage = Write-LedgerEntry …`; then `Write-RunArchive …`; then the existing deletes.
  `close -Force` archives too when `task.json` is readable.

**`orchestrator.json`** — `"archive": { "enabled": true, "note": "close writes runs/_archive/<id>.json (task.json, review/read-back/verify texts, session usage) before deleting the run; read by Project Orchestra until it records runs live." }`

**`CLOSE-PLAN.md` §4b** — one sentence: before deleting, `close` also writes the archive file.

## 2. Tests (offline suite)

- **T-C** (close) gains: after a successful close, `runs/_archive/T-C-001.json` exists, parses, has
  `task.id = T-C-001`, and the run folder is gone.
- **T-AR** new: `archive.enabled = false` → no file; an unwritable `_archive` (a file named `_archive`
  in its place) → close still succeeds, prints `[archive] not written`, and the run is deleted.
- Suite once at the end, in both orchestrators.

## 3. Rules reviewed (owner's rule: a change reviews the rules around it)

- `Cmd-Close`'s header comment says the run is "working state only; the durable record is the plan
  filed in its repo". It becomes: the filed plan is the record; the archive is a temporary data copy for
  the dashboard. No other rule overlaps.

## 4. Stop and ask

- A run's artifact names differ from `plan.findings.v<n>.md` / `plan.readback.v<n>.md` → list what was
  found and ask before guessing.

## 5. Order

1. Project A orchestrator: code, config, CLOSE-PLAN, tests; suite green.
2. Same patch to Project B; suite green.
3. Commit both on `main`, exact paths, only when the owner says so.

## 6. Question for the owner

**Archive write fails → close continues (default above) or close stops and deletes nothing?**
I recommend *continue*: the archive is a convenience and `close` is part of the close-plan routine.
