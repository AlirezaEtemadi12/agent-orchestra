# Plan 0008 — Hosting on the personal server: sign-in, HTTPS, backups, sync

**Status:** Dropped — the owner wants no server (2026-11-24, [ADR 0009](../../.ai/decisions/0009-local-app-sets-projects-up.md)); hosting is an idea again ([`../ideas/hosting.md`](../ideas/hosting.md))
**Created:** 2026-11-19 · **Updated:** 2026-11-24 · **Done:** —
**Preview:** none of its own — the roadmap's phase 3 (approved 2026-11-19)
**Run:** —
**Planned by:** Claude Opus 5.5, 2026-11-19. **Executor:** another agent, ideally a cheaper model.
**Plan review:** — (a different model family than the planner's)
**Decided by:** — (gate 2)
**Branch:** `alireza/hosting` — the implementer creates it.
**Base:** `main` after plans 0004 and 0006 are done
**Companion:** None — the engine is in this repo (0005); the `sync` and `profile pull` commands are
written in `engine/`.
**Steps changed:** no preview of its own (owner, 2026-11-19)

---

## فهم من از درخواست

داشبورد روی سرور شخصی شما اجرا می‌شود تا از هر جا، حتی موبایل، در دسترس باشد. این‌ها لازم است:
- یک کاربر با ورود،
- HTTPS از reverse proxy خود سرور،
- و پشتیبان‌گیری روزانه از دیتابیس.

ارکستر همچنان روی ویندوز شما اجرا می‌شود و فقط **گزارش** می‌دهد. سرور هیچ‌وقت به ویندوز فرمان نمی‌دهد (ADR 0002).

یک واقعیت که طراحی را تعیین می‌کند: **سرور به فایل‌های ویندوز دسترسی ندارد.** پس روش فاز ۰ (خواندن ledger و
`task.json` و کتابخانه از دیسک) روی سرور کار نمی‌کند. برداشت من این است:
- runها از سه راه به سرور می‌رسند: رویدادهای فاز ۱، یک دستور `sync` که ledger و runهای باز را می‌فرستد، و
  (طبق جواب شما در §8) فهرست کتابخانه.
- پروفایل‌ها را موتور با یک دستور صریح قبل از run دانلود می‌کند، نه وسط run.

این پلن کارهای زیر را **نمی‌کند**:
- passkey ندارد (بعداً).
- چند کاربره نیست.
- هیچ مسیری از سرور به ویندوز ندارد.

## جریان کار

1. روی سرور، `deploy/docker-compose.server.yml` را با یک `.env` اجرا می‌کنید. این `.env` شامل این‌هاست:
   - رمز ورود به شکل hash (با یک دستور ساخته می‌شود)،
   - توکن گزارش،
   - توکن خواندن پروفایل،
   - آدرس عمومی.

   reverse proxy سرور، HTTPS را به پورت ۳۵۰۰ روی `127.0.0.1` می‌فرستد.
2. صفحه را باز می‌کنید و وارد می‌شوید. ۵ بار رمز اشتباه، ورود را ۱۵ دقیقه قفل می‌کند. خروج در منوی هدر است.
3. روی ویندوز، `report.url` موتور را به آدرس سرور می‌دهید. رویدادها مثل فاز ۱ می‌رسند.
4. `run.ps1 sync` بلوک‌های تازه‌ی ledger و `task.json`های باز را می‌فرستد. همان قاعده‌ی fire-and-forget را
   دارد، و دوباره فرستادن چیزی را تکرار نمی‌کند. آن را دستی می‌زنید، یا `close` و `verify` بعد از نوشتن ledger
   خودشان صدایش می‌زنند.
5. `run.ps1 profile pull` آخرین نسخه‌ی پروفایل را از سرور می‌گیرد و در پوشه‌ی پروفایل‌های محلی می‌گذارد. اگر
   سرور در دسترس نباشد، نسخه‌ی محلی قبلی استفاده می‌شود.
6. هر شب ساعت ۳، یک کپی از دیتابیس در `/data/backups/` ساخته می‌شود و ۱۴ کپی آخر نگه داشته می‌شود.

فازها:
- **A:** ورود و حالت سرور.
- **B:** sync و دریافت پروفایل.
- **C:** پشتیبان و راه‌اندازی.

## نتیجه و آنچه بعد از پیاده‌سازی تست می‌کنید

- آدرس سرور را باز کنید. بدون ورود فقط صفحه‌ی ورود دیده می‌شود، و هر `/api/*` جواب 401 می‌دهد.
- با رمز درست وارد شوید. همه‌ی صفحه‌ها کار می‌کنند. ۵ بار رمز اشتباه، ورود را قفل می‌کند.
- یک run روی ویندوز بزنید. روی سرور در تخته‌ی runها دیده می‌شود. بعد از `close` و `sync`، run با هزینه‌اش
  در فهرست هست.
- دو بار پشت‌سرهم `sync` بزنید. تعدادها تغییر نمی‌کند.
- پروفایل را روی سرور عوض کنید و `profile pull` بزنید. run بعدی نسخه‌ی تازه را دارد. سرور را خاموش کنید:
  `profile pull` یک هشدار می‌دهد و run با نسخه‌ی محلی اجرا می‌شود.
- فردا صبح در `/data/backups/` یک فایل تازه هست. راهنمای restore را یک بار روی یک کپی امتحان کنید.

## 0. Ground rules for the executor

1. Follow [`../workflow.md`](../workflow.md).
2. No secret in any committed file: password hash, tokens and URLs come from `.env` (git-ignored) or
   environment variables.
3. Local mode (no `Orchestra:Mode` or `Local`) behaves exactly as before this plan: no sign-in, file
   import on.

### Baseline

Plans 0001–0006 gates and the offline suite green; counts recorded.

## 1. Goal

A server mode with one-user sign-in, push-based data (events + sync) instead of file import, profile
pull for the engine, nightly backups, and a compose file for the server.

## 2. Out of scope

Passkeys, more users, OAuth; the server reaching the Windows machine; mobile app; full plan/review texts
unless §8 question 1 says so.

## 3. Rules that apply

ADRs 0002, 0003, 0005, 0006, 0007; Web `.ai/architecture/authentication.md` and ADR 0013 of Project B
(forwarded headers behind a proxy) as the reference for proxy trust.

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | `Orchestra:Mode` = `Local` (default) or `Server`. Server: sign-in required, file import **off** (`ImportOnStart` ignored, `POST api/import` → 404), CORS off |
| D2 | One user: `Orchestra:Auth:User` and `Orchestra:Auth:PasswordHash` (ASP.NET `PasswordHasher` v3 format), produced by `dotnet Orchestra.Api.dll hash-password` (reads the password from stdin, prints the hash) |
| D3 | Cookie auth: `HttpOnly`, `Secure`, `SameSite=Strict`, 14-day sliding expiry; every non-GET under `/api` also needs the header `X-Orchestra: 1` (a cross-site form cannot send it) |
| D4 | Lockout: 5 failures → 15 min, kept in memory; `POST api/auth/login` rate-limited to 10/min per client IP |
| D5 | Three credentials, three scopes: the user cookie (everything except events/sync), the **report token** (`POST api/events`, `POST api/sync`), the **profile token** (`GET api/profiles/{id}/export` only) |
| D6 | Forwarded headers trusted only from `Orchestra:Proxy:KnownProxies` (IP list); the container binds `127.0.0.1:3500` on the server |
| D7 | `sync` sends the ledger as raw blocks and each open `task.json` as JSON; the server runs the **same parser and assembler** as the local import on what it received, stored per project as `SyncedLedger` / `SyncedTask` rows — so a sync is idempotent (blocks keyed by heading text + content hash) |
| D8 | Backups: `VACUUM INTO '/data/backups/orchestra-yyyyMMdd.db'` at 03:00 server time by a hosted service; keep 14; restore = stop, copy the file over `orchestra.db`, start (written in `deploy/README.md`) |
| D9 | `profile pull` is explicit (and run by `new` once per day when `profileSource.url` is set); never during a run; a failure falls back to the local file with one warning |

## 5. File manifest

| # | File | NEW/EDIT | Phase |
|---|---|---|---|
| 1 | `api/src/Orchestra.Api/Common/OrchestraOptions.cs` — `Mode`, `Auth`, `Proxy`, `ProfileToken`, `PublicUrl` | EDIT | A |
| 2 | `api/src/Orchestra.Api/Modules/Auth/**` — login, logout, me, lockout, `hash-password` command | NEW | A |
| 3 | `api/src/Orchestra.Api/Extensions/*` — auth, forwarded headers, the `X-Orchestra` check, mode switches | EDIT | A |
| 4 | `ui/src/features/auth/**`, page `/{locale}/login/`, the client guard in `AppShell`, logout in the header | NEW/EDIT | A |
| 5 | `api/src/Orchestra.Api/Modules/Sync/**` — `POST api/sync`, `SyncedLedger`, `SyncedTask`, assembler input from them in server mode; migration `Sync` | NEW | B |
| 6 | `api/src/Orchestra.Api/Modules/Profiles/Features/ExportProfile/*` — `GET api/profiles/{id}/export` (profile token) | NEW | B |
| 7 | `engine/lib/report.ps1` — `Send-OrchestraSync`; `engine/run.ps1` — `sync`, `profile pull`; hooks after `Write-LedgerEntry` | EDIT | B |
| 8 | `engine/tests/offline/*` — sync and pull with an unreachable URL; idempotent payload | EDIT | B |
| 9 | `api/src/Orchestra.Api/Modules/Backup/BackupService.cs` | NEW | C |
| 10 | `deploy/docker-compose.server.yml`, `deploy/.env.server.example`, `deploy/README.md` | NEW | C |
| 11 | `api/tests/Orchestra.Api.Tests/{Auth,Sync,Backup}/**` | NEW | A–C |
| 12 | `.ai/architecture/overview.md` (server column), `.ai/decisions/0008-server-mode-is-push-only.md` | EDIT/NEW | A |

## 6. Per-file specification

### 6.1 Auth API

| Endpoint | Does |
|---|---|
| `POST api/auth/login` `{ user, password }` | 204 + cookie; wrong → 401 `Auth.Login.Failed` (same answer for a wrong user or a wrong password); locked → 423 `Auth.Login.Locked` with `retryAfter` |
| `POST api/auth/logout` | 204, cookie cleared |
| `GET api/auth/me` | `{ user }` or 401 |

| Code | Status | en | fa |
|---|---|---|---|
| `Auth.Login.Failed` | 401 | Wrong user name or password. | نام کاربری یا رمز اشتباه است. |
| `Auth.Login.Locked` | 423 | Too many tries. Try again in {minutes} minutes. | تلاش زیاد بود. {minutes} دقیقه‌ی دیگر دوباره امتحان کنید. |
| `Auth.Required` | 401 | Sign in first. | اول وارد شوید. |

UI: `/{locale}/login/` (user, password, submit with spinner); `AppShell` calls `me` on load in server
mode and on any 401 redirects to login with `returnUrl` (same-app paths only, as Project B's
`safeReturnUrl`). The client learns the mode from `GET api/health` (`{ status, mode }`).

| Key | en | fa |
|---|---|---|
| `auth.title` | Sign in | ورود |
| `auth.user` | User name | نام کاربری |
| `auth.password` | Password | رمز |
| `auth.submit` | Sign in | ورود |
| `auth.logout` | Sign out | خروج |

### 6.2 Sync

`POST api/sync` (report token) `{ schema: 1, project, ledger: { blocks: string[] }, tasks: object[] }`
≤ 2 MB. Each block is stored once by `(project, sha256(block))`; tasks replace the project's previous
set. After storing, the server runs the plan 0001 import for that project with `SyncedLedger`/`SyncedTask`
as the source instead of the disk. The engine's `sync` sends all blocks newer than the last 200 (the
server ignores those it has), and every `runs/T-*/task.json`.

