namespace WirelessMic.Infrastructure.Networking;

/// <summary>
/// Gecikme ölçümü değerlerini izler.
/// </summary>
public sealed class LatencyMonitor : Application.Interfaces.ILatencyMonitor
{
    private double _currentLatencyMs;

    /// <inheritdoc />
    public double CurrentLatencyMs => _currentLatencyMs;

    /// <inheritdoc />
    public void UpdateLatency(double latencyMs) => _currentLatencyMs = latencyMs;
}
