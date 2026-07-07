namespace WirelessMic.Application.Interfaces;

/// <summary>
/// Ağ ve ses gecikmesini izler.
/// </summary>
public interface ILatencyMonitor
{
    /// <summary>Mevcut gecikmeyi milisaniye cinsinden döndürür.</summary>
    double CurrentLatencyMs { get; }

    /// <summary>Gecikme ölçümünü günceller.</summary>
    void UpdateLatency(double latencyMs);
}
