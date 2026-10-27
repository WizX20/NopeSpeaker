<p align="center">
  <a href="https://github.com/WizX20">
    <picture>
      <source media="(prefers-color-scheme: dark)" srcset="docs/wizx20.png">
      <img src="docs/wizx20-transparent.png" alt="WizX20" height="140">
    </picture>
  </a>
</p>

# NopeSpeaker

[![CI](https://github.com/WizX20/NopeSpeaker/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/WizX20/NopeSpeaker/actions/workflows/ci.yml)

<p align="center">
  <img src="docs/nopespeaker-logo.png" alt="NopeSpeaker logo: a speaker struck through by a lightning bolt" width="420">
</p>

> Your speaker connects. Windows wants to play on it. **Nope.**

Windows moves your sound to a Bluetooth speaker or headset the moment it connects - the soundbar
in the living room, the earbuds in your bag. NopeSpeaker lets you say, per device, whether Windows
may do that. A device switched off still pairs and connects; Windows just plays nothing on it.

> **Status: 0.x preview.** Lists your paired devices and switches their audio on and off.

## How it works

Windows has no "do not auto-connect" setting per device. What it does have is the device tree: a
Bluetooth audio device brings its own nodes - the audio profile driver (class `MEDIA`) and the
speaker or headset Windows lists under Sound (class `AudioEndpoint`). With those disabled, the
device can connect all it likes; there is nothing for Windows to switch your audio to.

NopeSpeaker finds every paired device (Classic and Low Energy), links it to its nodes through the
container ID they share, and disables or enables the audio nodes when you flip the switch. That
is the same as `Disable-PnpDevice` on them by hand ([the approach on Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/2157521/how-to-disable-auto-connect-for-specific-or-all-bl)),
and it needs an administrator: each change shows one UAC prompt. The app itself runs as you.

To use a switched-off device again, flip it back on.

## Requirements

- Windows 10 (2004) or Windows 11, x64 or ARM64

## Install

**Scoop:**

```powershell
scoop bucket add nopespeaker https://github.com/WizX20/NopeSpeaker
scoop install nopespeaker
```

NopeSpeaker is then in the Start menu (Scoop Apps); `scoop update nopespeaker` gets new versions.

**winget:**

```powershell
winget install WizX20.NopeSpeaker
```

Start it with `nopespeaker` from the Run dialog or a terminal. (Each version is reviewed by the
winget moderators first, so a new release can take a few days to show up there.)

**Zip:** download `NopeSpeaker-<version>-win-x64.zip` (or `-win-arm64.zip`) from the
[latest release](https://github.com/WizX20/NopeSpeaker/releases/latest), unzip it anywhere and run
`NopeSpeaker.exe`. Nothing to install: the Windows App SDK ships inside.

The app is not code-signed yet, so Windows SmartScreen may warn on first start ("More info" →
"Run anyway"). A Microsoft Store version is planned ([#23](https://github.com/WizX20/NopeSpeaker/issues/23)).

## Build from source

```powershell
task build   # or: dotnet build src/NopeSpeaker -p:Platform=x64
task run
```

It needs the .NET SDK 10 and [Task](https://taskfile.dev) (or run the `dotnet` commands directly); the
Windows App SDK comes from NuGet and ships next to the exe.
More in [DEVGUIDE.md](DEVGUIDE.md).

## License

[MIT](LICENSE).
