# Plan 0007 â€” Move Project A and Project B to `.ai/` + `.orchestra/`

**Status:** Draft â€” revised 2026-11-22 after plans 0005 and 0006; gate 2 passed (second pass: no blockers, 8 small points applied); waiting for the owner's approval
**Created:** 2026-11-19 Â· **Updated:** 2026-11-22 Â· **Done:** â€”
**Preview:** none of its own â€” preview 0001 Â§2 (approved 2026-11-19)
**Run:** â€”
**Planned by:** Claude Opus 5.5, 2026-11-19; revised 2026-11-22. **Executor:** another agent, ideally a cheaper model.
**Plan review:** gate 2 by another model family (Freebuff), 2026-11-22: first pass B1â€“B3 + 12 smaller points; second pass no blockers, 8 smaller points â€” all applied in this version
**Decided by:** â€” (gate 2)
**Branch:** `alireza/orchestra-layout` here; each companion names its own
**Base:** the commit that merges plan 0006 into `main` (phase A does not start before that merge)
**Companion:** **two** â€” `Project B` and `Project A`, each run in a session opened in that workspace,
from the brief in `.orchestra/prompts/move-to-orchestra-layout.md` (file 6).
**Steps changed:** no preview of its own (owner, 2026-11-19)

---

## ÙÙ‡Ù… Ù…Ù† Ø§Ø² Ø¯Ø±Ø®ÙˆØ§Ø³Øª

Ø¯Ùˆ Ù¾Ø±ÙˆÚ˜Ù‡â€ŒÛŒ ÙØ¹Ù„ÛŒ Ø¨Ø§ÛŒØ¯ Ù‡Ù…Ø§Ù† Ø³Ø§Ø®ØªØ§Ø±ÛŒ Ø±Ø§ Ø¨Ú¯ÛŒØ±Ù†Ø¯ Ú©Ù‡ Orchestra Ø¯Ø§Ø±Ø¯:
- `.ai/` ÙÙ‚Ø· Ø¯Ø§Ù†Ø´ Ù¾Ø±ÙˆÚ˜Ù‡ Ø¨Ø§Ø´Ø¯.
- Ù‡Ø± Ú†ÛŒØ²ÛŒ Ú©Ù‡ Ù…Ø§Ù„ Ø§Ø±Ú©Ø³ØªØ± Ø§Ø³Øª Ø¯Ø± `.orchestra/` Ø¨Ø§Ø´Ø¯: workflowØŒ Ù†Ù‚Ø´â€ŒÙ‡Ø§ØŒ promptÙ‡Ø§ØŒ Ù¾Ù„Ù†â€ŒÙ‡Ø§ØŒ Ù¾ÛŒØ´â€ŒÙ†Ù…Ø§ÛŒØ´â€ŒÙ‡Ø§ØŒ
  Ø§ÛŒØ¯Ù‡â€ŒÙ‡Ø§ØŒ ØªØ­Ù‚ÛŒÙ‚â€ŒÙ‡Ø§ØŒ Ø´ÙˆØ§Ù‡Ø¯ØŒ runÙ‡Ø§ Ùˆ ØªÙ†Ø¸ÛŒÙ…Ø§Øª Ù¾Ø±ÙˆÚ˜Ù‡ (`orchestrator.json`).

ÛŒÚ© ÙˆØ§Ù‚Ø¹ÛŒØª Ù…Ù‡Ù…: Ù‡Ø± Ø¯Ùˆ Ù¾Ø±ÙˆÚ˜Ù‡ **Ú†Ù†Ø¯ repo Ø²ÛŒØ± ÛŒÚ© Ù¾ÙˆØ´Ù‡â€ŒÛŒ workspace** Ø¯Ø§Ø±Ù†Ø¯ØŒ Ùˆ Ø®ÙˆØ¯ workspace repo Ù†ÛŒØ³Øª:
- Project B: AgentØŒ Ui Ùˆ Web.
- Project A: Ui Ùˆ Web.

Ù¾ÙˆØ´Ù‡â€ŒÛŒ `orchestrator/` Ù‡Ø± Ú©Ø¯Ø§Ù… Ø®ÙˆØ¯Ø´ ÛŒÚ© repo Ø¬Ø¯Ø§ Ø§Ø³Øª. Ø¨Ø¹Ø¯ Ø§Ø² Ù¾Ù„Ù† 0005 ÙÙ‚Ø· Ø§ÛŒÙ†â€ŒÙ‡Ø§ Ø±Ø§ Ø¯Ø§Ø±Ø¯: shim
(`run.ps1`)ØŒ `orchestrator.json`ØŒ ÙØ§ÛŒÙ„ Ù…Ø­Ù„ÛŒ git-ignoredØŒ Ú†Ú©â€ŒÙ„ÛŒØ³Øª Ø¨Ø³ØªÙ† Ù¾Ù„Ù† Ùˆ `runs/`.

