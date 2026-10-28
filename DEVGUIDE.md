# Developer guide

## Layout

```
src/NopeSpeaker.Core/    devices, no UI: WindowsDevices (read), ElevatedPnp (change), DeviceCatalog, PnpScript
src/NopeSpeaker/         the WinUI 3 app: MainWindow, DeviceItem
tests/NopeSpeaker.Core.Tests/   xUnit v3 + FluentAssertions 7, pure logic only
docs/design.md          how and why
changelog.d/            changelog fragments, one per pull request
scripts/                pack.ps1 (zip), set-version.ps1 + cut-changelog.ps1 (release)
bucket/nopespeaker.json Scoop manifest, bumped by the release
winget/                 winget manifest templates, filled in and submitted by the release
.github/workflows/      ci.yml (tests, pack, release token expiry; also weekly), release.yml
```

## Build and run

Needs the .NET SDK 10 (`global.json`) and [Task](https://taskfile.dev). The Windows App SDK and the
Community Toolkit settings controls come from NuGet.

```powershell
task build   # core + app, x64 Debug
task run     # build and start
task test    # the core's tests
task coverage  # the tests with coverage: dist/core-coverage.cobertura.xml
task check   # Release build + tests, before every push
task pack    # dist/NopeSpeaker-<version>-win-x64.zip
```

The app is unpackaged (`WindowsPackageType=None`) and self-contained
(`WindowsAppSDKSelfContained=true`): the output folder runs on its own, no runtime installer. It
builds per platform: pass `-p:Platform=x64` or `-p:Platform=ARM64` to `dotnet build`.

## Trying a change on a real device

Switching a device needs an administrator (a UAC prompt) and really disables its audio nodes.
Switch it back on in the app, or by hand:

```powershell
Get-PnpDevice | ? { $_.FriendlyName -like '*<device>*' -and $_.Class -in 'AudioEndpoint','MEDIA' } |
    % { Enable-PnpDevice -InstanceId $_.InstanceId -Confirm:$false }   # as administrator
```

## Release

Releases are cut by `.github/workflows/release.yml`. It runs **once a week, Tuesday 06:00 UTC**,
and on manual dispatch - nothing else triggers it:

```powershell
task release                  # release now: next patch version (or the props version if that was never released)
task release VERSION=0.2.0    # release now with an explicit version
```

The `check` job:

1. **Anything to release?** If `main` is exactly the commit of the latest `v*` tag, stop quietly - unless that tag has no published GitHub Release: then fail with the command that publishes its draft (see below).
2. **Which version?** The dispatch input if given; else `<Version>` in `Directory.Build.props` when no tag for it exists yet (first release, or a bump made in a PR); else the next patch of it. For a **minor/major** bump, raise `<Version>` in your PR - the next release ships exactly that.
3. **Release token** - the `NOPESPEAKER_RELEASE_TOKEN` secret must exist.
4. **Gate on CI** - the CI run of the exact commit being released must be `success` (it waits up to 20 minutes for a run still going; no run after five minutes refuses the release).

Then the `release` job, on the commit step 4 verified - not whatever `main` is by then:

5. **Stamp** - `scripts/set-version.ps1` writes `<Version>`; `scripts/cut-changelog.ps1 -FallbackFromGit` folds the `changelog.d/` fragments (and any lines under `## [Unreleased]`) into `## [x.y.z] - <date>`, deletes the fragments and extracts that section as the release notes. With no entries at all it uses the commit subjects since the last tag.
6. **Test** the core, **pack** `NopeSpeaker-x.y.z-win-x64.zip` and `-win-arm64.zip`.
7. **Scoop** - `bucket/nopespeaker.json` gets the version and both zips' URLs and SHA256.
8. **Commit** `chore: release vx.y.z` (as `github-actions[bot]`).
9. **Draft the GitHub Release** `vx.y.z` with both zips and the changelog section - before anything reaches `main`.
10. **Tag + push** - the commit and the tag go to `main` with the release token, atomically. When `main` moved meanwhile the push is refused, the draft is deleted and nothing is published; run the release again.
11. **Publish** the draft as the latest release (Scoop's `checkver` follows `releases/latest`).

Then the `winget` job (dispatch input `winget`, on by default and in the weekly run):

12. **winget** - fills in the templates in `winget/` (version, date, both hashes) and submits them to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) with `wingetcreate submit`, as a pull request from the `WINGET_PAT` account's fork. The winget moderators review it; it shows in `winget search` once merged. Without the secret the job warns and the release stands.

If step 11 fails after step 10 pushed, `main`'s manifest points at zips nobody can download yet. Publish the draft by hand: `git gh release edit vx.y.z --draft=false --latest`. Do **not** re-pack and upload zips from the tag: a rebuilt zip has another SHA256 than the pushed manifest, and every `scoop install` would fail on it. If only the `winget` job failed, use **Re-run failed jobs** on that run: the release stands and the job resubmits the same version.

### Secrets

| Secret | What | Used by |
|---|---|---|
| `NOPESPEAKER_RELEASE_TOKEN` | fine-grained PAT of WizX20: repository `NopeSpeaker`, **Contents: Read and write** | step 10 - the only actor that may bypass the pull-request rule on `main` (`GITHUB_TOKEN` cannot on a user-owned repo) |
| `WINGET_PAT` | classic PAT with `public_repo` | step 12 - forks `microsoft/winget-pkgs` and opens the pull request |

```powershell
Get-Clipboard | git gh secret set NOPESPEAKER_RELEASE_TOKEN -R WizX20/NopeSpeaker
Get-Clipboard | git gh secret set WINGET_PAT -R WizX20/NopeSpeaker
```

Only the push step sees the release token: both checkouts persist no credentials, so the build, the tests and the NuGet packages they restore never run next to a token that may bypass the ruleset. CI's required **release token expiry** job reads the token's real expiry from the API on every PR and push, and weekly on Mondays 05:00 UTC: a warning 30 days out, a failure 14 days out - an expiring token blocks merges until it is rotated. The dated rotation issue ([#27](https://github.com/WizX20/NopeSpeaker/issues/27)) has the checklist; after rotating, close it and open the next one with the new dates.

### Ruleset

**Settings → Rules → Rulesets → main**: pull request required, `squash` the only merge method, required checks `test (core)`, `pack (x64)`, `pack (ARM64)` and `release token expiry`, deletion and force-push blocked; bypass list: repository admin only.

### Scoop

- Manifest: `bucket/nopespeaker.json`, one zip per architecture (`64bit`, `arm64`). The release workflow bumps `version` and both `url`/`hash` pairs; `checkver: github` + `autoupdate` let `scoop update` find new releases. Until the first release the hashes are placeholders (all zeros).
- Users add the bucket straight from this repo: `scoop bucket add nopespeaker https://github.com/WizX20/NopeSpeaker`. The repo must stay **public**: Scoop downloads release assets anonymously.

### Microsoft Store

Not yet: MSIX packaging and the Store listing are tracked in [#23](https://github.com/WizX20/NopeSpeaker/issues/23).
