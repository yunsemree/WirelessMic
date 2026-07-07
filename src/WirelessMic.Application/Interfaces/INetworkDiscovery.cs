using WirelessMic.Application.DTO;

namespace WirelessMic.Application.Interfaces;

/// <summary>
/// Yerel ağ üzerinde masaüstü sunucuları keşfeder.
/// </summary>
public interface INetworkDiscovery
{
    /// <summary>Keşif işlemini başlatır.</summary>
    Task<IReadOnlyList<DiscoveredServerDto>> DiscoverServersAsync(CancellationToken cancellationToken = default);

    /// <summary>Keşif dinleyicisini başlatır (masaüstü modu).</summary>
    Task StartListeningAsync(CancellationToken cancellationToken = default);

    /// <summary>Keşif dinleyicisini durdurur.</summary>
    Task StopListeningAsync(CancellationToken cancellationToken = default);

    /// <summary>Dinleyicinin aktif olup olmadığını belirtir.</summary>
    bool IsListening { get; }
}
