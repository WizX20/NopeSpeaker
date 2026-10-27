# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

**xBlueTooth** — a WinUI 3 app (Windows App SDK, unpackaged, self-contained) that lists the paired Bluetooth devices and switches, per device, whether Windows may play audio on it: it disables or enables the device's `MEDIA` and `AudioEndpoint` nodes through an elevated `Disable-PnpDevice`/`Enable-PnpDevice`. Design: [docs/design.md](docs/design.md). Layout, build and tests: [DEVGUIDE.md](DEVGUIDE.md). Conventions: [CONTRIBUTING.md](CONTRIBUTING.md).

## GitHub account — always WizX20

This repo is published under the **WizX20** account from a machine where another `gh` account may be the active one. Never run `gh auth switch`. Inside this clone:

- `git push` / `git fetch` already authenticate as WizX20 through [`.gitconfig`](.gitconfig), copied into `.git/` by `task setup` (once per clone and after it changes — check with `git config user.name`, it must print `WizX20`).
- Use **`git gh …`** (or `task gh -- …`) instead of `gh …` for PRs, releases, workflow runs, API calls. Plain `gh` acts as the wrong account.
- Commits must be authored as `WizX20 <nerdsonwaves@outlook.com>`; if `git config user.email` shows anything else, run `task setup` before committing.

## Commands

```powershell
task check   # build + tests — run before every push
task build   # core + app (x64)
task test    # the core's xUnit tests
task run     # build and start the app
task pack    # dist/xBlueTooth-<version>-win-<arch>.zip
```

## Rules

- **Core vs app**: everything about devices lives in `src/xBlueTooth.Core` (no UI, no package dependencies); `src/xBlueTooth` is only the window. Pure logic (grouping, state, the PowerShell it runs) gets xUnit tests; FluentAssertions stays on **7.x** — 8 is commercial.
- **Never touch a real device from a test.** Tests build `PairedDevice`/`PnpNode` values by hand; nothing calls `WindowsDevices` or `ElevatedPnp`.
- **The app runs as the user.** Only the change itself elevates, one UAC prompt per change; never `requireAdministrator` in the manifest.
- **Instance IDs go into PowerShell single-quoted** (`PnpScript` accepts only valid instance IDs and doubles every kind of single quote) and the script travels as `-EncodedCommand`; never splice an ID into a command line.
- **Changelog**: a user-visible change adds a fragment `changelog.d/<branch-without-prefix>.<section>.md` (see `changelog.d/README.md`) — never edit `CHANGELOG.md` in a PR.
- **Commits**: imperative subject ≤72 chars, new commits (no amend), no `--no-verify`. Branches `feature/…`, `fix/…`, `chore/…` off `main`.
- **Do not commit or push without asking**; never push to `main` directly — open a PR.
