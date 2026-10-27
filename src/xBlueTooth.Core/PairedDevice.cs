namespace XBlueTooth.Core;

/// <summary>A paired Bluetooth device as Windows' association endpoints report it.</summary>
/// <param name="Name">The device name.</param>
/// <param name="Address">The Bluetooth address, <c>00:11:22:33:44:55</c>.</param>
/// <param name="ContainerId">The physical device; links it to its nodes in the device tree.</param>
/// <param name="IsConnected">Whether it is connected right now.</param>
public sealed record PairedDevice(string Name, string Address, Guid ContainerId, bool IsConnected);
