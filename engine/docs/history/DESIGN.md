<!--
Historical document, ported unchanged from the workspace this orchestrator was built in. Its Persian
environment findings (claude relay hang, codex missing) describe that machine at that time
and are NOT current configuration. For what the tool does now read REVIEW-BRIEF.md; for the working
model and its reasons read WORKING-MODEL.md; for the procedure read CONDUCTOR.md.
-->

# Orchestrator — طراحی

> سند طراحیِ «ارکستریتور چند-ایجنتِ» پروژه‌ی DrivingScore.
> هدف: یک ایجنت نقش orchestrator بگیرد و بتواند بقیه‌ی ایجنت‌ها را با خط فرمان صدا بزند،
> خروجی‌شان را بگیرد و بین‌شان پیام رد و بدل کند — دقیقاً روی همان چهار گیتِ
> [`Skills/plan-workflow`](../Skills/plan-workflow/SKILL.md).

وضعیت: **Design / Draft**. تست اثبات‌مفهوم انجام شده (بخش ۸). هنوز کدی ساخته نشده.

---

## ۱. هدف و دامنه

یک تسک از کاربر می‌آید و باید از چهار مرحله بگذرد:

```
plan → plan-review → (revise) → ⏸ APPROVAL → implement (per phase) → ⏸ per-phase → verify → close
```

هر مرحله توسط یک **نقش** (role) و روی یک **مدل** اجرا می‌شود. ارکستریتور:
- نقش‌ها را به آداپتورهای اجرایی (CLI/headless) نگاشت می‌کند،
- تحویل‌دهیِ خروجی‌ها (file-bus) را انجام می‌دهد،
- قواعد چهار گیت را **الزام** می‌کند (نه اینکه به ایجنت اعتماد کند)،
- در نقاط تأیید (gate 2 / per-phase) متوقف می‌شود و از کاربر می‌پرسد.

خارج از دامنه (در این نسخه): اجرای واقعی کامیت/مرج (طبق Gate 4 ممنوع است) و خودکارسازی کاملِ
اپ‌های دسکتاپ مثل WorkBuddy.

---

## ۲. یافته‌های محیطی (واقعیت ابزارها روی این ماشین)

