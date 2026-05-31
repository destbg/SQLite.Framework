# AI Assistance

This page is for people who use an AI coding agent (Claude Code, Cursor, Copilot, Opencode, and others like them) to write code that uses `SQLite.Framework`. The framework is small but its surface differs from Entity Framework Core in a few places that AI agents might get wrong (no navigation properties, `[AutoIncrement]` is a separate attribute, schema lives on `db.Schema`, and so on). Giving the agent a short reference up front saves you from correcting it over and over.

## The reference to use

Give the agent the [Overview](Overview) page. It is a single self-contained page that covers package selection, options builder, model definition, CRUD, querying, joins, subqueries, grouping, supported expressions, transactions, raw SQL, hooks, multi-threading, AOT setup, and the common pitfalls. It is sized so an agent can load the whole thing into its context without crowding out your actual task.

You can grab the raw markdown from the repository:

- [wiki/Overview.md](https://github.com/destbg/SQLite.Framework/blob/main/wiki/Overview.md)

## Drop it into your project

Copy the Overview text to the root of the project (or solution) where the agent runs. Most agents pick up project-level markdown automatically:

| Agent | File the agent reads |
|---|---|
| Claude Code | `CLAUDE.md` |
| Cursor | `.cursorrules` or `.cursor/rules/*.mdc` |
| GitHub Copilot Chat | `.github/copilot-instructions.md` |
| Other / generic | `AGENTS.md` |

If your agent does not auto-load a known filename, save the Overview text under whatever name the agent reads, or point at it explicitly from your existing instructions file:

```markdown
When working with SQLite or the database layer, follow the SQLite.Framework Overview.
```

You can also keep the Overview in its own file and reference that file from `CLAUDE.md` / `AGENTS.md` / `.cursorrules`. The agent will read both.

## Updating the reference

`SQLite.Framework` is small and changes are localised, but new features do land. When you bump to a new major or minor version, grab the latest `Overview.md` from the repository linked above and refresh your local copy.
