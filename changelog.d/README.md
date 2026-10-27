# Changelog fragments

A pull request with a user-visible change adds **one file here** instead of editing
`CHANGELOG.md`. Each pull request has its own file, so no pull request ever conflicts with
another over the changelog. The release folds the files into `CHANGELOG.md` under the new
version and deletes them.

**Name:** `<name>.<section>.md` — the branch name without its `feature/`, `fix/` or `chore/` prefix works well: `tray-icon.added.md`.
The section is one of Keep a Changelog's:

| Section | For |
|---|---|
| `added` | new features, options |
| `changed` | changes in existing behaviour, docs |
| `deprecated` | soon-to-be removed features |
| `removed` | removed features |
| `fixed` | bug fixes |
| `security` | vulnerabilities |

**Content:** one or more bullets, written like the entries in `CHANGELOG.md`:

```markdown
- fix: devices - a device paired over Classic and LE shows once
```

Two kinds of change in one pull request: two files (`x.added.md`, `x.fixed.md`).
