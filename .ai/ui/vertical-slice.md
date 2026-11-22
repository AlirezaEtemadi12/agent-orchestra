# Vertical slices

> **AUTHORITATIVE** for how `ui/src/features/` is organised. Trimmed from Project B's
> `Ui/.ai/architecture/vertical-slice.md`, without permissions and guarded routes.

```
features/<name>/
  index.ts            the only public entry: the feature object, its messages
  <name>.manifest.ts  key, route, slices
  <name>.api.ts       calls apiClient, returns the DTOs
  <name>.queries.ts   query keys and useQuery wrappers
  <name>.types.ts     DTO types, camelCase
  i18n/{fa,en}.ts     the feature's own catalogue
  components/         the feature's components and their tests
```

Rules:

- Outside a feature, import it **only** through its `index.ts`. A feature never imports another
  feature; shared code goes to `lib/` or `components/app/`.
- `lib/` and `components/ui|app/` never import a feature — the dependency points one way.
- Every feature is listed once in `features/registry.ts`; the menu is built from that list.
- A feature owns its strings, its queries and its components together, so deleting the folder
  removes the feature.