ØªØµÙ…ÛŒÙ…â€ŒÙ‡Ø§ (Ø¬ÙˆØ§Ø¨â€ŒÙ‡Ø§ÛŒ Â§8ØŒ 2026-11-20):
- `orchestrator/` Ø¨Ù‡ **`.orchestra/` Ø¯Ø± Ø±ÛŒØ´Ù‡â€ŒÛŒ workspace** ØªØºÛŒÛŒØ± Ù†Ø§Ù… Ù…ÛŒâ€ŒØ¯Ù‡Ø¯ Ùˆ Ù‡Ù…Ø§Ù† repo git Ø®ÙˆØ¯Ø´ Ø±Ø§ Ù†Ú¯Ù‡ Ù…ÛŒâ€ŒØ¯Ø§Ø±Ø¯.
- Ù¾Ù„Ù†â€ŒÙ‡Ø§ØŒ Ù¾ÛŒØ´â€ŒÙ†Ù…Ø§ÛŒØ´â€ŒÙ‡Ø§ØŒ Ø§ÛŒØ¯Ù‡â€ŒÙ‡Ø§ Ùˆ ØªØ­Ù‚ÛŒÙ‚â€ŒÙ‡Ø§ÛŒ **Ù‡Ù…Ù‡â€ŒÛŒ repoÙ‡Ø§ÛŒ Ø¢Ù† Ù¾Ø±ÙˆÚ˜Ù‡** Ø¯Ø± Ø¢Ù† Ø¬Ù…Ø¹ Ù…ÛŒâ€ŒØ´ÙˆÙ†Ø¯ØŒ Ø¨Ù‡ ØªÙÚ©ÛŒÚ© repo
  (`plans/Web/0032-â€¦`).
- Ù‡Ø± repo ÙÙ‚Ø· `.ai/` Ø¯Ø§Ù†Ø´ Ø®ÙˆØ¯Ø´ Ø±Ø§ Ù†Ú¯Ù‡ Ù…ÛŒâ€ŒØ¯Ø§Ø±Ø¯.
- Ø³Ù‡ Ù†Ø³Ø®Ù‡â€ŒÛŒ `workflow.md` Ø¯Ø± Project B ÛŒÚ©ÛŒ Ù…ÛŒâ€ŒØ´ÙˆÙ†Ø¯ØŒ Ùˆ Ø§ÛŒÙ† ÛŒÚ© ØªØºÛŒÛŒØ± Ù‚Ø§Ø¹Ø¯Ù‡ Ø§Ø³Øª Ú©Ù‡ Ø¨Ø±Ø±Ø³ÛŒ Ù…ÛŒâ€ŒØ´ÙˆØ¯.
- Project A Ø®ØµÙˆØµÛŒ Ù…ÛŒâ€ŒÙ…Ø§Ù†Ø¯ Ùˆ repo Ø¹Ù…ÙˆÙ…ÛŒ Orchestra Ù‡ÛŒÚ† Ù…Ø³ÛŒØ± ÛŒØ§ Ù…Ø­ØªÙˆØ§ÛŒÛŒ Ø§Ø² Ø¢Ù† Ø±Ø§ Ù†Ø§Ù… Ù†Ù…ÛŒâ€ŒØ¨Ø±Ø¯.

Ù‡Ø²ÛŒÙ†Ù‡â€ŒÛŒ Ø§ÛŒÙ† Ø±ÙˆØ´: Ù¾Ù„Ù† Ø¯ÛŒÚ¯Ø± Ø¯Ø± ØªØ§Ø±ÛŒØ®Ú†Ù‡â€ŒÛŒ git Ù‡Ù…Ø§Ù† repo Ú©Ø¯ Ù†ÛŒØ³Øª Ùˆ Ø¯Ø± PR Ø¢Ù† Ø¯ÛŒØ¯Ù‡ Ù†Ù…ÛŒâ€ŒØ´ÙˆØ¯.

