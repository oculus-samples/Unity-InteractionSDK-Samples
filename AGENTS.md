# Agent Instructions — Unity Interaction SDK Samples

External sample set for the Meta XR Interaction SDK (ISDK), pairing the package's standard example scenes with additional showcase scenes that demonstrate more advanced interaction patterns.

## Source-of-truth files (read these first, do not duplicate their contents in this file)

For setup, build steps, SDK versions, and project layout, read:

- `README.md` — official setup, two install workflows (per-release `.unitypackage` vs full clone), and version-matching rules
- `ProjectSettings/ProjectVersion.txt` — Unity editor version
- `Packages/manifest.json` — Unity package versions, including the ISDK package
- `Assets/Samples/Meta XR Interaction SDK/<version>/` — the in-tree ISDK example scenes (folder name encodes the ISDK version)
- `.gitattributes` — Git LFS configuration if present
- `LICENSE.txt` — license terms (Oculus SDK License, not MIT)

## Quest / Horizon-specific notes

- ISDK version-match is enforced: a sample release tagged `v74` requires a `v74` ISDK install. When asked to update, bump the in-project ISDK package AND verify a matching release tag exists on this repo. New ISDK releases land as a sibling folder under `Assets/Samples/Meta XR Interaction SDK/<new-version>/`, not as in-place rewrites.
- Add new work in `Assets/ShowcaseSamples/`. Files under `Assets/Samples/Meta XR Interaction SDK/<version>/` are the package's own sample drop and will be overwritten on a package re-import.
- License is the Oculus SDK License (not MIT) — do not relicense or copy code wholesale into MIT-licensed projects without confirming the terms.

## Meta Quest tooling

This repository is part of the Meta Quest / Horizon OS ecosystem (a sample, library, template, or related project — the bespoke intro above describes which). Use that intro and the source-of-truth files it references for project-specific decisions; don't restate or invent facts from memory.

When the user asks anything about Quest device behavior, build / deploy / debug / capture flows, on-device performance, or Horizon OS APIs, reach for these tools instead of generic Unity answers:

- **`hzdb`** — Quest-aware ADB wrapper (device list, install / launch / stop, logs, screenshots, Perfetto traces, on-device docs search). Already wired up as an MCP server via `.mcp.json`, `.vscode/mcp.json`, and `.cursor/mcp.json`. Also runnable directly: `npx -y @meta-quest/hzdb <subcommand>`.
- **Meta Quest Agentic Tools** — the full skill set, including Unity-specific skills: [github.com/meta-quest/agentic-tools](https://github.com/meta-quest/agentic-tools). Install per your client (Claude Code: `/plugin install meta-vr@meta-quest`; Gemini CLI: `gemini extensions install https://github.com/meta-quest/agentic-tools`; Cursor / VS Code: install the **Meta Horizon** extension from the Marketplace).

A few behavior expectations:

- **Read this repo's files first.** Before answering anything project-specific, read `README.md` and whichever source-of-truth files the intro above points at. Don't restate their contents in chat — quote or link instead.
- **Use `hzdb` for device-side work.** Anything that touches an attached Quest (install, launch, logs, screenshot, capture, manifest inspection) goes through `hzdb`, not raw `adb`.
- **Check live Horizon OS docs before answering API questions.** `hzdb docs search "..."` queries the live docs; training data on Horizon OS APIs goes stale fast.
- **Don't fabricate SDK / engine versions.** If a version isn't visible in this repo's files, say so rather than guessing.
