namespace WirelessMic.Shared.Constants;

/// <summary>
/// Ses akışı ile ilgili sabitler.
/// </summary>
public static class AudioConstants
{
    public const int SampleRate = 48000;
    public const int BitsPerSample = 16;
    public const int Channels = 1;
    public const int FrameDurationMs = 20;
    public const int FrameSizeBytes = SampleRate * BitsPerSample / 8 * Channels * FrameDurationMs / 1000;
}