Ø§ÛŒÙ† Ù¾Ù„Ù† Ú©Ø§Ø±Ù‡Ø§ÛŒ Ø²ÛŒØ± Ø±Ø§ **Ù†Ù…ÛŒâ€ŒÚ©Ù†Ø¯**:
- Ù…Ø­ØªÙˆØ§ÛŒ Ù‡ÛŒÚ† Ù‚Ø§Ø¹Ø¯Ù‡ ÛŒØ§ Ù¾Ù„Ù†ÛŒ Ø±Ø§ ØªØºÛŒÛŒØ± Ù†Ù…ÛŒâ€ŒØ¯Ù‡Ø¯ (Ø¬Ø² ÛŒÚ©ÛŒ Ú©Ø±Ø¯Ù† workflowÙ‡Ø§)Ø› ÙÙ‚Ø· Ø¬Ø§Ø¨Ù‡â€ŒØ¬Ø§ Ù…ÛŒâ€ŒÚ©Ù†Ø¯ Ùˆ Ù„ÛŒÙ†Ú©â€ŒÙ‡Ø§ Ø±Ø§ Ø¯Ø±Ø³Øª Ù…ÛŒâ€ŒÚ©Ù†Ø¯.
- ØªØ§Ø±ÛŒØ®Ú†Ù‡â€ŒÛŒ git Ø±Ø§ Ø¨Ø§Ø²Ù†ÙˆÛŒØ³ÛŒ Ù†Ù…ÛŒâ€ŒÚ©Ù†Ø¯ Ùˆ Ú†ÛŒØ²ÛŒ Ø±Ø§ push Ù†Ù…ÛŒâ€ŒÚ©Ù†Ø¯.
- Ù¾Ø±ÙˆÙØ§ÛŒÙ„ `default` Ø±Ø§ Ø¹ÙˆØ¶ Ù†Ù…ÛŒâ€ŒÚ©Ù†Ø¯: Ù…Ø³ÛŒØ±Ù‡Ø§ÛŒ ØªØ§Ø²Ù‡ Ø¯Ø± `orchestrator.json` Ø®ÙˆØ¯ Ù‡Ø± Ù¾Ø±ÙˆÚ˜Ù‡ Ù…ÛŒâ€ŒÙ†Ø´ÛŒÙ†Ù†Ø¯ØŒ Ú†ÙˆÙ†
  Ø¯Ùˆ Ù¾Ø±ÙˆÚ˜Ù‡ Ø¯Ø± Ø¯Ùˆ Ø²Ù…Ø§Ù† Ø¬Ø§Ø¨Ù‡â€ŒØ¬Ø§ Ù…ÛŒâ€ŒØ´ÙˆÙ†Ø¯ Ùˆ Ø¯Ø§Ø¯Ù‡â€ŒÛŒ Ù¾Ø±ÙˆÚ˜Ù‡ Ù¾ÛŒØ´ Ø®ÙˆØ¯ Ù¾Ø±ÙˆÚ˜Ù‡ Ù…ÛŒâ€ŒÙ…Ø§Ù†Ø¯.

## Ø¬Ø±ÛŒØ§Ù† Ú©Ø§Ø±

1. Ø³Ù…Øª Orchestra (Ù‡Ù…ÛŒÙ† repoØŒ ÙØ§Ø² A):
   - Ù…ÙˆØªÙˆØ± ÛŒØ§Ø¯ Ù…ÛŒâ€ŒÚ¯ÛŒØ±Ø¯ Ù¾Ù„Ù†â€ŒÙ‡Ø§ Ø±Ø§ Ø¯Ø± `.orchestra/plans/{repo}/` Ù¾ÛŒØ¯Ø§ Ú©Ù†Ø¯ Ùˆ ÛŒÚ© Ù¾Ù„Ù† Ø±Ø§ Ø¨Ø§ Ø³Ø·Ø±
     `**Status:** Done` Â«Ø¨Ø³ØªÙ‡Â» Ø¨Ø¯Ø§Ù†Ø¯ØŒ Ù†Ù‡ Ø¨Ø§ Ù¾ÙˆØ´Ù‡â€ŒÛŒ `done/`. Ù¾Ø±ÙˆÚ˜Ù‡â€ŒØ§ÛŒ Ú©Ù‡ Ù‡Ù†ÙˆØ² Ø¬Ø§Ø¨Ù‡â€ŒØ¬Ø§ Ù†Ø´Ø¯Ù‡ Ù…Ø«Ù„ Ø§Ù…Ø±ÙˆØ² Ú©Ø§Ø± Ù…ÛŒâ€ŒÚ©Ù†Ø¯.
   - Ù…ØªÙ†â€ŒÙ‡Ø§ÛŒÛŒ Ú©Ù‡ Ø§Ø³Ù… `orchestrator/run.ps1` Ø±Ø§ Ù…ÛŒâ€ŒØ¨Ø±Ù†Ø¯ØŒ Ø§Ø³Ù… `.orchestra/run.ps1` Ø±Ø§ Ù‡Ù… Ù…ÛŒâ€ŒØ¨Ø±Ù†Ø¯.
   - Ø¯Ø§Ø´Ø¨ÙˆØ±Ø¯ `orchestrator.json` Ù¾Ø±ÙˆÚ˜Ù‡ Ø±Ø§ Ø§ÙˆÙ„ Ø¯Ø± `.orchestra/` Ùˆ Ø¨Ø¹Ø¯ Ø¯Ø± `orchestrator/` Ù…ÛŒâ€ŒØ¬ÙˆÛŒØ¯.
   - Ù†Ù‚Ø´â€ŒÙ‡Ø§ Ùˆ promptÙ‡Ø§ Ø¨Ø±Ø§ÛŒ Ù‡Ø± repo Ø¬Ø¯Ø§ Ù…ÛŒâ€ŒÙ…Ø§Ù†Ù†Ø¯ (`roles/Web/`)ØŒ Ú†ÙˆÙ† Ù†Ø³Ø®Ù‡â€ŒÙ‡Ø§ÛŒ repoÙ‡Ø§ Ø¨Ø§ Ù‡Ù… ÙØ±Ù‚ Ø¯Ø§Ø±Ù†Ø¯.
   - brief Ù‡Ù…Ø±Ø§Ù‡ (prompt) Ù†ÙˆØ´ØªÙ‡ Ù…ÛŒâ€ŒØ´ÙˆØ¯.