### 6.3 Engine

`run.ps1 sync` and `run.ps1 profile pull` — both use `report.url`'s host, `Invoke-RestMethod` with the
configured timeout, no retry, one warning on failure, exit 0 either way (a failed sync never fails a
command). `profile pull` writes `<profilesDir>/<id>.json` atomically only when the downloaded version is
newer than the local one.

## 7. Tests

| File | Facts |
|---|---|
| `Auth/LoginTests.cs` | right → cookie flags (HttpOnly, Secure, SameSite=Strict); wrong user and wrong password give the same body; lockout after 5 and its reset; rate limit |
| `Auth/ScopeTests.cs` | the report token cannot read runs; the profile token can only export; the cookie cannot post events; a non-GET without `X-Orchestra` → 400 |
| `Auth/LocalModeTests.cs` | Local mode: no sign-in, import on — unchanged from plan 0001 |
| `Sync/SyncTests.cs` | the same blocks twice → no new rows, same runs; a corrupt block → one import error; tasks replaced |
| `Backup/BackupServiceTests.cs` | `VACUUM INTO` produces a readable copy; only 14 kept |
| engine offline | `sync`/`profile pull` with an unreachable URL return 0 and warn once; `pull` replaces only with a newer version |

## 8. Stop and ask — open questions for the owner

