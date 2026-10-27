namespace XBlueTooth.Core;

/// <summary>A node in the Plug and Play device tree, as much of it as xBlueTooth needs.</summary>
/// <param name="InstanceId">The device instance ID, e.g. <c>BTHENUM\{0000110B-...}_LOCALMFG&amp;0002\7&amp;...</c>.</param>
/// <param name="Name">The friendly name Device Manager shows.</param>
/// <param name="ClassGuid">The device setup class.</param>
/// <param name="ContainerId">The physical device the node belongs to; every node of one device shares it.</param>
/// <param name="ProblemCode">The Configuration Manager problem code; 0 when the node is fine or not present.</param>
/// <param name="ConfigFlags">The node's stored configuration flags; kept while the node is not present.</param>
public sealed record PnpNode(string InstanceId, string Name, Guid ClassGuid, Guid ContainerId, uint ProblemCode, uint ConfigFlags = 0)
{
    /// <summary>CM_PROB_DISABLED: switched off in Device Manager, by pnputil or by Disable-PnpDevice.</summary>
    public const uint ProblemDisabled = 22;

    /// <summary>CONFIGFLAG_DISABLED: the stored "switched off" - also there when the node is not present.</summary>
    public const uint ConfigFlagDisabled = 0x1;

    /// <summary>
    /// Whether the node is switched off. A node that is not present (a disconnected device, or an
    /// endpoint whose driver is off) has no problem code, so the stored flag counts too.
    /// </summary>
    public bool IsDisabled => ProblemCode == ProblemDisabled || (ConfigFlags & ConfigFlagDisabled) != 0;
}
