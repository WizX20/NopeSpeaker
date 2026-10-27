# Developer guide

## Layout

```
src/xBlueTooth.Core/    devices, no UI: WindowsDevices (read), ElevatedPnp (change), DeviceCatalog, PnpScript
src/xBlueTooth/         the WinUI 3 app: MainWindow, DeviceItem
tests/xBlueTooth.Core.Tests/   xUnit v3 + FluentAssertions 7, pure logic only
docs/design.md          how and why
changelog.d/            changelog fragments, one per pull request
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
task pack    # dist/xBlueTooth-<version>-win-x64.zip
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

Not automated yet (see the roadmap in docs/design.md): `task pack`, then attach the zip to a
GitHub Release with `git gh release create`. Fold `changelog.d/` into `CHANGELOG.md` in the
release commit.
