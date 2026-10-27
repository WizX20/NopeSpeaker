namespace NopeSpeaker.Core;

/// <summary>Whether Windows may play audio on a device.</summary>
public enum AudioState
{
    /// <summary>The device has no audio nodes (a mouse, or an audio device not seen since it was paired).</summary>
    None,

    /// <summary>Every audio node is on: Windows switches to the device as soon as it connects.</summary>
    Enabled,

    /// <summary>Every audio node is off: the device connects, but Windows plays nothing on it.</summary>
    Disabled,

    /// <summary>Some nodes on, some off - switched by hand elsewhere.</summary>
    Partial,
}

/// <summary>A paired Bluetooth device with its audio nodes.</summary>
/// <param name="Name">The device name.</param>
/// <param name="Address">The Bluetooth address.</param>
/// <param name="ContainerId">The physical device.</param>
/// <param name="IsConnected">Whether it is connected right now.</param>
/// <param name="AudioNodes">The MEDIA and AudioEndpoint nodes of the device.</param>
public sealed record BluetoothDevice(string Name, string Address, Guid ContainerId, bool IsConnected, IReadOnlyList<PnpNode> AudioNodes)
{
    /// <summary>Whether Windows may play audio on the device.</summary>
    public AudioState AudioState
    {
        get
        {
            if (AudioNodes.Count == 0)
            {
                return AudioState.None;
            }

            var disabled = AudioNodes.Count(n => n.IsDisabled);
            return disabled == 0 ? AudioState.Enabled
                : disabled == AudioNodes.Count ? AudioState.Disabled
                : AudioState.Partial;
        }
    }
}
