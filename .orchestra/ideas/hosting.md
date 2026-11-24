# Hosting on a server

**Status:** raw · **Added:** 2026-11-24 · **Plan:** — (was [plan 0008](../plans/0008-hosting.md), dropped by plan 0012)

One person, several machines, one dashboard that shows them all: the runner on each machine reports to a
server, with sign-in, HTTPS and backups. Plan 0008 had this as phase 3. The owner dropped it on
2026-11-24 ("I don't want a server; simple for me first"), and plan 0012 makes Orchestra a local app and
deletes `deploy/`.

If it comes back:
- the Docker files are in git history before plan 0012;
- ADR 0009 assumes loopback (setup writes into projects), so a server needs a new ADR that keeps setup on
  the machine that owns the folders;
- a tunnel to the local app ([mobile view](mobile-view.md)) may cover the need without a server.
