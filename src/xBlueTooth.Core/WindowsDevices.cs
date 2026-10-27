using Windows.Devices.Enumeration;

namespace XBlueTooth.Core;

/// <summary>Reads the paired Bluetooth devices and their device-tree nodes from Windows.</summary>
public static class WindowsDevices
{
    private const string ClassicProtocol = "{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}";
    private const string LowEnergyProtocol = "{bb7bb05e-5972-42b5-94fc-76eaa7084d49}";

    /// <summary>The association endpoints of paired Bluetooth devices, Classic and Low Energy.</summary>
    public const string PairedFilter =
        $"(System.Devices.Aep.ProtocolId:=\"{ClassicProtocol}\" OR System.Devices.Aep.ProtocolId:=\"{LowEnergyProtocol}\")"
        + " AND System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True";

    private const string IsConnectedKey = "System.Devices.Aep.IsConnected";
    private const string AddressKey = "System.Devices.Aep.DeviceAddress";
    private const string AepContainerKey = "System.Devices.Aep.ContainerId";
    private const string ContainerKey = "System.Devices.ContainerId";
    // DEVPKEY_Device_ClassGuid, DEVPKEY_Device_ConfigFlags and DEVPKEY_Device_ProblemCode have no canonical names.
    private const string ClassGuidKey = "{a45c254e-df1c-4efd-8020-67d146a850e0} 10";
    private const string ConfigFlagsKey = "{a45c254e-df1c-4efd-8020-67d146a850e0} 12";
    private const string ProblemCodeKey = "{4340a6c5-93fa-4706-972c-7b648008a5a7} 3";

    private static readonly string[] PairedProperties = [IsConnectedKey, AddressKey, AepContainerKey];
    private static readonly string[] NodeProperties = [ContainerKey, ClassGuidKey, ConfigFlagsKey, ProblemCodeKey];

    /// <summary>Every paired Bluetooth device with its audio nodes.</summary>
    public static async Task<IReadOnlyList<BluetoothDevice>> GetDevicesAsync()
    {
        var paired = await GetPairedAsync().ConfigureAwait(false);
        var nodes = await Task.WhenAll(paired
            .Select(p => p.ContainerId)
            .Where(c => c != Guid.Empty)
            .Distinct()
            .Select(GetNodesAsync)).ConfigureAwait(false);

        return DeviceCatalog.Build(paired, nodes.SelectMany(n => n));
    }

    /// <summary>A watcher that reports paired devices coming, going, connecting and disconnecting.</summary>
    public static DeviceWatcher CreatePairedWatcher() =>
        DeviceInformation.CreateWatcher(PairedFilter, PairedProperties, DeviceInformationKind.AssociationEndpoint);

    private static async Task<IReadOnlyList<PairedDevice>> GetPairedAsync()
    {
        var endpoints = await DeviceInformation.FindAllAsync(PairedFilter, PairedProperties, DeviceInformationKind.AssociationEndpoint).AsTask().ConfigureAwait(false);
        return [.. endpoints.Select(e => new PairedDevice(
            e.Name,
            GetString(e, AddressKey) ?? "",
            Get<Guid>(e, AepContainerKey) ?? Guid.Empty,
            Get<bool>(e, IsConnectedKey) ?? false))];
    }

    private static async Task<IReadOnlyList<PnpNode>> GetNodesAsync(Guid container)
    {
        var filter = $"{ContainerKey}:=\"{{{container}}}\"";
        var devices = await DeviceInformation.FindAllAsync(filter, NodeProperties, DeviceInformationKind.Device).AsTask().ConfigureAwait(false);
        return [.. devices.Select(d => new PnpNode(
            d.Id,
            d.Name,
            Get<Guid>(d, ClassGuidKey) ?? Guid.Empty,
            Get<Guid>(d, ContainerKey) ?? container,
            Get<uint>(d, ProblemCodeKey) ?? 0,
            Get<uint>(d, ConfigFlagsKey) ?? 0))];
    }

    private static T? Get<T>(DeviceInformation info, string key) where T : struct =>
        info.Properties.TryGetValue(key, out var value) && value is T typed ? typed : null;

    private static string? GetString(DeviceInformation info, string key) =>
        info.Properties.TryGetValue(key, out var value) ? value as string : null;
}
