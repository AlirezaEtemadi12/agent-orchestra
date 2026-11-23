// Copied from plan 0009 §6.8. Never retype or "improve" these strings.
export const startFa = {
  "title": "شروع کار",
  "done": "انجام شد",
  "todo": "مانده",
  "s1": {
    "title": "داشبورد را اجرا کنید",
    "docker": "داکر: در deploy/.env خط ORCHESTRA_HOST_ROOT=<پوشه‌ای که پروژه‌هایتان در آن است> را بگذارید و اجرا کنید: docker compose -f deploy/docker-compose.yml up -d",
    "iis": "IIS: بسته‌ی ASP.NET Core 10 Hosting Bundle را نصب کنید، به Application Pool اجازه‌ی خواندن پوشه‌ی پروژه‌ها را بدهید، و سایت را فقط روی localhost باز کنید."
  },
  "s2": {
    "title": "پروژه اضافه کنید",
    "body": "مسیر پوشه را همان‌طور که در ویندوز می‌بینید بدهید؛ بقیه خودش پر می‌شود.",
    "action": "پروژه‌ی تازه"
  },
  "s3": {
    "title": "وصلش کنید",
    "body": "دو تکه‌ی صفحه‌ی «اتصال» پروژه را paste کنید؛ با اولین رویداد این قدم سبز می‌شود."
  },
  "s4": {
    "title": "یک پلن اجرا کنید",
    "body": "Library ← یک پلن را باز کنید ← Copy prompt ← آن را به ایجنتی بدهید که در پوشه‌ی پروژه باز است."
  },
  "s5": {
    "title": "بررسی و بستن",
    "body": "diff را ببینید و خودتان commit کنید، Status پلن را Done کنید، بعد اجرا کنید: run.ps1 close <task id>"
  }
};
