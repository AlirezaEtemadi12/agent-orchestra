# Orchestra on Linux and macOS

**Status:** ready · **Added:** 2026-11-24 · **Plan:** —

Owner, 2026-11-24: Windows first, multi-platform later. It is on the roadmap under "Later". Plan 0012
builds the desktop app for Windows only, but adds nothing Windows-only to the app code: Photino.NET runs
on all three systems. What blocks Linux and macOS is the engine, which is written for Windows
PowerShell 5.1: the shim calls `powershell`, and paths are built with `\`.

The work, in order:
1. **The engine on PowerShell 7 (`pwsh`)** on every OS, still working on 5.1 on Windows as long as that
   is needed. Go through `engine/lib` for Windows-only calls: `\` in paths, `Start-Process` options,
   process and lock handling, encoding defaults.
2. **The offline suite on `pwsh` on Linux**, so a regression shows up.
3. **A shim that picks `pwsh` or `powershell`**, and setup's refused-folder list for each OS.
4. **Builds:** `linux-x64` and `osx-arm64` in `scripts/publish.ps1`. Linux needs WebKitGTK on the machine.
   macOS needs signing to open without warnings.

The agent CLIs (`claude`, `opencode`, …) already exist on Linux and macOS.

Related: [a window of its own](desktop-window.md).
