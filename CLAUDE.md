# CEM_RevitMCP

A **fork** of `mcp-servers-for-revit` — an MCP server exposing Revit to an AI agent over a localhost
socket. Two projects: a TypeScript MCP server (`server/`) and a C# Revit command set
(`commandset/` + `plugin/`).

**[CEM_RevitMCP.md](CEM_RevitMCP.md) is the authoritative architecture and conventions document.**
Read it before making changes — it covers domains, folder structure, canonical tool templates, the
full toolset, build order, and the smoke test. (`README.md` covers install.)

**Before writing code here, load the `cem-revitmcp-dev` skill**
(`.claude/skills/cem-revitmcp-dev/`) — it is a thin workflow layer over that document, not a
replacement for it.

Cross-repo rules: [../../CROSS_REPO.md](../../CROSS_REPO.md).

The three facts that break builds if missed:

1. A tool is **four files plus a `command.json` entry**: `server/src/tools/<name>.ts`,
   `commandset/Commands/<Domain>/`, `commandset/Services/<Domain>/`, `commandset/Models/<Domain>/`.
2. **Three names must match byte-for-byte:** `server.tool("x")`, `CommandName => "x"`, and
   `command.json`'s `commandName`. Runtime resolves by string only, so a mismatch fails silently.
3. **The Command must never call the Revit API directly.** The socket runs on a background thread;
   only the EventHandler runs on the UI thread and owns the `Transaction`.

Also:

- TS tools are **auto-discovered** (`register.ts` imports every file and calls any `register*`
  export) — no list to edit. The C# side is the opposite: `command.json` is hand-maintained and is
  the source of truth for what exists (`grep -c '"commandName"' command.json`).
- Folder name == namespace suffix == the `domain_` tool prefix.
- Conventions that make tools AI-grade: mm in/mm out, `AIResult<T>` with a human-readable `Message`,
  never hard-fail a batch (skip + warn + continue), accept display names *and* enum names.
- When a batch lands, update `CEM_RevitMCP.md` sections 5 and 7.

Verify: `cd server && npm run build`, build the command set, then run the smoke test in
`CEM_RevitMCP.md` section 8.

Commit: `FEAT|FIX|REFACTOR - Description. <issue URL>` — **that line is the entire message**:
no body, no file list, no test tally, no `Co-Authored-By`. One `git commit -m "..."`.
