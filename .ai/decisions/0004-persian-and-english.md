# 0004 — Persian and English from day one

**Status:** Accepted
**Date:** 2026-11-19
**Decided by:** Alireza («پروژه چند زبانه و از همین الان فارسی و انگلیسی رو پوشش بده»; default
language and date format chosen in the plan 0001 preview)

## Context

Project B's Ui is Persian only (its ADR 0004). Orchestra will be a **public** repository others can
use, and its owner works in Persian. The UI is a static export ([ADR 0001](0001-one-repo-dotnet-nextjs-sqlite.md)),
so there is no server to read a cookie or an `Accept-Language` header at page load.

## Decision

- Two locales, **`fa` (RTL) and `en` (LTR)**, through next-intl with **locale routing**:
  `/fa/…` and `/en/…`, pre-rendered for both with `generateStaticParams`. `/` is a static page that
  picks the stored choice, else the browser's language if it is `fa` or `en`, else `en`.
- The header has a language switch; the choice is kept in `localStorage`.
- `dir` and `lang` follow the locale. Only logical direction utilities, as in ProjectB.
- Every feature has `i18n/fa.ts` and `i18n/en.ts`; a test fails when their keys differ or a message is
  empty.
- Dates: `fa` uses the Persian (Jalali) calendar with **Latin digits**; `en` uses Gregorian. The time
  zone is the **browser's**.
- The API localizes its own error text from the `Accept-Language` the client sends (`fa` or `en`).
- Persian text is never machine-translated, reflowed or retyped; English text is written in `en.ts`,
  not derived from the Persian.

## Consequences

- Every screen is built and checked twice (two languages × two themes). The key-parity test keeps the
  catalogues from drifting.
- Static export rules out next-intl's proxy (middleware) redirect; the `/` page does it in the browser.
- A third language is additive: one more catalogue per feature and one more static param.
