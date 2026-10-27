# xBlueTooth

[![CI](https://github.com/WizX20/xBlueTooth/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/WizX20/xBlueTooth/actions/workflows/ci.yml)

Windows moves your sound to a Bluetooth speaker or headset the moment it connects - the soundbar
in the living room, the earbuds in your bag. xBlueTooth lets you say, per device, whether Windows
may do that. A device switched off still pairs and connects; Windows just plays nothing on it.

> **Status: 0.x preview.** Lists your paired devices and switches their audio on and off.

## How it works

Windows has no "do not auto-connect" setting per device. What it does have is the device tree: a
Bluetooth audio device brings its own nodes - the audio profile driver (class `MEDIA`) and the
speaker or headset Windows lists under Sound (class `AudioEndpoint`). With those disabled, the
device can connect all it likes; there is nothing for Windows to switch your audio to.

xBlueTooth finds every paired device (Classic and Low Energy), links it to its nodes through the
container ID they share, and disables or enables the audio nodes when you flip the switch. That
is the same as `Disable-PnpDevice` on them by hand ([the approach on Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/2157521/how-to-disable-auto-connect-for-specific-or-all-bl)),
and it needs an administrator: each change shows one UAC prompt. The app itself runs as you.

To use a switched-off device again, flip it back on.

## Requirements

- Windows 10 (2004) or Windows 11, x64 or ARM64

## Build from source

```powershell
task build   # or: dotnet build src/xBlueTooth -p:Platform=x64
task run
```

It needs the .NET SDK 10 and [Task](https://taskfile.dev) (or run the `dotnet` commands directly); the
Windows App SDK comes from NuGet and ships next to the exe.
More in [DEVGUIDE.md](DEVGUIDE.md).

## License

[MIT](LICENSE).
