using XBlueTooth.Core;

namespace XBlueTooth.Core.Tests;

public class DeviceCatalogTests
{
    private static readonly Guid Soundbar = Guid.NewGuid();
    private static readonly Guid Mouse = Guid.NewGuid();

    private static PnpNode Node(string id, Guid container, Guid cls, uint problem = 0, uint configFlags = 0) =>
        new(id, id, cls, container, problem, configFlags);

    [Fact]
    public void Links_audio_nodes_to_their_device_by_container()
    {
        var devices = DeviceCatalog.Build(
            [new("Soundbar", "00:11", Soundbar, true), new("Mouse", "00:22", Mouse, true)],
            [
                Node("BTHENUM\\DEV_0011", Soundbar, Guid.NewGuid()),
                Node("BTHENUM\\{110B}_0011", Soundbar, DeviceClasses.Media),
                Node("SWD\\MMDEVAPI\\speakers", Soundbar, DeviceClasses.AudioEndpoint),
                Node("HID\\mouse", Mouse, Guid.NewGuid()),
            ]);

        var soundbar = devices.Single(d => d.Name == "Soundbar");
        soundbar.AudioNodes.Select(n => n.InstanceId).Should().BeEquivalentTo("BTHENUM\\{110B}_0011", "SWD\\MMDEVAPI\\speakers");
        soundbar.AudioState.Should().Be(AudioState.Enabled);
        devices.Single(d => d.Name == "Mouse").AudioState.Should().Be(AudioState.None);
    }

    [Fact]
    public void Shows_a_device_paired_over_classic_and_low_energy_once_connected_if_either_is()
    {
        var devices = DeviceCatalog.Build(
            [new("Buds", "00:33", Soundbar, false), new("Buds", "00:33", Soundbar, true)],
            []);

        devices.Should().ContainSingle().Which.IsConnected.Should().BeTrue();
    }

    [Fact]
    public void Takes_name_and_address_of_a_dual_mode_device_from_the_connected_endpoint()
    {
        var device = DeviceCatalog.Build(
            [new("Buds LE", "00:44", Soundbar, false), new("Buds", "00:33", Soundbar, true)],
            []).Single();

        device.Name.Should().Be("Buds");
        device.Address.Should().Be("00:33");
    }

    [Fact]
    public void Lists_connected_devices_first_then_by_name()
    {
        var devices = DeviceCatalog.Build(
            [new("b", "", Guid.NewGuid(), false), new("z", "", Guid.NewGuid(), true), new("a", "", Guid.NewGuid(), false)],
            []);

        devices.Select(d => d.Name).Should().Equal("z", "a", "b");
    }

    [Fact]
    public void Leaves_out_endpoints_without_a_container()
    {
        DeviceCatalog.Build([new("ghost", "", Guid.Empty, false)], []).Should().BeEmpty();
    }

    [Fact]
    public void Ignores_audio_nodes_of_devices_that_are_not_paired()
    {
        var device = DeviceCatalog.Build(
            [new("Soundbar", "", Soundbar, true)],
            [Node("SWD\\MMDEVAPI\\other", Guid.NewGuid(), DeviceClasses.AudioEndpoint), Node("SWD\\MMDEVAPI\\none", Guid.Empty, DeviceClasses.AudioEndpoint)])
            .Single();

        device.AudioState.Should().Be(AudioState.None);
    }

    [Fact]
    public void Lists_a_node_reported_twice_once()
    {
        var device = DeviceCatalog.Build(
            [new("Soundbar", "", Soundbar, true)],
            [Node("BTHENUM\\{110B}_0011", Soundbar, DeviceClasses.Media), Node("bthenum\\{110b}_0011", Soundbar, DeviceClasses.Media)])
            .Single();

        device.AudioNodes.Should().ContainSingle();
    }

    [Fact]
    public void Ignores_nodes_without_a_class()
    {
        var device = DeviceCatalog.Build([new("Soundbar", "", Soundbar, true)], [Node("X", Soundbar, Guid.Empty)]).Single();

        device.AudioNodes.Should().BeEmpty();
    }

    [Fact]
    public void Refuses_null_inputs()
    {
        var noPaired = () => DeviceCatalog.Build(null!, []);
        var noNodes = () => DeviceCatalog.Build([], null!);

        noPaired.Should().Throw<ArgumentNullException>();
        noNodes.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0u, 0u, AudioState.Enabled)]
    [InlineData(PnpNode.ProblemDisabled, PnpNode.ProblemDisabled, AudioState.Disabled)]
    [InlineData(PnpNode.ProblemDisabled, 0u, AudioState.Partial)]
    // Another problem (a driver error, say) is not "switched off".
    [InlineData(10u, 0u, AudioState.Enabled)]
    public void Reads_the_audio_state_from_the_problem_codes(uint media, uint endpoint, AudioState expected)
    {
        var device = new BluetoothDevice("x", "", Soundbar, true,
            [Node("m", Soundbar, DeviceClasses.Media, media), Node("e", Soundbar, DeviceClasses.AudioEndpoint, endpoint)]);

        device.AudioState.Should().Be(expected);
    }

    // A node that is not present (a disconnected device) has no problem code: the stored flag says it is off.
    [Theory]
    [InlineData(PnpNode.ConfigFlagDisabled, PnpNode.ConfigFlagDisabled, AudioState.Disabled)]
    [InlineData(PnpNode.ConfigFlagDisabled | 0x400u, 0u, AudioState.Partial)]
    [InlineData(0x400u, 0u, AudioState.Enabled)]
    public void Reads_the_audio_state_of_absent_nodes_from_the_config_flags(uint media, uint endpoint, AudioState expected)
    {
        var device = new BluetoothDevice("x", "", Soundbar, false,
            [Node("m", Soundbar, DeviceClasses.Media, configFlags: media), Node("e", Soundbar, DeviceClasses.AudioEndpoint, configFlags: endpoint)]);

        device.AudioState.Should().Be(expected);
    }

    [Theory]
    [InlineData("4d36e96c-e325-11ce-bfc1-08002be10318", true)]
    [InlineData("c166523c-fe0c-4a94-a586-f1a80cfbbf3e", true)]
    [InlineData("e0cbf06c-cd8b-4647-bb8a-263b43f0f974", false)]
    public void Knows_the_audio_classes(string cls, bool expected)
    {
        DeviceClasses.IsAudio(new Guid(cls)).Should().Be(expected);
    }
}
