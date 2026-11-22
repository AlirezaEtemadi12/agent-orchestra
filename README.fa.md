<div dir="rtl" align="right">

# agent-orchestra

[English](README.md) | **فارسی**

یک ارکستراتور چندایجنتی با گیت‌های کنترل برای کار نرم‌افزاری، به‌همراه یک داشبورد که نشان می‌دهد چه کرده است.

هیچ کاری مستقیم از یک پرامپت به کد نمی‌رسد. هر کار از چهار مرحله می‌گذرد:

</div>

```
plan → independent review → read-back → approval → implementation (cheap model) → verify
```

<div dir="rtl" align="right">

- **برنامه‌ریز** پلنی می‌نویسد که همه‌چیز را تصمیم می‌گیرد؛ پیاده‌ساز هیچ تصمیمی نمی‌گیرد.
- پلن را یک مرورکننده از **خانوادهٔ مدلیِ دیگر** مرور می‌کند.
- در **read-back** پیاده‌ساز، پیش از دست زدن به هر فایل، پلن را با زبان خودش بازگو می‌کند.
- **شما** صریحاً تأیید می‌کنید و نتیجه بدون commit تحویل داده می‌شود.

## چه چیزهایی در این مخزن است

| پوشه | توضیح |
|---|---|
| `engine/` | خود ارکستراتور (PowerShell 5.1): زنجیرهٔ مدل‌ها برای هر نقش، poolها و سهمیه، و یک مجموعه تست offline روی یک ایجنت ساختگی. پروژه‌ها آن را با یک shim ده‌خطی صدا می‌زنند. |
| `api/` | .NET 10 و SQLite. runها و ledgerها را import می‌کند، رویدادهای زنده را می‌گیرد و UI را سرو می‌کند. |
| `ui/` | خروجی static از Next.js 16 (shadcn/ui)، فارسی و انگلیسی، روشن و تیره. |
| `deploy/` | Dockerfile و `docker-compose.yml`. |
| `.ai/` | دانش برای هر ایجنت: معماری، تصمیم‌ها (ADR) و قراردادها. |
| `.orchestra/` | کارِ ارکستراشده: workflow، پلن‌ها، پیش‌نمایش‌ها و ایده‌ها. |

داشبورد این‌ها را نشان می‌دهد: runها به تفکیک پروژه، هزینه به تفکیک روز و ماه و مدل و پروژه، کیفیت مدل‌ها
(تعداد دورِ رسیدن به PASS، ارزش read-back، نرخ FAIL در verify، نرخ cache hit)، پلن‌ها و یک برد زنده.
داشبورد فقط می‌خواند: چیزی را تأیید یا پیاده نمی‌کند و در پوشهٔ پروژه نمی‌نویسد. اگر داشبورد خاموش باشد،
run دقیقاً مثل قبل کار می‌کند.

<!-- Screenshots: add them under docs/images/ and link them here. -->

## اجرای داشبورد

.NET 10 و Node 22 لازم دارید، یا Docker.

فایل `api/src/Orchestra.Api/appsettings.Local.example.json` را به `appsettings.Local.json` کپی کنید
(در git نادیده گرفته می‌شود) و پوشهٔ runهای پروژه‌هایتان را در آن بنویسید.

</div>

```
cd ui && npm ci && npm run build && cd ..
dotnet run --project api/src/Orchestra.Api
```

<div dir="rtl" align="right">

یا با Docker. اول `deploy/.env.example` را به `deploy/.env` کپی و پر کنید:

</div>

```
docker compose -f deploy/docker-compose.yml up --build
```

<div dir="rtl" align="right">

سپس `http://localhost:3500` را باز کنید. پوشه‌های پروژه‌ها فقط‌خواندنی mount می‌شوند.

## استفاده از موتور

هر پروژه یک `run.ps1` کوچک (shim) دارد و یک فایل `orchestrator.local.json` که در git نیست و در آن
`enginePath` به پوشهٔ `engine/` همین مخزن اشاره می‌کند. دستورها، نقش‌ها و تنظیمات در
[`engine/README.md`](engine/README.md) آمده است. اجرای تست‌های offline:

</div>

```
powershell -NoProfile -File engine/tests/offline/run-offline.ps1
```

<div dir="rtl" align="right">

## وضعیت

فاز ۰ (import، کتابخانه، پروژهٔ جدید)، فاز ۱ (رویداد زنده) و انتقال موتور تمام شده است. پروفایل‌ها با
تنظیم اختصاصی هر پروژه مرحلهٔ بعدی است. فازها در [`.ai/vision/roadmap.md`](.ai/vision/roadmap.md) و وضعیت
پلن‌ها در [`.orchestra/plans/`](.orchestra/plans/README.md) است. ایجنت‌ها از [`AGENTS.md`](AGENTS.md) شروع
می‌کنند.

این یک ابزار شخصی است که هر وقت زمان باشد ساخته می‌شود. issue خوش‌آمد است، ولی تعهدی برای پشتیبانی نیست.

## مجوز

[MIT](LICENSE)

</div>
