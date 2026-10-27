namespace XBlueTooth.Core;

/// <summary>The device setup classes xBlueTooth switches: the ones that make Windows play audio on a device.</summary>
public static class DeviceClasses
{
    /// <summary>MEDIA - the Bluetooth audio profile driver (A2DP sink, hands-free).</summary>
    public static readonly Guid Media = new("4d36e96c-e325-11ce-bfc1-08002be10318");

    /// <summary>AudioEndpoint - the speaker or headset Windows lists under Sound.</summary>
    public static readonly Guid AudioEndpoint = new("c166523c-fe0c-4a94-a586-f1a80cfbbf3e");

    /// <summary>Whether a node of this class carries audio.</summary>
    public static bool IsAudio(Guid classGuid) => classGuid == Media || classGuid == AudioEndpoint;
}
