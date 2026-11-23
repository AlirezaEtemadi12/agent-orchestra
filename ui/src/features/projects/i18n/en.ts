// Plan 0003 §6.5 strings kept; the field hints, hosting and connect copy are plan 0009 §6.7 and §6.8.
// Never retype or "improve" these strings.
export const projectsEn = {
  "title": "Projects",
  "new": "New project",
  "edit": "Edit project",
  "origin": {
    "config": "From the configuration file",
    "dashboard": "Added here"
  },
  "fields": {
    "name": "Name",
    "id": "Id",
    "rootPath": "Project folder",
    "runsPath": "Runs folder",
    "repos": "Repos"
  },
  "hints": {
    "rootPath": "The folder as you see it in Windows, e.g. F:\\Projects\\MyApp. The rest fills itself.",
    "name": "Shown in menus and filters.",
    "id": "Lowercase letters, digits and dashes. The engine's report block uses it; it cannot change later.",
    "repos": "The git repositories inside the folder. Found ones are ticked; add one by its folder name.",
    "runsPath": "Where the orchestrator keeps its runs. Usually .orchestra\\runs inside the project."
  },
  "hosting": {
    "docker": "Running in Docker. It can see: {roots}",
    "dockerNone": "Running in Docker, but no folder is shared with it. Add ORCHESTRA_HOST_ROOT to deploy/.env (the folder that holds your projects, e.g. F:/Projects) and restart.",
    "direct": "Running directly on this computer; any folder you can open works."
  },
  "form": {
    "notVisible": "The dashboard cannot see this folder.",
    "ready": "Ready for the orchestrator — no setup needed.",
    "browse": "Browse",
    "choose": "Choose this folder",
    "up": "Up",
    "badgeReady": "ready",
    "badgeRepo": "repo",
    "addRepo": "Add a repo folder name",
    "add": "Add",
    "inEachRepo": "in each repo",
    "advanced": "Advanced"
  },
  "connect": {
    "title": "Connect {name}",
    "block": "1. Add this block to .orchestra/orchestrator.json",
    "orPrompt": "Or give this prompt to your agent:",
    "local": "2. Add these keys to .orchestra/orchestrator.local.json yourself. They hold the token, so they never go to an agent. Keep enginePath as it is.",
    "noProfilesPath": "profilesPath is not included: the dashboard cannot tell where its profiles folder is on your computer. Keep yours; it is needed only when the project names a profile.",
    "waiting": "3. Waiting for the first event — start any run of this project.",
    "connected": "3. Connected — last event {time}.",
    "board": "Open Runs board",
    "back": "Back to projects",
    "copy": "Copy",
    "copied": "Copied"
  },
  "check": "Check folder",
  "checkAgain": "Check again",
  "found": "found",
  "missing": "missing",
  "folderMissing": "This folder does not exist.",
  "promptTitle": "Prompt for your agent",
  "promptHint": "Give this to the agent you work with in this folder. It creates the files; the dashboard never writes into your project.",
  "copy": "Copy",
  "copied": "Copied",
  "save": "Save",
  "saved": "Project saved",
  "remove": "Remove",
  "removeTitle": "Remove {name}?",
  "removeBody": "It leaves the dashboard with its runs and library. Its folder is not touched.",
  "removed": "Project removed",
  "storeUnreadable": "projects.json cannot be read. Projects added here are hidden until it is fixed or removed.",
  "errors": {
    "IdTaken": "This id is taken.",
    "required": "Required.",
    "idFormat": "Lowercase letters, digits and dashes, up to 40."
  }
};
