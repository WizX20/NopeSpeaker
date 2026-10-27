using System.Text;
using NopeSpeaker.Core;

namespace NopeSpeaker.Core.Tests;

public class PnpScriptTests
{
    [Theory]
    [InlineData(true, "Enable-PnpDevice")]
    [InlineData(false, "Disable-PnpDevice")]
    public void Switches_every_node_the_asked_way(bool enable, string cmdlet)
    {
        var script = PnpScript.Build([@"BTHENUM\{0000110B}_LOCALMFG&0002\7&3&0", @"SWD\MMDEVAPI\{0.0.0}"], enable);

        script.Should().Contain($@"PnpDevice\{cmdlet} -InstanceId $id -Confirm:$false")
            .And.Contain(@"foreach ($id in @('BTHENUM\{0000110B}_LOCALMFG&0002\7&3&0', 'SWD\MMDEVAPI\{0.0.0}'))");
    }

    [Fact]
    public void Loads_the_pnpdevice_module_from_windows_powershell_itself()
    {
        PnpScript.Build(["X"], enable: true)
            .Should().StartWith("$ErrorActionPreference = 'Stop'; Import-Module (Join-Path $PSHOME 'Modules\\PnpDevice\\PnpDevice.psd1');");
    }

    [Fact]
    public void Goes_on_past_a_failing_node_and_exits_1_when_any_failed()
    {
        var script = PnpScript.Build(["X"], enable: false);

        script.Should().Contain("catch { $failed += \"$id`: $_\" }")
            .And.EndWith("if ($failed) { exit 1 }");
    }

    [Fact]
    public void Writes_the_errors_to_the_error_file_quoted()
    {
        PnpScript.Build(["X"], enable: false, errorFile: @"C:\Temp\O'Brien\err.txt")
            .Should().Contain(@"$failed | Out-File -LiteralPath 'C:\Temp\O''Brien\err.txt' -Encoding utf8");
    }

    [Fact]
    public void Writes_no_error_file_when_none_is_given()
    {
        PnpScript.Build(["X"], enable: false).Should().NotContain("Out-File");
    }

    [Theory]
    [InlineData("a'; Remove-Item x; '")]
    // PowerShell ends a single-quoted string at the typographic quotes too.
    [InlineData("a\u2019; Remove-Item x; \u2019")]
    [InlineData("a\u2018b")]
    [InlineData("a b")]
    [InlineData("a,b")]
    [InlineData("a\u00e9")]
    [InlineData("")]
    [InlineData(null)]
    public void Refuses_what_is_not_a_device_instance_id(string? id)
    {
        var build = () => PnpScript.Build([id!], enable: false);

        build.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("C:\\Users\\O'Brien\\x.txt", "'C:\\Users\\O''Brien\\x.txt'")]
    [InlineData("C:\\Users\\O\u2019Brien\\x.txt", "'C:\\Users\\O\u2019\u2019Brien\\x.txt'")]
    [InlineData("C:\\a\u2018\u201a\u201b", "'C:\\a\u2018\u2018\u201a\u201a\u201b\u201b'")]
    public void Doubles_every_kind_of_single_quote_in_the_error_file(string path, string quoted)
    {
        PnpScript.Build(["X"], enable: true, errorFile: path).Should().Contain($"-LiteralPath {quoted} ");
    }

    [Theory]
    [InlineData(@"BTHENUM\{0000110B-0000-1000-8000-00805F9B34FB}_LOCALMFG&0002\7&3A1B2C3D&0&001122334455_C00000000", true)]
    [InlineData(@"SWD\MMDEVAPI\{0.0.0.00000000}.{90ba3902-0000-0000-0000-000000000000}", true)]
    [InlineData("a b", false)]
    [InlineData("a\tb", false)]
    [InlineData("a\u007fb", false)]
    public void Knows_a_device_instance_id(string id, bool expected)
    {
        PnpScript.IsInstanceId(id).Should().Be(expected);
    }

    [Fact]
    public void Refuses_an_empty_list()
    {
        var build = () => PnpScript.Build([], enable: true);

        build.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Encodes_as_base64_of_utf16_the_way_powershell_reads_it()
    {
        var encoded = PnpScript.Encode("Get-Date");

        Encoding.Unicode.GetString(Convert.FromBase64String(encoded)).Should().Be("Get-Date");
    }
}
