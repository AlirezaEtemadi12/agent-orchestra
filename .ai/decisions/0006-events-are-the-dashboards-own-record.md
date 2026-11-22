# 0006 — Events are the dashboard's own record

**Status:** Accepted
**Date:** 2026-11-20
**Decided by:** Alireza, 2026-11-19 (the live-detail decision); written down by plan 0004.

## Context

[ADR 0005](0005-ai-is-knowledge-orchestra-is-work.md) makes the database a read model: files are the
truth, and the database can be deleted and rebuilt at any time. That holds for everything the importer
reads — the ledger, an open run's `task.json`, the close archive.

From phase 1 the orchestrator also *reports*: each stage, each review round with its blocker counts,
each pool wait, the latest line of a hop, each ledger write. Some of that exists nowhere else. The
ledger keeps only the final review round; the archive keeps the texts but neither the counts of an
earlier round nor the order things happened in. A run that is still going has nothing but `task.json`,
which is rewritten as it proceeds.

If events were derived data the dashboard would have to throw them away on every import — and with them
the per-round detail that is the reason for the channel.

## Decision

Events the runner reports are the dashboard's **own record**. `RunEvent` rows are not derived from any
file, are never deleted by an import, and an import only *reads* them (plan 0004 D3).

Consequences that follow from it:

1. Losing the database loses per-round detail and the live history — never a run: the ledger still has
   every run, and the files still rebuild everything else.
2. Backups matter from now on (plan 0008).
3. The "drop the database and re-import" advice in
   [`../architecture/data-model.md`](../architecture/data-model.md) gets this one exception, and the doc
   says so where it gives that advice.
4. Events carry a retention rule of their own (progress events, seven days) instead of living as long as
   the file they came from.

## Alternatives

- **Re-derive events from `task.json` and the archive.** Rejected: the counts of an earlier round and
  the pool waits are not in either file, and a run in flight would still need a live channel.
- **Keep events in a file beside the database.** Rejected: they are queried per project and per run and
  ordered by time; that is a table, and a file would need its own index and its own corruption story.
