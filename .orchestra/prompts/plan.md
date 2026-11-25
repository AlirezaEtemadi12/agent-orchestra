Read the owner's request, then the docs this repo's `AGENTS.md` points at: its index, the owning doc
for every area the work touches, and the newest session log. Read the code, not only the docs.

Write two things, in this order:

1. **The understanding** — in your own words, never a copy of the request. Name every sentence that
   could mean two things and the reading you chose, and what the owner may have meant that this work
   will not do.
2. **The plan** — from `plans/TEMPLATE.md`, in the folder `orchestrator.json` names.

Rules for the plan:

- Decide everything. The executor decides nothing: if you leave a choice open, it stops and asks, and
  the gate is wasted.
- Name every file with its full path and what changes in it, one row per file. A file you do not name
  will not be touched.
- Every gate is a command whose exit code is the answer. Write the command, not "run the tests".
- List the tests by name, including those that must change or be deleted with the code they cover.
- Say what you are not doing, and why. An unstated scope is a bug.
- When reality contradicts the plan while writing it, stop and ask; do not invent a workaround.