2. Ø¨Ø±Ø§ÛŒ Ù‡Ø± Ù¾Ø±ÙˆÚ˜Ù‡ØŒ Ø¯Ø± ÛŒÚ© session Ø¯Ø§Ø®Ù„ Ù‡Ù…Ø§Ù† workspaceØŒ Ú©Ø§Ø± Ø§Ø² Ø±ÙˆÛŒ brief Ø§Ù†Ø¬Ø§Ù… Ù…ÛŒâ€ŒØ´ÙˆØ¯:
   - ØªØºÛŒÛŒØ± Ù†Ø§Ù… Ù¾ÙˆØ´Ù‡â€ŒÛŒ `orchestrator/` Ø¨Ù‡ `.orchestra/` (Ø®ÙˆØ¯ Ù¾ÙˆØ´Ù‡ Ø±ÛŒØ´Ù‡â€ŒÛŒ repo Ø§Ø³ØªØŒ Ù¾Ø³ git Ú†ÛŒØ²ÛŒ Ù†Ù…ÛŒâ€ŒØ¨ÛŒÙ†Ø¯)ØŒ
   - Ø§Ù†ØªÙ‚Ø§Ù„ workflowØŒ Ù†Ù‚Ø´â€ŒÙ‡Ø§ØŒ promptÙ‡Ø§ØŒ Ù¾Ù„Ù†â€ŒÙ‡Ø§ØŒ Ù¾ÛŒØ´â€ŒÙ†Ù…Ø§ÛŒØ´â€ŒÙ‡Ø§ Ùˆ Ø§ÛŒØ¯Ù‡â€ŒÙ‡Ø§ Ø§Ø² repoÙ‡Ø§ Ø¨Ù‡ `.orchestra/`ØŒ
   - Ø¯Ø±Ø³Øª Ú©Ø±Ø¯Ù† Ù…Ø³ÛŒØ±Ù‡Ø§ Ø¯Ø± `orchestrator.json`ØŒ Ù„ÛŒÙ†Ú©â€ŒÙ‡Ø§ØŒ skillÙ‡Ø§ Ùˆ `AGENTS.md`ØŒ
   - Ùˆ ÛŒÚ© commit Ø¯Ø± Ù‡Ø± repoØŒ **ÙÙ‚Ø· ÙˆÙ‚ØªÛŒ Ø´Ù…Ø§ Ø¨Ú¯ÙˆÛŒÛŒØ¯**.
3. Ø´Ù…Ø§ ØªÙ†Ø¸ÛŒÙ…Ø§Øª Orchestra Ø±Ø§ Ø¨Ø±Ø§ÛŒ Ø¢Ù† Ù¾Ø±ÙˆÚ˜Ù‡ Ø¹ÙˆØ¶ Ù…ÛŒâ€ŒÚ©Ù†ÛŒØ¯ (`RunsPath` = `<root>/.orchestra/runs`) Ùˆ Ù¾Ø±ÙˆÚ˜Ù‡
   Ø¯ÙˆØ¨Ø§Ø±Ù‡ Ø®ÙˆØ§Ù†Ø¯Ù‡ Ù…ÛŒâ€ŒØ´ÙˆØ¯. ØªØ¹Ø¯Ø§Ø¯ runÙ‡Ø§ Ùˆ Ù¾Ù„Ù†â€ŒÙ‡Ø§ Ø¨Ø§ÛŒØ¯ Ø¨Ø§ Ù‚Ø¨Ù„ ÛŒÚ©ÛŒ Ø¨Ø§Ø´Ø¯.

