// Copied verbatim from plan 0003 §6.5. Never retype or "improve" these strings.
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
    "repos": "Repos",
    "profile": "Profile",
    "profileLater": "Profiles arrive in phase 2; every project uses the default for now."
  },
  "dockerHint": "In Docker, only folders mounted into the container can be checked.",
  "check": "Check folder",
  "checkAgain": "Check again",
  "found": "found",
  "missing": "missing",
  "folderMissing": "This folder does not exist.",
  "notAbsolute": "Use a full path.",
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
