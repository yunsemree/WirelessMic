using System;

namespace WirelessMic.Domain.Audio;

/// <summary>
/// 16-bit little-endian PCM ses kareleri üzerinde yazılım kazancı (gain) uygular.
/// Saf (yan etkisiz, altyapıya bağımsız) DSP mantığıdır.
/// </summary>
public static class PcmGainProcessor
{
    /// <summary>Gain Boost için kullanılan +6 dB'lik kazanç katsayısı (10^(6/20) ≈ 1.9953).</summary>
    public const float GainBoostFactor = 1.9952623f;

    private const short SampleMax = short.MaxValue;
    private const short SampleMin = short.MinValue;

    /// <summary>
    /// Verilen 16-bit PCM tamponuna kazancı yerinde uygular. Örnekler
    /// [-32768, 32767] aralığına kırpılır (clipping) böylece taşma/sarma olmaz.
    /// </summary>
    /// <param name="pcm16">İşlenecek 16-bit little-endian PCM verisi.</param>
    /// <param name="gainFactor">Uygulanacak kazanç katsayısı (1.0 = değişiklik yok).</param>
    public static void ApplyGainInPlace(Span<byte> pcm16, float gainFactor)
    {
        if (gainFactor == 1.0f || pcm16.Length < 2)
            return;

        // Son tek bayt (yarım örnek) varsa güvenli şekilde atlanır.
        int sampleCount = pcm16.Length / 2;

        for (int i = 0; i < sampleCount; i++)
        {
            int offset = i * 2;
            short sample = (short)(pcm16[offset] | (pcm16[offset + 1] << 8));

            float amplified = sample * gainFactor;

            short result = amplified >= SampleMax
                ? SampleMax
                : amplified <= SampleMin
                    ? SampleMin
                    : (short)amplified;

            pcm16[offset] = (byte)(result & 0xFF);
            pcm16[offset + 1] = (byte)((result >> 8) & 0xFF);
        }
    }
}
