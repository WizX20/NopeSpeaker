using System.Text;

namespace NopeSpeaker.Core;

/// <summary>
/// The PowerShell that switches device nodes on or off. It runs elevated: disabling a device needs
/// an administrator, the app itself does not.
/// </summary>
public static class PnpScript
{
    /// <summary>
    /// A script that enables or disables every node. It goes on past a node that fails (switching
    /// off the audio driver can take its endpoint with it) and exits 1 when any did, writing the
    /// errors to <paramref name="errorFile"/> when one is given.
    /// </summary>
    /// <exception cref="ArgumentException">No IDs, or one that is not a device instance ID.</exception>
    public static string Build(IEnumerable<string> instanceIds, bool enable, string? errorFile = null)
    {
        ArgumentNullException.ThrowIfNull(instanceIds);

        var ids = instanceIds.ToList();
        if (ids.Count == 0)
        {
            throw new ArgumentException("No device node to change.", nameof(instanceIds));
        }

        foreach (var id in ids)
        {
            if (!IsInstanceId(id))
            {
                throw new ArgumentException($"Not a device instance ID: '{id}'.", nameof(instanceIds));
            }
        }

        var list = string.Join(", ", ids.Select(Quote));
        var verb = enable ? "Enable" : "Disable";
        // The cmdlets come from the module in Windows PowerShell's own folder: an elevated script
        // should not load a PnpDevice module planted earlier on the user's PSModulePath.
        var script = new StringBuilder()
            .Append("$ErrorActionPreference = 'Stop'; ")
            .Append("Import-Module (Join-Path $PSHOME 'Modules\\PnpDevice\\PnpDevice.psd1'); ")
            .Append("$failed = @(); ")
            .Append("foreach ($id in @(").Append(list).Append(")) { ")
            .Append("try { PnpDevice\\").Append(verb).Append("-PnpDevice -InstanceId $id -Confirm:$false } ")
            .Append("catch { $failed += \"$id`: $_\" } } ");
        if (errorFile is not null)
        {
            script.Append("if ($failed) { $failed | Out-File -LiteralPath ").Append(Quote(errorFile)).Append(" -Encoding utf8 } ");
        }

        return script.Append("if ($failed) { exit 1 }").ToString();
    }

    /// <summary>The script for <c>powershell -EncodedCommand</c>: base64 of UTF-16LE, so no quoting survives to the command line.</summary>
    public static string Encode(string script) => Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

    /// <summary>
    /// What Windows allows in a device instance ID: printable ASCII past the space, no comma. Nothing
    /// else gets into the elevated script, so no quote of any kind can end its string.
    /// </summary>
    public static bool IsInstanceId(string? id) =>
        !string.IsNullOrEmpty(id) && id.All(c => c > ' ' && c < '\x7f' && c != ',');

    // Single-quoted PowerShell strings take everything literally but a quote, which doubles. PowerShell
    // also ends them at the typographic quotes U+2018-U+201B: those double too.
    private static string Quote(string value)
    {
        var quoted = new StringBuilder("'");
        foreach (var c in value)
        {
            quoted.Append(c);
            if (c is '\'' or '‘' or '’' or '‚' or '‛')
            {
                quoted.Append(c);
            }
        }

        return quoted.Append('\'').ToString();
    }
}
