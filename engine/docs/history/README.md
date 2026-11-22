# History — how the orchestrator got here

These files are **history, not rules**. They were written while the engine lived in
`Project ProjectB/orchestrator` and are kept unchanged: the design notes it was built from
(`DESIGN.md`), the plans that were finished (`*-PLAN.md`), and the porting brief
(`PROJECTA-PORT-PROMPT.md`).

Read them to understand *why* something is the way it is. What to do is in `engine/README.md`,
`engine/docs/CONDUCTOR.md`, and [`.ai/engine/conventions.md`](../../../.ai/engine/conventions.md).
Decisions that still bind are recorded as ADRs in [`.ai/decisions/`](../../../.ai/decisions/README.md).

`SUITE-SPEED-PLAN.md` is here for a second reason: it was an untracked working note in the
Project B orchestrator at the move (plan 0005 §8 question 1), and the owner asked for it to travel
with the history rather than be committed there.

`ARCHIVE-ON-CLOSE-PLAN.md` came from the Project A orchestrator in phase C (it was only there); it
describes the archive written on close, which both projects now share through the engine.
