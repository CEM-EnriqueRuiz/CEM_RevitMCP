# CEM_RevitMCP

A fork of `mcp-servers-for-revit`: a TypeScript MCP server (`server/`) that drives Revit through a
C# plugin (`plugin/`) and command set (`commandset/`) over a localhost socket.

**Read [docs/index.md](docs/index.md) ("Start here") before planning or editing**, then the pages it
names. The wiki holds the conventions, the decisions and their reasons, and the workflows.
`CEM_RevitMCP.md` is legacy and partly stale; the code and the wiki win.

Load the `cem-revitmcp-dev` skill (`.claude/skills/cem-revitmcp-dev/SKILL.md`) before writing code.
To **use** the MCP on a live model (modelling, replacing elements, running or opening the Cemengal
add-ins through it), load the `cem-aimodeler` skill (`.claude/skills/cem-aimodeler/SKILL.md`) instead;
it is the only copy of that skill. Cross-repo rules: [../../CROSS_REPO.md](../../CROSS_REPO.md).

Every commit carries its wiki update ([docs/workflows/finish-a-change.md](docs/workflows/finish-a-change.md)).
Commit message: `FEAT|FIX|REFACTOR - Description. <issue URL>`. **That line is the entire message**:
no body, no file list, no test tally, no `Co-Authored-By`. Use one `git commit -m "..."`.
