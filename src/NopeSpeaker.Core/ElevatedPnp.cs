using System.ComponentModel;
using System.Diagnostics;

namespace NopeSpeaker.Core;

/// <summary>How a change to device nodes went.</summary>
public enum ChangeResult
{
    /// <summary>Every node changed.</summary>
    Succeeded,

    /// <summary>The elevation prompt was declined, or the account may not elevate.</summary>
    Cancelled,

    /// <summary>The elevated script failed, or did not finish in time.</summary>
    Failed,
}

/// <summary>A change's result, with what went wrong when it failed.</summary>
/// <param name="Result">How it went.</param>
/// <param name="Error">The script's errors for a failed change, when it could report them.</param>
public sealed record ChangeOutcome(ChangeResult Result, string? Error = null);

/// <summary>Switches device nodes on or off through an elevated Windows PowerShell (one UAC prompt per change).</summary>
public static class ElevatedPnp
{
    // ERROR_CANCELLED: the UAC prompt was declined.
    private const int ErrorCancelled = 1223;

    /// <summary>How long a change may take before the app stops waiting for it.</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    /// <summary>Enables or disables the nodes and waits for it.</summary>
    public static async Task<ChangeOutcome> SetEnabledAsync(IEnumerable<string> instanceIds, bool enable, CancellationToken cancellationToken = default)
    {
        // The elevated script writes its errors here: its window is hidden, and a runas process
        // cannot have its output redirected.
        var errorFile = Path.Combine(Path.GetTempPath(), $"NopeSpeaker-{Guid.NewGuid():N}.txt");
        var script = PnpScript.Build(instanceIds, enable, errorFile);
        // The full path: an elevated process should not be whatever powershell.exe PATH finds first.
        var powershell = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        var start = new ProcessStartInfo(powershell)
        {
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden,
            Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + PnpScript.Encode(script),
        };

        Process? process;
        try
        {
            process = Process.Start(start);
        }
        catch (Win32Exception e) when (e.NativeErrorCode == ErrorCancelled)
        {
            return new(ChangeResult.Cancelled);
        }

        if (process is null)
        {
            return new(ChangeResult.Failed);
        }

        using (process)
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            timeout.CancelAfter(Timeout);
            try
            {
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new(ChangeResult.Failed, $"The change did not finish within {Timeout.TotalMinutes:0} minutes.");
            }

            return process.ExitCode == 0
                ? new(ChangeResult.Succeeded)
                : new(ChangeResult.Failed, ReadAndDelete(errorFile));
        }
    }

    private static string? ReadAndDelete(string path)
    {
        try
        {
            var text = File.ReadAllText(path).Trim();
            File.Delete(path);
            return text.Length == 0 ? null : text;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
