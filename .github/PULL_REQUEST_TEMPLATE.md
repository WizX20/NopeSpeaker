<!--
Thanks for the PR! Fill in the sections below — the checklist at the bottom catches the things that bounce most often in review.
-->

## What

<!-- One or two sentences on the change. -->

## Why

<!-- The user-facing problem or use case. Link the issue if there is one: `Fixes #123`. -->

## How it works

<!-- Brief technical note on the approach if non-obvious. Skip for trivial changes. -->

## Testing

<!-- How you verified this, and on which device(s). -->

- [ ] `task check` passes locally
- [ ] Logic in `NopeSpeaker.Core` has an xUnit test; no test touches a real device
- [ ] Tried in the app (screenshot for UI changes)

## Checklist

- [ ] A changelog fragment in `changelog.d/` (user-visible changes only; see `changelog.d/README.md`) - not an edit to `CHANGELOG.md`.
- [ ] `docs/design.md` updated if the change deviates from it.
- [ ] Commits follow the conventions in [CONTRIBUTING.md](https://github.com/WizX20/NopeSpeaker/blob/main/CONTRIBUTING.md) (imperative subject ≤72 chars, new commits not amends, hooks not skipped).