| ابزار | نوع | headless؟ | وضعیت auth | یادداشت |
|---|---|---|---|---|
| `opencode` (CLI) | CLI رسمی | ✅ `opencode run -m <model> "..."` | **لازم نیست** (مدل رایگان) | `v1.19.3`، نصب‌شده؛ بهترین موتور |
| `claude` | CLI | ✅ `claude -p` | OAuth ثبت شده ولی هنگام اجرا **هنگ** می‌کند | نیاز به بازبینی login |
| `codex` | CLI | ✅ `codex exec` | ❌ `401 Unauthorized` | کاربر ندارد؛ کنار گذاشته شد |
| `freebuff` | CLI | ❌ فقط TUI | — | عمداً headless ندارد (issue #947) |
| `freebuff` Desktop | Electron | ❌ | — | هیچ CLI/API عمومی ندارد |
| `WorkBuddy AI` | Electron | ❌ | — | بسته؛ فقط MCP/guest-SDK داخلی |
| `OpenCode` Desktop | Electron | ❌ (خودِ اپ) | — | موتور مشترک با CLI |

**نتیجه:** موتور خودکار = `opencode` CLI با **مدل‌های رایگان**. مدل‌های موجود:

```
opencode/nemotron-3-ultra-free      opencode/glm-5.3-flash-free
opencode/deepseek-v4.1-flash-free   opencode/big-pickle
opencode/mimo-v2.6-flash-free       opencode/longcat-2.5-preview-free
opencode/beam-free                  opencode/nemotron-3.5-*-free
opencode/space-bunny-free
```

نکته‌ی کلیدی: **DeepSeek و GLM هر دو به‌صورت رایگان و بدون auth** در دسترس‌اند.

---

## ۳. معماری — دو لایه

### لایه‌ی ۱: آداپتورها (اجرا)
هر ایجنت یک adapter با اینترفیس مشترک:
```
adapter.run({ prompt, cwd, model, systemPrompt, readOnly, timeout }) -> {
    stdout, outboxFiles, exitCode, sentinels[]
}
```
انواع adapter (به‌ترتیب اولویت):
- `opencode` — `opencode run -m <model> <prompt>` (فعال)
- `claude` — `claude -p --model <m> --output-format json` (وقتی auth درست شد)
- `manual` — برای ابزاری که حالت headless ندارد (اپ دسکتاپ، TUI تعاملی). ارکستریتور منتظر نمی‌ماند:
  prompt را در `runs/<id>/inbox/<key>.md` + `.fingerprint` می‌نویسد و همان لحظه با `-2` برمی‌گردد، پس
  دستور با `exit 3` (`DEFERRED`) تمام می‌شود و `task.json` دست‌نخورده می‌ماند. `run.ps1 answer <taskId>
  <key> <file>` پاسخ را در `answers/<key>.md` می‌گذارد و اجرای دوبارهٔ همان دستور آن را مصرف می‌کند.
  کلید می‌گوید پاسخ برای چه معتبر است (`plan-reviewer.v<round>`، `verifier.<hash گیت‌ها+diff>`)؛ پاسخ
  کلیدِ قدیمی نادیده گرفته می‌شود و اگر ریپو بعد از نوشتن prompt تغییر کرده باشد `-3` (violation)
  برمی‌گردد. فقط در `plan-reviewer` و `verifier` مجاز است؛ هرگز در `planner`/`implementer` (کانفیگ
  بارگذاری نمی‌شود).

### لایه‌ی ۲: جریان (state machine)
دقیقاً چهار گیت. وضعیت در `runs/<task>/task.json`.

---

## ۴. کانفیگ — `orchestrator.json`

```jsonc
{
  "repos": {
    "Web":    "Web",
    "Mobile": "Mobile",
    "Ui":     "Ui"
  },
  "roles": {
    "planner":        { "chain": [ { "adapter": "opencode", "model": "opencode/nemotron-3-ultra-free" } ] },
    "plan-reviewer":  { "chain": [ { "adapter": "opencode", "model": "opencode/glm-5.3-flash-free" } ] },
    "implementer":    { "chain": [ { "adapter": "opencode", "model": "opencode/deepseek-v4.1-flash-free" } ] },
    "verifier":       { "chain": [ { "adapter": "opencode", "model": "opencode/glm-5.3-flash-free" } ] }
  },
  "rules": {
    "reviewerModelMustDifferFromPlanner": true,
    "neverCommit": true,
    "gateCommandsFromRepoDoc": true
  },
  "preflight": { "enabled": true, "pingPrompt": "Reply with exactly: PONG" }
}
```

هر نقش یک **زنجیره‌ی fallback** دارد: اگر آداپتور اول لیمیت خورد/شکست خورد، بعدی اجرا می‌شود.

---

## ۵. قرارداد پیام (file-bus) و sentinelها

- خروجی هر فراخوانی در `outbox/<role>/<step>.md` ذخیره و در prompt مرحله‌ی بعد تزریق می‌شود.
- پروتکل sentinel در متن خروجی (همان «Stop and ask» دستیِ پروژه را ماشینی می‌کند):
  - `@@NEED_INPUT: <سؤال>` → ارکستر pause می‌کند و از کاربر می‌پرسد.
  - `@@BLOCKED: <دلیل>` → فاز متوقف، پلن برمی‌گردد به planner.
  - `@@PASS` / `@@DONE` → عبور به مرحله‌ی بعد.
- آدرس‌دهی پیام: `inbox/<role>/<seq>.json` با شکل
  `{ "task", "from", "action", "input", "replyTo" }`.

---

## ۶. نگاشت به چهار گیت (اجبار در ارکستر)

| گیت | قاعده | نحوه‌ی اجرا در ارکستر |
|---|---|---|
| 1 — Plan | قبل از هر ویرایش پلن | نقش `planner` پلن می‌نویسد؛ وضعیت `Planned` |
| 2 — Approval | پلن با **مدل متفاوت** مرور، سپس تأیید کاربر | ارکستر قبل از فراخوانی چک می‌کند `planner.model != reviewer.model`؛ سپس `@@NEED_INPUT` → تأیید کاربر |
| 3 — Branch | برنچ قبل از ویرایش | `implementer` با system-prompt «برنچ اول»؛ ارکستر با `git status` صحت برنچ را می‌سنجد |
| 4 — Hand over | uncommitted | آداپتورها با ابزارهای پرخطر غیرفعال؛ ارکستر هرگز commit/merge/delete نمی‌زند؛ سه سؤال بستن را به کاربر می‌دهد |

Read-only برای `planner` / `plan-reviewer` / `verifier` با فلگ‌های مخصوص هر CLI.

---

## ۷. ساختار پوشه (ریشه؛ ریشه git نیست پس با گیت‌های ریپو تضاد ندارد)

```
Project DrivingScore/
  orchestrator/
    DESIGN.md              # همین سند
    orchestrator.json      # نقش ← adapter + fallback
    run.ps1                # CLI: new | status | approve | review | close
    lib/
      agents.ps1           # adapterها
      bus.ps1              # file-bus + sentinel parser
      gates.ps1            # gate commands از .ai/coding/ci.md هر ریپو
    runs/
      <task-id>/
        task.json          # state machine
        inbox/<role>/*.json
        outbox/<role>/*.md
        artifacts/         # plan.v1.md, plan.findings.md, plan.final.md, diff, verify.md
        transcript/        # خام stdout/stderr هر گام
```

---

## ۸. Preflight / health-check

قبل از شروع هر پایپ‌لاین، ارکستر برای هر نقشِ فعال یک ping یک‌توکنی می‌زند.
اگر آداپتور سالم نبود، پایپ‌لاین **شروع نمی‌شود** و دقیقاً می‌گوید کدام ایجنت چه کم دارد
(مثال واقعی: `codex` → 401؛ `claude` → هنگ/لاگین).

---

## ۹. صف سهمیه (quota pools)

مسئله: چند `run.ps1` هم‌زمان به یک حساب می‌خورند و ۴۲۹ می‌گیرند، چون هیچ‌کدام از دیگری خبر ندارد.

- **سهمیه یعنی حساب، نه مدل.** هر صف با `pool` نام‌گذاری می‌شود: `pool` همان hop، وگرنه `pool` آداپتور، وگرنه
  نام آداپتور. پس همه‌ی مدل‌های رایگان opencode یک صف‌اند و یک مدل فقط وقتی صف جدا می‌گیرد که در تنظیمات گفته شود.
- **جا (slot) یک قفل فایلِ سیستم‌عامل است**: `runs/_pool/<pool>/slot-<n>.lock`. پس بین پردازش‌ها کار می‌کند و با
  مرگ صاحبش خودبه‌خود آزاد می‌شود؛ نه پاک‌سازی قفل کهنه لازم است نه بررسی PID. نام صاحب جا در `slot-<n>.json`
  نوشته می‌شود و فقط تا وقتی معتبر است که قفلش باز نشود.
- **سقف‌ها** با ترتیب hop ← آداپتور ← `tiers[<tier>]` ← مقدار درونی حل می‌شوند: `maxConcurrent` (۰ = بی‌سقف)،
  `minGapSec`، `rateLimitRetries`، `rateLimitBackoffSec`، `whenBusy` (`wait`|`next`) و `queueWaitSec`. مقدارهای
  درونی همان رفتار پیش از این تغییرند، پس کانفیگی که `tiers` ندارد همان‌طور کار می‌کند که کار می‌کرد.
- **`free` و `paid`** دو پیش‌فرض‌اند. هر دو برای هر عامل یک جا دارند؛ تفاوتشان در نحوه‌ی درخواست است: رایگان
  ۱۵ ثانیه فاصله و دو تلاش پس از ۴۲۹، پولی بدون فاصله و یک تلاش کوتاه (سقف مصرف اشتراک با چند دقیقه صبر
  باز نمی‌شود). `claude` پولی است، ولی چون خودش هم ۴۲۹ داد یک جا دارد.
- **hop های inbox** (پاسخ انسانی) هیچ جا نمی‌گیرند و منتظر نمی‌مانند.
- **۴۲۹**: خواب فزاینده روی همان hop (`backoff × 2^(n-1)`) و ثبت «زمان خنک شدن» برای صف، تا بقیه‌ی اجراها تا آن
  زمان به همان سهمیه نکوبند؛ بعد از تلاش‌های مجاز، زنجیره سراغ hop بعدی می‌رود.
- **انتظار**: hop منتظر هر ۵ ثانیه نگاه می‌کند، یک‌بار «منتظر صف … در اختیار …» می‌نویسد (و اگر صاحب جا عوض شود
  دوباره)، و در `progress.json` مقدار `phase: queued` را با `pool` و `heldBy` می‌گذارد. با `whenBusy: next`
  هیچ‌وقت منتظر نمی‌ماند و دلیل رد شدنش در گزارش اجرا ثبت می‌شود. `health` هم هیچ‌وقت منتظر نمی‌ماند.
- **`run.ps1 pools`** همه‌ی صف‌ها را نشان می‌دهد: tier، چند جا از چند، چه کسی جا را گرفته، و تا کی خنک می‌شود.
- همه‌ی این منطق در `lib/pool.ps1` است تا انتقالش به ارکستر دیگر فقط کپی فایل باشد. `ORCH_POOL_POLL_MS` و
  `ORCH_POOL_BACKOFF_SCALE` فقط برای تست آفلاین‌اند و در تولید تنظیم نمی‌شوند.

---

## ۱۰. شاهد تست اثبات‌مفهوم (انجام‌شده)

اسکریپت موقت `orch-test.ps1` دو نقش را با دو مدل متفاوت زنجیر کرد:

- planner → `opencode/nemotron-3-ultra-free` → `outbox/planner/plan.md`
- reviewer → `opencode/glm-5.3-flash-free` → `outbox/reviewer/review.md`

خروجی: reviewer واقعاً عیب‌های جدی گرفت (تناقض Unicode، عدم تصمیم درباره‌ی
transliteration، runtime نامشخص). یعنی زنجیره، «مدل متفاوت» و file-bus کار می‌کنند —
بدون هیچ authی.

---

## ۱۱. سؤالات باز / نقشه‌ی راه

1. **claude — علت پیدا شد**: `-p` هنگ نمی‌کند؛ envهای `ANTHROPIC_BASE_URL=https://raszzjk.abc-tunnel.us/v1` و `ANTHROPIC_AUTH_TOKEN` (بازمانده‌ی free-claude-code) به یک تونلِ مرده (`...trycloudflare.com` → Cloudflare 1016) اشاره می‌کنند و کلاینت ~۱۱ بار retry می‌کند (~۹ دقیقه) و بعد خطا می‌دهد. راه‌حل: حذف آن سه env تا کلاد به OAuth خودِ کاربر برگردد. آداپتور claude در `orchestrator.json` غیرفعال است.
2. **codex**: کاربر ندارد (`401 Unauthorized`).
3. **WorkBuddy**: پل دستی در فاز اول؛ خودکارسازی از طریق guest-SDK/MCP داخلی = پروژه‌ی جداگانه.
4. **freebuff**: کنار گذاشته شد؛ اگر PR #1477 یا پذیرش freebuff-token در `@codebuff/sdk` بیاید، دوباره بررسی می‌شود.
5. تصمیم دربارهٔ محل final (ریشه‌ی پروژه در برابر `Skills/`).
