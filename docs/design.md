# NopeSpeaker design

## Problem

Windows connects a paired Bluetooth audio device as soon as it is in range and moves the default
audio output to it. There is no per-device setting to stop that. Unpairing works, but then the
device is gone for the times you do want it.

## Approach

A Bluetooth audio device has nodes in the Plug and Play tree next to its Bluetooth node:

| Class | What it is | Example |
|---|---|---|
| `MEDIA` `{4d36e96c-…}` | the audio profile driver (A2DP sink, hands-free) | `BTHENUM\{0000110B-…}_LOCALMFG&0002\…` |
| `AudioEndpoint` `{c166523c-…}` | the speaker/headset under Sound | `SWD\MMDEVAPI\{0.0.0.00000000}.{…}` |

All nodes of one physical device share a **container ID**. Disabling the `MEDIA` and
`AudioEndpoint` nodes leaves pairing and the connection alone but gives Windows nothing to play
on. Enabling them restores the device as it was.

### Finding devices

1. Paired devices: association endpoints (`DeviceInformationKind.AssociationEndpoint`) with the
   Bluetooth Classic or LE protocol ID and `IsPaired`, asking for `IsConnected`, `DeviceAddress`
   and `ContainerId`. A device paired over both protocols shows twice and is merged by container.
2. Per container: the device nodes (`DeviceInformationKind.Device`) with their class GUID, problem
   code and config flags. Problem code 22 (`CM_PROB_DISABLED`) or the stored `CONFIGFLAG_DISABLED`
   means switched off; a node that is not present (a disconnected device) has only the flag.
3. `DeviceCatalog.Build` joins the two; `AudioState` is Enabled, Disabled, Partial or None.

A `DeviceWatcher` over the paired endpoints refreshes the list when a device connects,
disconnects, pairs or unpairs (debounced).

### Changing a device

`Disable-PnpDevice`/`Enable-PnpDevice` need an administrator. The app runs as the user and starts
Windows PowerShell elevated (`runas`, one UAC prompt) with the script as `-EncodedCommand`; the
instance IDs (validated: printable ASCII, no comma) sit in single-quoted strings, and the
`PnpDevice` module is loaded from `$PSHOME`, not the user's module path. The script goes on past a
node that fails and writes its errors to a temp file the app shows; the result is judged by the
state the device ends up in. One change runs at a time. A declined prompt reverts the switch.

## Roadmap

- Tray icon: switch audio devices from the notification area without opening the window.
- A PowerToys Command Palette extension on top of `NopeSpeaker.Core`.
- Connect/disconnect a device from the list.
- Code signing for the zips, and a Microsoft Store (MSIX) version (#23). The releases themselves -
  GitHub Releases, Scoop, winget - run weekly ([DEVGUIDE.md → Release](../DEVGUIDE.md#release)).
