using System.ComponentModel;
using System.Runtime.CompilerServices;
using NopeSpeaker.Core;

namespace NopeSpeaker;

/// <summary>One row in the device list. A refresh updates it in place, so the row keeps focus and scroll.</summary>
public sealed partial class DeviceItem(BluetoothDevice device) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public BluetoothDevice Device
    {
        get;
        set
        {
            field = value;
            // Also when equal: after a declined change this puts the switch back where the device is.
            OnPropertyChanged(null);
        }
    } = device;

    public string Name => Device.Name;

    public bool HasAudio => Device.AudioState != AudioState.None;

    public bool IsAudioEnabled => Device.AudioState == AudioState.Enabled;

    public string Glyph => HasAudio ? "" : ""; // Speakers : Bluetooth

    public string Description
    {
        get
        {
            var audio = Device.AudioState switch
            {
                AudioState.Enabled => "audio switches over when it connects",
                AudioState.Disabled => "audio off - Windows will not switch to it",
                AudioState.Partial => "audio partly off",
                _ => "no audio",
            };
            string[] parts = [Device.IsConnected ? "Connected" : "Not connected", audio, Device.Address];
            return string.Join(" · ", parts.Where(p => p.Length > 0));
        }
    }

    /// <summary>A change is running - on this device or another: one at a time.</summary>
    public bool IsBusy
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsIdle));
            }
        }
    }

    public bool IsIdle => !IsBusy;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
