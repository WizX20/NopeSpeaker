# Contributing

Thanks for taking the time to contribute to xBlueTooth.

For building from source and the layout, see [DEVGUIDE.md](DEVGUIDE.md); for how it works, [docs/design.md](docs/design.md).

By participating in this project you agree to abide by the [Code of Conduct](CODE_OF_CONDUCT.md).

## Reporting bugs

Open a [GitHub issue](https://github.com/WizX20/xBlueTooth/issues/new/choose) with:

- What you did (numbered steps) and which device (make and model)
- What you expected and what happened
- Your Windows version and architecture
- For a device that shows wrong, the output of (device names only, nothing secret):

  ```powershell
  Get-PnpDevice | ? FriendlyName -like '*<device>*' | Select Status, Class, FriendlyName, InstanceId
  ```

## Pull requests

1. Open an issue first for anything bigger than a small fix.
2. Branch off `main`: `feature/…`, `fix/…` or `chore/…`.
3. `task check` passes locally.
4. A user-visible change adds a fragment in [changelog.d/](changelog.d/README.md) - not an edit to `CHANGELOG.md`.
5. A change that deviates from `docs/design.md` updates it in the same PR.

## Commits

- Imperative subject, 72 characters at most: `Show the audio state per device`.
- New commits rather than amends once a PR is under review; never `--no-verify`.