ÙØ§Ø²Ù‡Ø§:
- **A:** Ù¾Ø´ØªÛŒØ¨Ø§Ù†ÛŒ Ù…ÙˆØªÙˆØ± Ùˆ brief.
- **B:** companion Ø¯Ø± Project B.
- **C:** companion Ø¯Ø± Project A.

## Ù†ØªÛŒØ¬Ù‡ Ùˆ Ø¢Ù†Ú†Ù‡ Ø¨Ø¹Ø¯ Ø§Ø² Ù¾ÛŒØ§Ø¯Ù‡â€ŒØ³Ø§Ø²ÛŒ ØªØ³Øª Ù…ÛŒâ€ŒÚ©Ù†ÛŒØ¯

- Ø¯Ø± Ù‡Ø± Ø¯Ùˆ workspaceØŒ `orchestrator/` Ø¯ÛŒÚ¯Ø± Ù†ÛŒØ³Øª Ùˆ `.orchestra/` Ø¨Ø§ Ù‡Ù…Ù‡â€ŒÛŒ Ø¯Ø³ØªÙ‡â€ŒÙ‡Ø§ Ù‡Ø³Øª.
- `powershell -File .orchestra/run.ps1 agents` Ùˆ `health` Ú©Ø§Ø± Ù…ÛŒâ€ŒÚ©Ù†Ù†Ø¯.
- Ø¯Ø± Ù‡Ø± repoØŒ `.ai/` Ø¯ÛŒÚ¯Ø± `plans/`ØŒ `agents/` ÛŒØ§ `prompts/` Ù†Ø¯Ø§Ø±Ø¯.
- Ø¯Ø± Ø¯Ø§Ø´Ø¨ÙˆØ±Ø¯ØŒ ØªØ¹Ø¯Ø§Ø¯ runÙ‡Ø§ Ùˆ Ù¾Ù„Ù†â€ŒÙ‡Ø§ÛŒ Ù‡Ø± Ù¾Ø±ÙˆÚ˜Ù‡ Ù‚Ø¨Ù„ Ùˆ Ø¨Ø¹Ø¯ Ø§Ø² Ø§Ù†ØªÙ‚Ø§Ù„ ÛŒÚ©ÛŒ Ø§Ø³ØªØŒ Ùˆ Ù¾Ù„Ù†ÛŒ Ú©Ù‡ Ø´Ù…Ø§ Ø§Ù†ØªØ®Ø§Ø¨ Ù…ÛŒâ€ŒÚ©Ù†ÛŒØ¯
  Ù‡Ù†ÙˆØ² Ø¨Ù‡ run Ø®ÙˆØ¯Ø´ Ù„ÛŒÙ†Ú© Ø§Ø³Øª.
- ØµÙØ­Ù‡â€ŒÛŒ effective Ù‡Ø± Ù¾Ø±ÙˆÚ˜Ù‡ Ø¯Ø± Ø¯Ø§Ø´Ø¨ÙˆØ±Ø¯ Ù‡Ù†ÙˆØ² Ø¨Ø§Ø² Ù…ÛŒâ€ŒØ´ÙˆØ¯ (Û´Û²Û² Ù†Ù…ÛŒâ€ŒØ¯Ù‡Ø¯).
- skill Ù‡Ù…Ø§Ù‡Ù†Ú¯â€ŒÚ©Ù†Ù†Ø¯Ù‡â€ŒÛŒ Ù‡Ø± Ù¾Ø±ÙˆÚ˜Ù‡ Ù‡Ù†ÙˆØ² `CONDUCTOR.md` Ùˆ Ú†Ú©â€ŒÙ„ÛŒØ³Øª Ø¨Ø³ØªÙ† Ù¾Ù„Ù† Ø±Ø§ Ù¾ÛŒØ¯Ø§ Ù…ÛŒâ€ŒÚ©Ù†Ø¯.
- `close` ÛŒÚ© run Ú©Ù‡ Ù¾Ù„Ù†Ø´ `Status: Done` Ø¯Ø§Ø±Ø¯ Ú©Ø§Ø± Ù…ÛŒâ€ŒÚ©Ù†Ø¯.
- `git log` Ø¯Ø± Ù‡Ø± repo: ÛŒÚ© commit Ø¬Ø§Ø¨Ù‡â€ŒØ¬Ø§ÛŒÛŒØ› `git diff -M --stat` Ù‡Ù…Ù‡ Ø±Ø§ rename Ù†Ø´Ø§Ù† Ù…ÛŒâ€ŒØ¯Ù‡Ø¯.

## 0. Ground rules for the executor

1. Phase A changes only this repo. Phases Bâ€“C are companions in their workspaces.
2. **Moves only.** Files cross repos, so a move is copy + `git add` in `.orchestra/` and `git rm` in
   the product repo (Â§6.2); content edits only for links and paths a move broke,
   the one workflow merge (D7), and the `**Status:** Done` line D4 adds.
