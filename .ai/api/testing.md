# API testing

> **AUTHORITATIVE** for `api/tests/`. Trimmed from Project B's `Web/.ai/coding/testing.md`.

- xunit + FluentAssertions **7.0.0** (8.x is under a commercial licence). No mocking library.
- No test starts a server or opens a socket: controllers are not tested over HTTP, handlers and
  parsers are tested directly.
- SQLite **in-memory** (`DataSource=:memory:` with the connection kept open) is the real provider and
  is allowed. Nothing else is faked that could be real.
- Fixtures are synthetic: invented task ids (`T-DEMO-*`), invented text. A real ledger or `task.json`
  never enters the repository, not even "to check".
- A test name states the guarantee; the body asserts it. Hand-computed ledger totals go in a comment
  at the top of the test that uses them.
- The test count never drops between phases (`.ai/ci.md`).
