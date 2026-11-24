// Plan 0003 §6.5 strings kept; the setup copy is plan 0012 §6.6.
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
  "table": {
    "check": "Check setup"
  },
  "setup": {
    "title": "Set up {name}",
    "continue": "Continue",
    "apply": "Set up",
    "back": "Back",
    "action": {
      "create": "will be created",
      "add": "will be added",
      "update": "will be updated",
      "exists": "already there"
    },
    "commands": "Build and test commands (one per line)",
    "nothing": "Everything is already set up.",
    "noEngine": "The engine folder was not found. Set Orchestra:EnginePath.",
    "waiting": "Waiting for the first event",
    "connected": "Connected — last event {time}.",
    "knowledge": "Fill the knowledge with an agent (optional)",
    "errors": {
      "WriteFailed": "Could not write this file.",
      "GitInitFailed": "git init failed; nothing after it was done.",
      "InvalidJson": "This JSON file could not be read, so it was left unchanged.",
      "Skipped": "Not done: an earlier step failed."
    }
  },
  "check": "Check folder",
  "checkAgain": "Check again",
  "found": "found",
  "missing": "missing",
  "folderMissing": "This folder does not exist.",
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
