using WirelessMic.Application.DTO;
using WirelessMic.Infrastructure.Discovery;
using WirelessMic.Shared.Constants;

namespace WirelessMic.Tests;

public class DiscoveryMessageFormatterTests
{
    [Fact]
    public void CreateRequest_ReturnsExpectedPayload()
    {
        var request = DiscoveryMessageFormatter.CreateRequest();

        Assert.Equal(DiscoveryProtocol.DiscoverRequest, System.Text.Encoding.UTF8.GetString(request));
    }

    [Fact]
    public void CreateResponse_FormatsMultilineMessage()
    {
        var response = DiscoveryMessageFormatter.CreateResponse("PC-01", "192.168.1.10", "1.0.0");
        var text = System.Text.Encoding.UTF8.GetString(response);

        Assert.Equal("MIC_SERVER\nPC-01\n192.168.1.10\n1.0.0", text);
    }

    [Fact]
    public void TryParseResponse_WithValidMessage_ReturnsServer()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("MIC_SERVER\nDESKTOP\n10.0.0.5\n1.0.0");

        var success = DiscoveryMessageFormatter.TryParseResponse(payload, out var server);

        Assert.True(success);
        Assert.NotNull(server);
        Assert.Equal("DESKTOP", server!.ComputerName);
        Assert.Equal("10.0.0.5", server.IpAddress);
        Assert.Equal("1.0.0", server.Version);
    }

    [Fact]
    public void TryParseResponse_WithInvalidPrefix_ReturnsFalse()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("INVALID\nDESKTOP\n10.0.0.5\n1.0.0");

        var success = DiscoveryMessageFormatter.TryParseResponse(payload, out DiscoveredServerDto? server);

        Assert.False(success);
        Assert.Null(server);
    }

    [Fact]
    public void IsDiscoverRequest_WithValidRequest_ReturnsTrue()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes(DiscoveryProtocol.DiscoverRequest);

        Assert.True(DiscoveryMessageFormatter.IsDiscoverRequest(payload));
    }
}
