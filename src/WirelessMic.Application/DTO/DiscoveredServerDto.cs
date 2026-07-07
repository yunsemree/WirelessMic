namespace WirelessMic.Application.DTO;

/// <summary>
/// Keşfedilen masaüstü sunucu bilgisi.
/// </summary>
public sealed class DiscoveredServerDto
{
    public required string ComputerName { get; init; }

    public required string IpAddress { get; init; }

    public required string Version { get; init; }

    public DateTimeOffset DiscoveredAt { get; init; } = DateTimeOffset.UtcNow;
}
