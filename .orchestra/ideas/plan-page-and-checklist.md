# A page per plan, with its checklist

**Status:** raw · **Added:** 2026-11-25 · **Plan:** —

Since plan 0013 a plan is a folder: `plan.md`, `preview.html` and `evidence/` sit together. The Library still
shows them in separate tabs (Plans, Previews, Evidence), so a plan's pieces are three clicks apart. Instead,
clicking a plan in the Library would open one page for it:

- the header: number, title, Status, repos, dates, the run with its state on the board;
- the preview, opened from the page;
- the evidence of that plan only, as a gallery;
- **the owner's checklist**: the plan's «چک‌لیست شما» section, read as Markdown checkboxes (`- [ ]` /
  `- [x]`), shown with a count such as "3 of 6".

**The page only reads.** A tick is written into `plan.md` by an agent when the owner says so ("item 3 passed"),
never by the dashboard. So the plan's data stays in the project, and the dashboard still writes only through
setup ([ADR 0003](../../.ai/decisions/0003-read-only-dashboard-first.md)). For that to work, the plan template
writes the checklist as checkboxes.

Open question: once this page exists, do the Previews and Evidence tabs stay as cross-plan lists, or go?
