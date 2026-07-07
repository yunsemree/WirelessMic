using WirelessMic.Shared.Constants;

namespace WirelessMic.Tests;

public class ConnectionMessageFormatterTests
{
    [Fact]
    public void CreateConnect_IncludesClientName()
    {
        var payload = Infrastructure.Networking.ConnectionMessageFormatter.CreateConnect("MyPhone");
        var text = System.Text.Encoding.UTF8.GetString(payload);

        Assert.Equal($"{ConnectionProtocol.Connect}\nMyPhone", text);
    }

    [Fact]
    public void TryParse_ConnectOk_ReturnsSessionId()
    {
        var payload = Infrastructure.Networking.ConnectionMessageFormatter.CreateConnectOk("abc123");

        var success = Infrastructure.Networking.ConnectionMessageFormatter.TryParse(
            payload,
            out var messageType,
            out var lines);

        Assert.True(success);
        Assert.Equal(ConnectionProtocol.ConnectOk, messageType);
        Assert.Equal("abc123", lines[1]);
    }

    [Fact]
    public void CreatePingAndPong_UseSameTimestamp()
    {
        const long ticks = 123456789;
        var ping = Infrastructure.Networking.ConnectionMessageFormatter.CreatePing(ticks);
        var pong = Infrastructure.Networking.ConnectionMessageFormatter.CreatePong(ticks);

        Infrastructure.Networking.ConnectionMessageFormatter.TryParse(ping, out var pingType, out var pingLines);
        Infrastructure.Networking.ConnectionMessageFormatter.TryParse(pong, out var pongType, out var pongLines);

        Assert.Equal(ConnectionProtocol.Ping, pingType);
        Assert.Equal(ConnectionProtocol.Pong, pongType);
        Assert.Equal(pingLines[1], pongLines[1]);
    }
}
