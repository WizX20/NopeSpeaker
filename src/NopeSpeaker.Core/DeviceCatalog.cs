namespace NopeSpeaker.Core;

/// <summary>Puts the paired devices and the device tree together.</summary>
public static class DeviceCatalog
{
    /// <summary>
    /// One <see cref="BluetoothDevice"/> per physical device, connected ones first, then by name.
    /// A device paired over both Classic and Low Energy shows up once: both endpoints share the container.
    /// </summary>
    public static IReadOnlyList<BluetoothDevice> Build(IEnumerable<PairedDevice> paired, IEnumerable<PnpNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(paired);
        ArgumentNullException.ThrowIfNull(nodes);

        var audio = nodes.Where(n => DeviceClasses.IsAudio(n.ClassGuid)).ToLookup(n => n.ContainerId);
        return [.. paired
            .Where(p => p.ContainerId != Guid.Empty)
            .GroupBy(p => p.ContainerId)
            .Select(g =>
            {
                var first = g.OrderByDescending(p => p.IsConnected).First();
                var nodesOfDevice = audio[g.Key]
                    .DistinctBy(n => n.InstanceId, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n.InstanceId, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return new BluetoothDevice(first.Name, first.Address, g.Key, g.Any(p => p.IsConnected), nodesOfDevice);
            })
            .OrderByDescending(d => d.IsConnected)
            .ThenBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase)];
    }
}
