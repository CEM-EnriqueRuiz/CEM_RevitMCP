# CEM_RevitMCP — agent instructions

A fork of `mcp-servers-for-revit`: a TypeScript MCP server (`server/`) that drives Revit through a
C# plugin (`plugin/`) and command set (`commandset/`) over a localhost socket.

1. **Read [docs/index.md](docs/index.md) ("Start here") first**, then the pages it names. The wiki
   in `docs/` holds the conventions, the decisions and their reasons, and the workflows.
   `CEM_RevitMCP.md` is legacy and partly stale; the code and the wiki win.
2. Then follow the canonical skill
   [.claude/skills/cem-revitmcp-dev/SKILL.md](.claude/skills/cem-revitmcp-dev/SKILL.md) (the hard
   rules and verify commands) and the cross-repo rules in [../../CROSS_REPO.md](../../CROSS_REPO.md).
   To **use** the MCP on a live model rather than change it (modelling, replacing elements, the
   Cemengal add-ins), follow [.claude/skills/cem-aimodeler/SKILL.md](.claude/skills/cem-aimodeler/SKILL.md)
   instead.
3. Search the wiki with the qmd CLI when the index is not enough:
   `qmd query -c cem_revitmcp "why does the command not call the Revit API"`, then
   `qmd get qmd://cem_revitmcp/<path>`. Collection names for other repos are in
   [../../tools/wiki/README.md](../../tools/wiki/README.md).
4. **Every commit carries its wiki update, in the same commit:** the source page
   (`docs/sources/issues/<org>-<repo>-<N>.md`, or `docs/sources/commits/<YYYY-MM>.md`) with every
   decision taken, its reason and the rejected alternatives; the affected pages; `docs/index.md`;
   and a `docs/log.md` entry. `python ../../tools/wiki/wiki_lint.py .` must report 0 errors. Commit
   only after the user supplies the issue URL, with the single line
   `FEAT|FIX|REFACTOR - Description. <issue URL>` (no body). The git `pre-commit` hook enforces
   the wiki update.
