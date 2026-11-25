# Design

> **AUTHORITATIVE** for the look of `ui/`. Tokens live in `src/styles/tokens.css`, mapped to Tailwind
> in `globals.css`.

## Tokens

- Every colour is a CSS variable used through a Tailwind colour (`bg-card`, `text-muted-foreground`,
  `border-border`). No palette class (`bg-slate-800`) and no hex value in a component.
- Brand: indigo primary, a dark sidebar in both themes, and semantic `success`, `warning`, `info`,
  `destructive`, plus `chart-1…5` for bars.
- One radius scale from `--radius`; `rounded-lg`/`rounded-xl` are the normal choices.

## Themes

- Light, dark and system (the default), through `next-themes` with `attribute="class"`.
- Both themes are checked for every screen; a colour that only works in one theme is a bug.

## Components

- shadcn components are copied into `components/ui/` and edited there, not wrapped twice.
- Shared app pieces in `components/app/`: `PageHeader`, `KpiCard`, `StatusBadge`, `VerdictBadge`,
  `BarList`, `DayColumns`, `ProjectChips`, `MoneyText`, `TokensText`, `DurationText`, `EmptyState`,
  `ErrorState`, `TableSkeleton`, `Markdown`.
- `Markdown` is the only markdown renderer (`react-markdown` + `remark-gfm`, no raw HTML). Library
  documents, evidence reports and the run detail's stage texts all go through it — never a second
  renderer, never `dangerouslySetInnerHTML`.

## The library screens

- One tab bar (`?tab=plans|previews|ideas|research|evidence`), a project chip row, a search box, and
  sortable column headers; the status filter appears only for plans and ideas, because no other
  category has a status.
- Plans, ideas, research and previews are compact tables; evidence is a gallery of cards — images as
  lazy `<img>` from the file endpoint, a `report.md` as a card with an icon and its group. A card
  opens a dialog: an image at full size, a report as markdown fetched from the file endpoint. The
  evidence gallery uses the shared pager under it when there are more than 100 rows.
- The item page holds the header fields, the plan prompt (button, hint as a tooltip, the text itself
  below so a failed copy can be selected), the runs, the plan's evidence and the document body.
- **The preview frame** is `<iframe sandbox="allow-scripts">` at 70vh with a short note and an "open
  in a new tab" link. It never carries `allow-same-origin`, and the served HTML's CSP allows scripts
  but no network (`connect-src 'none'`, `form-action 'none'`): the sandbox isolates the DOM, not the
  network, so the policy has to.
- Every list has three states: loading (skeleton), empty (a sentence, not a blank box) and error
  (a message plus retry).
- Latin values (task ids, models, branches, session ids, money) render with `data-ltr` and
  `tabular-nums`, so they line up in both directions.

## The project screens

- The form's **Browse** button asks the desktop host for the Windows folder dialog through
  `lib/desktop/bridge.ts` when `window.external.sendMessage` exists; in a plain browser it opens the
  in-app folder browser, which walks `api/system/folders`. One button, two ways to pick a folder.
- The setup screen lists one row per file or job with its action chip, the path in mono and the detail
  under it; its primary button stays off while the engine is missing or nothing is left to do, and the
  reason sits beside it.

## Density

- The dashboard is read, not filled: compact tables, one accent per screen, numbers right-aligned in
  their own direction.
