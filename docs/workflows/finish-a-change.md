---
title: Finish a change (commit-time wiki ingest)
type: workflow
updated: 2026-10-06
sources: [CLAUDE.md, .claude/settings.json]
related: [../decisions/0020-wiki-is-the-knowledge-base.md, ../log.md, ../index.md]
tags: [process, wiki, commit]
---

# Finish a change (commit-time wiki ingest)

Rules: [CROSS_REPO.md §4 and §9](../../../../CROSS_REPO.md). Tooling:
[tools/wiki](../../../../tools/wiki/README.md).

## Sequence

1. Implement and verify (`npx vitest run`, `npm run build`, C# build, smoke test). **Don't commit
   yet.**
2. The user tests and then sends the **issue URL**. That is the go-ahead to commit. Work on `main`;
   don't branch or push unless asked.
3. **Ingest the change into `docs/`** in the same commit:
   - Source page: `docs/sources/issues/<org>-<repo>-<N>.md` for the issue (create it if it is new;
     cross-repo umbrella issues are normal), or `docs/sources/commits/<YYYY-MM>.md` if there is no
     issue. Record what changed and **every decision taken while implementing, with its reason and
     the alternatives rejected**.
   - Update the affected `modules/`, `concepts/`, `workflows/` and `decisions/` pages. New tools go
     on [toolset](../modules/toolset.md). If a decision reverses an older one, mark the old one
     `superseded` and link the new one.
   - Add any new page to [index.md](../index.md).
   - Append to [log.md](../log.md): `## [YYYY-MM-DD] ingest | <org>/<repo>#<N> <title>`.
4. Lint: `python ../../tools/wiki/wiki_lint.py .` must show 0 errors.
5. Stage explicit paths (code + `docs/`), never `git add -A`. Remember `.claude/` is gitignored
   (`git add -f` for files there).
6. Commit with **one line**: `git commit -m "FEAT|FIX|REFACTOR - Description. <issue URL>"`. No
   body and no trailers. `DOCS - …` is only for wiki-only commits.

The `pre-commit` hook rejects staged code without staged `docs/log.md` plus another `docs/` page. A
commit that slips through is auto-ingested by the `post-commit` backstop as
`DOCS - Wiki ingest <sha>. <url>`.
