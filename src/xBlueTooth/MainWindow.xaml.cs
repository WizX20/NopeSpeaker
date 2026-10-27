using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Devices.Enumeration;
using Windows.Graphics;
using XBlueTooth.Core;

namespace XBlueTooth;

public sealed partial class MainWindow : Window
{
    private readonly DeviceWatcher _watcher = WindowsDevices.CreatePairedWatcher();
    private readonly DispatcherQueueTimer _refreshSoon;
    private readonly ObservableCollection<DeviceItem> _items = [];
    private volatile bool _enumerated;
    private bool _changing;
    // Bumped by every refresh and every change: a refresh that finishes after a newer one started,
    // or after a change started, is out of date and dropped.
    private int _generation;
    private int _refreshing;

    public MainWindow()
    {
        InitializeComponent();

        SystemBackdrop = new MicaBackdrop();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        // AppWindow sizes in physical pixels: scale for the display.
        var scale = GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(this)) / 96.0;
        AppWindow.Resize(new SizeInt32((int)(960 * scale), (int)(720 * scale)));
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "xBlueTooth.ico"));
        Devices.ItemsSource = _items;

        // A device connecting fires a burst of updates: refresh once it settles.
        _refreshSoon = DispatcherQueue.CreateTimer();
        _refreshSoon.Interval = TimeSpan.FromMilliseconds(500);
        _refreshSoon.IsRepeating = false;
        _refreshSoon.Tick += async (_, _) => await RefreshAsync();

        // The watcher reports every paired device as Added first; only changes after that matter.
        _watcher.EnumerationCompleted += (_, _) => _enumerated = true;
        _watcher.Added += (_, _) => { if (_enumerated) RefreshSoon(); };
        _watcher.Updated += (_, _) => RefreshSoon();
        _watcher.Removed += (_, _) => RefreshSoon();
        _watcher.Stopped += (sender, _) =>
        {
            if (sender.Status == DeviceWatcherStatus.Aborted)
            {
                DispatcherQueue.TryEnqueue(() => ShowStatus(InfoBarSeverity.Warning, "Not watching for changes",
                    "Windows stopped reporting Bluetooth changes. Use Refresh to update the list."));
            }
        };
        _watcher.Start();
        Closed += (_, _) =>
        {
            if (_watcher.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted)
            {
                _watcher.Stop();
            }
        };

        _ = RefreshAsync();
    }

    private void RefreshSoon() => DispatcherQueue.TryEnqueue(() =>
    {
        _refreshSoon.Stop();
        _refreshSoon.Start();
    });

    private async Task RefreshAsync()
    {
        // A refresh mid-change would put the old state back on the switch the user just flipped;
        // the change refreshes when it is done.
        if (_changing)
        {
            return;
        }

        var generation = ++_generation;
        _refreshing++;
        Loading.IsActive = true;
        try
        {
            var devices = await WindowsDevices.GetDevicesAsync();
            if (generation == _generation && !_changing)
            {
                Show(devices);
            }
        }
        catch (Exception e)
        {
            ShowStatus(InfoBarSeverity.Error, "Could not read the Bluetooth devices", e.Message);
        }
        finally
        {
            Loading.IsActive = --_refreshing > 0;
            FirstLoad.Visibility = Visibility.Collapsed;
        }
    }

    // Updates the rows in place, in the order of the list: a row keeps its switch, focus and scroll.
    private void Show(IReadOnlyList<BluetoothDevice> devices)
    {
        for (var i = 0; i < devices.Count; i++)
        {
            var device = devices[i];
            var at = IndexOf(device.ContainerId, i);
            if (at < 0)
            {
                _items.Insert(i, new DeviceItem(device) { IsBusy = _changing });
                continue;
            }

            if (at != i)
            {
                _items.Move(at, i);
            }

            _items[i].Device = device;
        }

        while (_items.Count > devices.Count)
        {
            _items.RemoveAt(_items.Count - 1);
        }

        Empty.Visibility = devices.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private int IndexOf(Guid container, int from)
    {
        for (var i = from; i < _items.Count; i++)
        {
            if (_items[i].Device.ContainerId == container)
            {
                return i;
            }
        }

        return -1;
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async void OnAudioToggled(object sender, RoutedEventArgs e)
    {
        // Toggled also fires when the binding sets IsOn: only a flip away from the device's state
        // is the user's.
        if (sender is not ToggleSwitch { DataContext: DeviceItem item } toggle
            || toggle.IsOn == item.IsAudioEnabled
            || _changing)
        {
            return;
        }

        var enable = toggle.IsOn;
        SetChanging(true);
        Status.IsOpen = false;
        ChangeOutcome outcome;
        try
        {
            outcome = await ElevatedPnp.SetEnabledAsync(item.Device.AudioNodes.Select(n => n.InstanceId), enable);
        }
        catch (Exception ex)
        {
            outcome = new(ChangeResult.Failed, ex.Message);
        }
        finally
        {
            SetChanging(false);
        }

        if (outcome.Result == ChangeResult.Cancelled)
        {
            // Nothing changed: the switch goes back - unless the row was reused for another device.
            if (toggle.DataContext == item)
            {
                toggle.IsOn = item.IsAudioEnabled;
            }

            ShowStatus(InfoBarSeverity.Informational, "Nothing changed",
                "Changing a device needs administrator rights, and the prompt was declined.");
            return;
        }

        await RefreshAsync();

        // Switching off the audio driver can take its endpoint with it, which the script reports
        // as a failure: what counts is the state the device ends up in.
        var wanted = enable ? AudioState.Enabled : AudioState.Disabled;
        if (outcome.Result == ChangeResult.Succeeded || item.Device.AudioState == wanted)
        {
            ShowStatus(InfoBarSeverity.Success, item.Name,
                enable ? "Audio is on: Windows switches to it when it connects." : "Audio is off: it still connects, but Windows plays nothing on it.");
        }
        else
        {
            ShowStatus(InfoBarSeverity.Error, "Could not change " + item.Name,
                outcome.Error ?? "Switching its audio nodes failed. Try it in Device Manager to see why.");
        }
    }

    // One change at a time: every switch is off while one runs.
    private void SetChanging(bool changing)
    {
        _changing = changing;
        if (changing)
        {
            _generation++;
        }

        foreach (var item in _items)
        {
            item.IsBusy = changing;
        }
    }

    private void ShowStatus(InfoBarSeverity severity, string title, string message)
    {
        Status.Severity = severity;
        Status.Title = title;
        Status.Message = message;
        Status.IsOpen = true;
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hwnd);
}