3. Every repo of the project must be clean of code (workflow.md, "Dirty means code") and no run may be
   in flight (`run.ps1 queue` empty, no `runs/T-*/task.json` in `Implementing`) before its companion starts.

### Baseline

Plans 0001â€“0006 merged; offline suite 133/133 on both configs; dashboard counts per project noted
before the move (runs, plans, previews, ideas).

## 1. Goal

Both projects follow ADR 0005's layout; the engine, the skills and the dashboard keep working with no
change in behaviour or counts.

## 2. Out of scope

Rewriting rules (except the D7 merge); merging per-repo `.ai/` knowledge docs; changing plan numbering;
changing the `default` profile; pushing; deleting history; a dashboard "move check" screen (dropped
2026-11-22, D8).

## 3. Rules that apply

[ADR 0005](../../.ai/decisions/0005-ai-is-knowledge-orchestra-is-work.md); plan 0006 (a project's
settings live in its own `orchestrator.json`); each project's own `workflow.md` gates for its companion.

## 4. Decisions already made

| # | Decision |
|---|---|
| D1 | The folder `orchestrator/` is renamed on disk to `.orchestra/`. It is the root of its own repo, so the rename changes nothing inside git; `orchestrator.json`, the git-ignored `orchestrator.local.json` and `runs/` move with it unchanged in name. The engine finds the workspace through `root` (relative to the config file), so it needs no change for the rename. There is **no** `project.json` (plan 0006) |
| D2 | The new paths are overrides in the project's own `orchestrator.json` `workflow` block. **Roles and prompts stay per repo** (review B2, 2026-11-22: the repos' copies differ, and a merge would be a rule change nobody asked for): `rolesPath: ".orchestra/roles/{repo}"`, `rolesIndex: ".orchestra/roles/{repo}/README.md"`, `promptsPath: ".orchestra/prompts/{repo}"`. The gates are one file: `gatesPath: ".orchestra/workflow.md"` (D7). `planTemplate: ".orchestra/plans/TEMPLATE.md"`; `agentsDoc` and `indexDoc` stay `{repo}/AGENTS.md` and `{repo}/.ai/README.md`. The profile is not touched |
| D3 | New **top-level** config key `plansDir` (next to `planFiling`; a project key, not a profile key â€” plan 0006 D1), workspace-relative with the `{repo}` token: `.orchestra/plans/{repo}`. Folder names are the repo names as configured (`plans/Web/`), no lowercasing. When `plansDir` is set, `close` looks for the plan there (by number, or by slug for `planFiling: slug`) and accepts only a file whose `**Status:**` line starts with `Done`. When it is not set, today's `plansDoneDir` behaviour is unchanged |
| D3a | **Where a plan is written** (review B3): with `plansDir` set, gate 1 writes the plan to `<plansDir>/NNNN-topic.md` (a copy of `runs/_drafts/<id>.plan.md`; slug filing: `<slug>.md`) and the preview to `.orchestra/previews/NNNN-topic/index.html`. The plan stays there for its whole life; closing it changes only its `Status:` line. CONDUCTOR.md and each project's close-plan checklist say this |
| D4 | Plan files keep their names. `done/` disappears: finished plans move next to open ones; a finished plan that has no `**Status:**` line gets `**Status:** Done` as its second line, and a Status line in another form (`Status: **Done**`, unbolded `Status: Done`) is rewritten to `**Status:** Done â€¦` keeping the rest of the line â€” no other content change. `close` accepts the same forms anyway: `(?i)^\s*\*{0,2}Status:?\*{0,2}\s*\*{0,2}Done` |
| D4a | The dashboard reads the project file from `<root>/.orchestra/orchestrator.json`, falling back to `<root>/orchestrator/orchestrator.json` (review B1: `EffectiveConfigHandler` hardcodes the old path). `EffectiveConfigDto` gains `configPath`, the root-relative path it read (`.orchestra/orchestrator.json`), and the effective page's copy text uses it instead of the literal |
| D4b | `LibraryDefaults.Previews` gains `.orchestra/runs/_drafts/*.html`, so in-flight preview drafts stay indexed once the projects' `Library` globs are removed |
| D5 | `cleanTree.ignore` in each project drops `.ai/plans/` and keeps `.ai/context/` |
| D6 | The worker guard (`agents.ps1:887`, "never run orchestrator/run.ps1") and the `close` help (`run.ps1:72-73`) name both `orchestrator/run.ps1` and `.orchestra/run.ps1`. The footer (`run.ps1:109`) points at the engine's own docs, `engine/docs/CONDUCTOR.md` and `engine/docs/WORKING-MODEL.md` (stale since plan 0005); the help line for `close` says "filed (Status: Done in plansDir, or in plansDoneDir)" |
| D7 | Project B's three `workflow.md` copies (owner, 2026-11-20: one file): identical â†’ one file; different â†’ the companion STOPS, shows the owner the diff, and the merged text is a rule change the owner approves in that session. Roles and prompts are **not** merged (D2) |
| D8 | No dashboard move-check endpoint or card: a one-time move of two projects does not earn an API, a UI card and tests. The companion's preconditions (rule 3) are checked by the session itself with `git status` and `run.ps1 queue` |
| D9 | The dashboard needs two code changes (D4a, D4b); the rest already works: `LibraryDefaults` indexes `.orchestra/plans/*.md` and `.orchestra/plans/*/*.md`, and `DeriveRootPath` accepts `<root>/.orchestra/runs`. Per project the owner changes `RunsPath` to `<root>/.orchestra/runs` and removes any `Library` globs (in `appsettings.Local.json` or `deploy/.env`) |

