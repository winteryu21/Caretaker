using Concentus.Enums;
using Concentus.Structs;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// Concentus 기반 Opus 음성 코덱 래퍼.
    /// </summary>
    /// <remarks>
    /// Opus는 2.5, 5, 10, 20, 40, 60ms 프레임을 요구한다.
    /// 현재 RadioSystem 기본값인 16 kHz / 320 samples는 20ms 프레임이다.
    /// </remarks>
    public sealed class ConcentusOpusVoiceCodec
    {
        private const int CHANNELS = 1;
        private const int DEFAULT_BITRATE = 48000;
        private const int DEFAULT_COMPLEXITY = 8;
        private const int DEFAULT_PACKET_LOSS_PERCENT = 5;

        private readonly OpusDecoder _decoder;
        private readonly OpusEncoder _encoder;
        private readonly int _frameSamples;

        /// <summary>
        /// Opus 코덱 인스턴스를 생성한다.
        /// </summary>
        /// <param name="sampleRate">입출력 샘플레이트.</param>
        /// <param name="frameSamples">프레임당 샘플 수.</param>
        public ConcentusOpusVoiceCodec(int sampleRate, int frameSamples)
        {
            _frameSamples = frameSamples;
#pragma warning disable CS0618 // Unity compatibility: avoid Span<T> API in this project profile.
            _encoder = new OpusEncoder(sampleRate, CHANNELS, OpusApplication.OPUS_APPLICATION_VOIP);
            _decoder = new OpusDecoder(sampleRate, CHANNELS);
#pragma warning restore CS0618

            _encoder.Bitrate = DEFAULT_BITRATE;
            _encoder.Complexity = DEFAULT_COMPLEXITY;
            _encoder.SignalType = OpusSignal.OPUS_SIGNAL_VOICE;
            _encoder.UseVBR = true;
            _encoder.UseConstrainedVBR = true;
            _encoder.UseInbandFEC = true;
            _encoder.PacketLossPercent = DEFAULT_PACKET_LOSS_PERCENT;
        }

        /// <summary>
        /// float PCM 프레임을 Opus 패킷으로 인코딩한다.
        /// </summary>
        /// <param name="samples">입력 PCM 샘플.</param>
        /// <param name="encoded">출력 Opus 패킷 버퍼.</param>
        /// <returns>인코딩된 바이트 수.</returns>
        public int Encode(float[] samples, byte[] encoded)
        {
#pragma warning disable CS0618 // Unity compatibility: avoid Span<T> API in this project profile.
            return _encoder.Encode(samples, 0, _frameSamples, encoded, 0, encoded.Length);
#pragma warning restore CS0618
        }

        /// <summary>
        /// Opus 패킷을 float PCM 프레임으로 디코딩한다.
        /// </summary>
        /// <param name="encoded">입력 Opus 패킷.</param>
        /// <param name="samples">출력 PCM 샘플 버퍼.</param>
        /// <returns>디코딩된 샘플 수.</returns>
        public int Decode(byte[] encoded, int encodedLength, float[] samples)
        {
#pragma warning disable CS0618 // Unity compatibility: avoid Span<T> API in this project profile.
            return _decoder.Decode(encoded, 0, encodedLength, samples, 0, _frameSamples, false);
#pragma warning restore CS0618
        }
    }
}
