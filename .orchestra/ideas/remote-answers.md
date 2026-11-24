# Answer an agent's question remotely

**Status:** raw · **Added:** 2026-11-24 · **Plan:** —

When a run stops because an agent hit a problem or has a question, the owner answers it from the
dashboard (or from the phone, see [mobile view](mobile-view.md)) instead of at the machine.

Two different things, two different routes:

1. **The conductor's chat session.** The conductor is a Claude session; it can already be continued from the
   phone with Claude's own Remote Control. No Orchestra code is needed for this.
2. **A run's hop that waits for a person.** The engine already has the route: the `inbox` transport writes the
   prompt to `runs/<id>/inbox/<key>.md`, stops with exit 3, and picks up `runs/<id>/answers/<key>.md` on the
   next `run.ps1` command. A dashboard that shows the inbox file and writes the answer file uses that same
   contract, so:
   - the engine still never calls or waits on the dashboard
     ([ADR 0002](../../.ai/decisions/0002-runner-reports-dashboard-never-commands.md)): the answer can still be
     written by hand or by another agent;
   - the dashboard writes into a project folder, which needs its own ADR beside ADR 0009 (setup is the
     only write today).

What stays out: **approving a plan or a gate is never an answer.** Gate 2 approval stays with the owner and the
conductor ([ADR 0003](../../.ai/decisions/0003-read-only-dashboard-first.md)). An inbox hop is only allowed
in `plan-reviewer`, `readback` and `verifier` today; that list does not grow through this idea.

The trigger is the same event as [notifications](notifications.md) (a run that waits for the owner). Related:
[low-risk actions](low-risk-actions.md).