## 5. File manifest (this repo)

| # | File | NEW/EDIT | Phase |
|---|---|---|---|
| 1 | `engine/run.ps1` â€” `close`: D3 (`plansDir` + Status check, both filing modes, one shared `$planDir` variable instead of the two `$doneDir` lines); help (`:72-73`) and footer (`:109`) text (D6) | EDIT | A |
| 2 | `engine/lib/agents.ps1` â€” worker guard text at `:887` (D6). Nothing else: the engine has no known-keys check | EDIT | A |
| 3 | `engine/tests/offline/run-offline.ps1` â€” T-PD1â€“6 (Â§7) | EDIT | A |
| 4 | `engine/README.md` â€” the project layout drawn as `.orchestra/` (old name noted), `plansDir` documented; `engine/examples/orchestrator.json` lines 3 (copy destination) and 11â€“17 (`gatesPath`/`rolesPath`/`rolesIndex`/`promptsPath`/`planTemplate` to the D2 values), plus `"plansDir": ".orchestra/plans/{repo}"` | EDIT | A |
| 5 | `engine/docs/CONDUCTOR.md` â€” paths to `.orchestra/`; D3a (gate 1 writes the plan into `plansDir` and the preview into `previews/`; close = set `Status: Done`), with the `plansDoneDir` text kept for a project without `plansDir` | EDIT | A |
| 6 | `.orchestra/prompts/move-to-orchestra-layout.md` â€” the companion brief (Â§6.2), with no project path, run id or plan number | NEW | A |
| 7 | `api/src/Orchestra.Api/Modules/Projects/Features/EffectiveConfig/EffectiveConfigHandler.cs` â€” D4a | EDIT | A |
| 8 | `api/tests/Orchestra.Api.Tests/Profiles/EffectiveConfigTests.cs` â€” one test per location (new, old, neither â†’ 422 as today), each asserting `configPath` | EDIT | A |
| 8a | `ui/src/features/projects/components/project-effective-config.tsx` (`:68`) and `ui/src/features/projects/types.ts` â€” the copy text names `configPath` (D4a) | EDIT | A |
| 8b | `api/src/Orchestra.Api/Modules/Library/LibraryDefaults.cs` and `api/tests/Orchestra.Api.Tests/Library/` â€” D4b, one test: a `.orchestra/runs/_drafts/T-X.html` is indexed as a preview | EDIT | A |
| 9 | `.orchestra/plans/README.md` â€” row 0007 | EDIT | A |

## 6. Per-file specification

### 6.1 `close` with `plansDir`

```
$pd = top-level plansDir from $Ctx.Config, else $null
if ($pd) {
  if (-not $state.repo) { throw "run '<id>' has no repo in task.json, so its plan folder is unknown. close -Force to delete it anyway." }
  $planDir = Resolve-WorkflowPath $Ctx $pd $state.repo
  <collect every candidate by today's matching (number; or slug exact, -slug suffix, branch text)>
  <keep only candidates whose first 15 lines match the D4 Status pattern; accept the first>
} else { $planDir = <today's plansDoneDir>; <today's code, unchanged> }
```

The Done filter runs **before** a candidate is chosen, so the run's own open plan (which also contains
its branch) never hides a Done copy (review point 4). Messages in `plansDir` mode:
- candidates found, none Done â†’ `plan <n> is in <dir> but its Status is not Done yet. Close the plan
  first (the close-plan checklist), or close -Force to delete the run anyway.`
- no candidate â†’ `plan <n> is not in <dir>. Write it there first (gate 1), or close -Force to delete
  the run anyway.`

### 6.2 Companion brief (per workspace)

