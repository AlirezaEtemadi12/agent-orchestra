# A window of its own

**Status:** planned · **Added:** 2026-11-24 · **Plan:** [0012](../plans/0012-local-app-and-project-setup.md)

Orchestra as a desktop app. Plan 0012 builds the core: a Photino.NET window (WebView2) around the same
API and static UI, the system's own folder dialog, and a self-contained `win-x64` build. Linux and macOS
come later ([engine-cross-platform](engine-cross-platform.md)).

Why Photino and not Electron or Blazor (2026-11-24): the UI is already HTML and CSS. Photino shows it
without Node at runtime and without rewriting it. Electron would add Chromium and Node, and Blazor
would mean rewriting every component.

Left after plan 0012:
- an installer, code signing (Windows SmartScreen, Apple notarization) and auto-update;
- a tray icon, so closing the window can keep the app running.
