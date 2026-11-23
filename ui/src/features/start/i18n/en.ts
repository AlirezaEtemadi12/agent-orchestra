// Copied from plan 0009 §6.8. Never retype or "improve" these strings.
export const startEn = {
  "title": "Getting started",
  "done": "Done",
  "todo": "To do",
  "s1": {
    "title": "Run the dashboard",
    "docker": "Docker: put ORCHESTRA_HOST_ROOT=<the folder that holds your projects> in deploy/.env, then run: docker compose -f deploy/docker-compose.yml up -d",
    "iis": "IIS: install the ASP.NET Core 10 Hosting Bundle, give the app pool read access to your projects folder, and bind the site to localhost only."
  },
  "s2": {
    "title": "Add a project",
    "body": "Give the folder as you see it in Windows; the rest fills itself.",
    "action": "New project"
  },
  "s3": {
    "title": "Connect it",
    "body": "Paste the two pieces from the project's Connect page; this step turns green with the first event."
  },
  "s4": {
    "title": "Run a plan",
    "body": "Library → open a plan → Copy prompt → paste it into your agent, opened in the project folder."
  },
  "s5": {
    "title": "Review and close",
    "body": "Review the diff and commit it yourself, set the plan's Status to Done, then run: run.ps1 close <task id>"
  }
};