1. Preconditions: ground rule 3; the owner said go in that session; dashboard counts written down.
2. Close the terminals/sessions that use `orchestrator/`; rename the folder to `.orchestra/` (D1).
3. Inside `.orchestra/` (its own repo), with `git mv` from the product repos impossible across repos,
   **copy then `git add`** here and **`git rm`** there:
   - `workflow.md` (D7);
   - `roles/<Repo>/` from `<repo>/.ai/agents/*.md` except `workflow.md` (its `README.md` stays
     `roles/<Repo>/README.md`); `prompts/<Repo>/` from `<repo>/.ai/prompts/` (D2). `rolesIndex` is
     set only if some repo had `.ai/agents/README.md`; it points at that file's new place and is
     otherwise left out (a missing index is a silent no-op in the engine);
   - `plans/<Repo>/` from `<repo>/.ai/plans/*.md` and `done/*.md` except `TEMPLATE.md` (D4); each
     repo's `.ai/plans/README.md` is kept as `plans/<Repo>/README.md` (the library never indexes a
     `README.md`, so counts do not change);
   - one `plans/TEMPLATE.md` at the root, by the D7 rule: identical copies â†’ one file; different â†’
     STOP and ask the owner which one;
   - `previews/` (root `*.html` and `runs/_drafts/*.html`), `ideas/` (root `ideas.md` as
     `ideas/README.md`), `research/`, `evidence/` (root `screenshots/`).
   - `.orchestra/.gitignore` keeps what it has and lists `runs/` and `evidence/` (ADR 0005).
4. `orchestrator.json`: D2 overrides, top-level `plansDir` (D3), D5.
5. In each product repo: fix links in `AGENTS.md` and `.ai/README.md` (relative `../.orchestra/â€¦`).
6. Skills under `<root>/.claude/skills/` and the close-plan checklist: paths to `.orchestra/â€¦`, and D3a
   (where gate 1 writes a plan and a preview; close = `Status: Done`).
7. `powershell -File .orchestra/run.ps1 agents` and `health` pass.
8. Commit only on the owner's word, one commit per repo: `Move orchestrator docs to .orchestra/ (plan
   0007, no content change)`.
9. The owner switches the dashboard setting (D9), re-imports and compares counts with step 1.

"Moves only" is checked per region, and the session log records each:
- **tracked product-repo files** â†’ the product repos show only deletions plus link edits;
  `.orchestra/` shows additions whose content equals the deleted files (a hash list), except the D4
  `Status` lines;
- **untracked workspace-root files** (root `*.html`, `ideas.md`, `research/`, `screenshots/`) â†’ a hash
  list before and after; no deletion side exists;
- **`<root>/.claude/skills/`** is outside every repo â†’ a local edit, listed in the session log for the
  owner to accept; no commit.

## 7. Tests

| Test | Facts |
|---|---|
| T-PD1 | `plansDir` set, plan `0042-x.md` in `plans/Web/` with `**Status:** Done` â†’ close succeeds |
| T-PD2 | same plan with `**Status:** Running` â†’ close refuses with the "not Done" line |
| T-PD3 | `planFiling: slug`, folder holds the run's own open plan (Running, names the branch) **and** a Done copy â†’ close succeeds on the Done one |
| T-PD4 | no `plansDir` â†’ today's `plansDoneDir` cases still pass (the existing tests unchanged) |
| T-PD5 | `plansDir` set, no plan file in the folder â†’ the "not in <dir>. Write it there first (gate 1)" line |
| T-PD6 | `plansDir` set, run whose `task.json` has no `repo` â†’ the "no repo" line, nothing read from `.orchestra/plans/` |
| companions | their own: `agents`, `health`, the per-side diff check, the dashboard counts |

## 8. Stop and ask â€” open questions for the owner

1. Where do a multi-repo project's plans live? â€” **answered 2026-11-20:** the workspace's `.orchestra/plans/<repo>/`.
2. The three `workflow.md` copies in Project B â€” **answered 2026-11-20:** merge into one, reviewed as a rule change (D7).
3. Project A is a client project â€” **answered 2026-11-20:** private only; Orchestra's public repo never names its paths or content.

## 9. Phases

- **A â€” engine and dashboard support, and the brief.** Files 1â€“9. **Gate:** `dotnet test` and the
  offline suite green on both configs; the suite count is the base's count (133 at 0006) + 6 per config,
  with T-PD1â€“6 listed in the log; UI typecheck, lint, test and build. Stop.
- **B â€” Project B companion.** From file 6, in that workspace. **Owner checks:** the items of Â«Ù†ØªÛŒØ¬Ù‡Â» for Project B.
- **C â€” Project A companion.** The same. **Owner checks:** the same for Project A.

## 10. Definition of done

Both workspaces on the new layout; counts equal before and after; skills and engine working; session
logs written here and there; this header `Status: Done` with the date and the commits.
