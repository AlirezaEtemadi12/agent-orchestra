# UI testing

> **AUTHORITATIVE** for `ui/src/**/*.test.tsx?`. Trimmed from Project B's `Ui/.ai/coding/testing.md`.

- Vitest + Testing Library, `globals: false`, jsdom, explicit `cleanup` in `vitest.setup.ts`, and
  stand-ins for `matchMedia` and `ResizeObserver`.
- Mock at the feature's API object (`vi.mock` of `<name>.api`), never at `fetch` and never at the
  query client.
- Wrap rendered components in `NextIntlClientProvider` with the locale's messages; use the real
  catalogue, so a missing key fails the test.
- Test what the user sees: a heading, a table row, an empty state, an error with retry. Do not test
  class names or component internals.
- The architecture test (`src/architecture.test.ts`) is part of the gate, not a nicety: it enforces
  the one-way imports, the single `fetch`, the single formatter and the token-only colours.
- No snapshots of whole pages. A snapshot that changes on every tweak teaches nothing.
