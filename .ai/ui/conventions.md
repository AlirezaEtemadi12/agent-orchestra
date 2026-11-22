# UI conventions

> **AUTHORITATIVE** for `ui/`. Trimmed from Project B's `Ui/.ai/coding/conventions.md`, minus axios,
> forms and data grids (this app only reads).

- Next.js App Router, static export (`output: 'export'`), served by the API. No server routes, no
  rewrites, no middleware.
- `apiClient` (over `fetch`) is the only place a request is made; every other file calls a feature's
  API object.
- TanStack Query owns server state. A component never caches data by hand.
- Components come from the shadcn registry (`radix-ui`, `cn`); `components/ui/` is generated and
  `components/app/` holds the shared app pieces. Colours come from tokens only — a hex value in a
  `.tsx` file fails the architecture test.
- Logical direction classes (`ps-`, `pe-`, `ms-`, `me-`, `start-`, `end-`) everywhere; a physical
  `left`/`right` in a component is a bug. Latin values (ids, models, costs) carry `data-ltr`.
- Everything user-visible comes from the message catalogue. A literal string in a component is a bug;
  the one exception is the static 404 page, which has no locale.
- `prettier` with `prettier-plugin-tailwindcss` formats; `eslint` and `tsc --noEmit` must be clean.
