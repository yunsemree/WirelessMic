namespace WirelessMic.Application.DTO;

/// <summary>
/// Bağlı istemci bilgisi (masaüstü modu).
/// </summary>
public sealed class ConnectedClientDto
{
    public required string SessionId { get; init; }

    public required string ClientName { get; init; }

    public required string RemoteAddress { get; init; }

    public DateTimeOffset ConnectedAt { get; init; } = DateTimeOffset.UtcNow;
}
