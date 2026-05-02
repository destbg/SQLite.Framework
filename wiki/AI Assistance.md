# AI Assistance

This page is for people who use an AI coding agent (Claude Code, Cursor, Copilot, Opencode, and others like them) to write code that uses `SQLite.Framework`. The framework is small but its surface differs from Entity Framework Core in a few places that AI agents might get wrong (no navigation properties, `[AutoIncrement]` is a separate attribute, schema lives on `db.Schema`, and so on). Giving the agent a short cheat sheet up front saves you from correcting it over and over.

## The cheat sheet file

There is a ready-made reference at:

- [Sample/SQLite.Framework.Maui/SQLITE_FRAMEWORK.md](https://github.com/destbg/SQLite.Framework/blob/main/Sample/SQLite.Framework.Maui/SQLITE_FRAMEWORK.md)
- [Sample/SQLite.Framework.Avalonia/SQLITE_FRAMEWORK.md](https://github.com/destbg/SQLite.Framework/blob/main/Sample/SQLite.Framework.Avalonia/SQLITE_FRAMEWORK.md)

The two copies are identical. It is a single self-contained markdown file that covers package selection, options builder, model definition, CRUD, querying, joins, subqueries, grouping, supported expressions, transactions, raw SQL, hooks, multi-threading, AOT setup, and the common pitfalls. It is sized so an agent can load the whole thing into its context without crowding out your actual task.

## Drop it into your project

Copy the file to the root of the project (or solution) where the agent runs. Most agents pick up project-level markdown automatically:

| Agent | File the agent reads |
|---|---|
| Claude Code | `CLAUDE.md` |
| Cursor | `.cursorrules` or `.cursor/rules/*.mdc` |
| GitHub Copilot Chat | `.github/copilot-instructions.md` |
| Other / generic | `AGENTS.md` |

If your agent does not auto-load a known filename, either rename `SQLITE_FRAMEWORK.md` to whatever the agent reads, or point at it explicitly from your existing instructions file:

```markdown
When working with SQLite or the database layer, follow `SQLITE_FRAMEWORK.md`.
```

You can also keep the cheat sheet as `SQLITE_FRAMEWORK.md` and reference it from `CLAUDE.md` / `AGENTS.md` / `.cursorrules`. The agent will read both.

## Updating the file

`SQLite.Framework` is small and changes are localised, but new features do land. When you bump to a new major or minor version, grab the latest `SQLITE_FRAMEWORK.md` from the sample folder linked above and overwrite your local copy.