**Deferred 2026-11-20:** the owner runs locally in Docker until the bugs are out; all three questions wait until this plan is picked up.

1. **What may leave the Windows machine?**
   - (a) **Recommended:** numbers and metadata only: verdicts, counts, models, usage, task line, plan
     number and title. The library shows titles and statuses, but **not** plan text, previews or
     screenshots.
   - (b) Everything, including plan, preview and evidence files (a `library sync` too).
   - (c) Like (a), plus the latest line.
2. **Which reverse proxy** runs on the server (Caddy, nginx, Traefik…)? The compose file assumes the
   proxy already exists and only publishes `127.0.0.1:3500`.
3. **Backups off the server** — is a local copy in `/data/backups` enough, or should a copy also go
   somewhere else (which)?

## 9. Phases

- **A — sign-in and server mode.** Files 1–4, 11 (auth), 12. **Gate** (plan 0001 §9). Stop.
- **B — sync and profile pull.** Files 5–8, 11 (sync). **Gate** + the offline suite. Stop.
- **C — backups and deploy.** Files 9–10, 11 (backup). **Gate** + `docker compose -f
  deploy/docker-compose.server.yml build`. **Owner checks:** the items of «نتیجه» on the real server. Stop.

## 10. Definition of done

All gates green; the server reachable over HTTPS with sign-in; both projects syncing; a restore tested
once; session log written; this header `Status: Done` with the date, commit and merge.
