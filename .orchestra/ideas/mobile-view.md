# Mobile / PWA view

**Status:** raw · **Added:** 2026-11-19 · **Plan:** —

The runs board on the phone: which runs are running, which wait for the owner, which failed.

How it could work without a server (owner, 2026-11-24: "no server"): the app stays on the Windows machine
([plan 0012](../plans/0012-local-app-and-project-setup.md)), and a private tunnel (Tailscale, or Cloudflare
Tunnel with access rules) lets the phone open the same page. The UI is already a web page; it needs a
narrow layout for the board and nothing else.

What it would need first:
- **Sign-in.** Once the page is reachable from anything but loopback, the setup routes, the folder browser
  and the token file must not be open. ADR 0009 assumes loopback; reaching it remotely needs its own ADR,
  and that ADR takes over what the dropped plan 0008 had (sign-in, HTTPS through the tunnel).
- **Read-only from the phone.** Setup stays a loopback-only action even after sign-in.

Related: [hosting](hosting.md), [notifications](notifications.md), [remote answers](remote-answers.md).
