using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 프로토타입용 8-bit mu-law 음성 코덱.
    /// </summary>
    /// <remarks>
    /// 최종 음성 품질 목적이 아니라, 마이크 캡처-압축-전송-복원 흐름 검증을 위한 경량 코덱이다.
    /// </remarks>
    public static class MuLawVoiceCodec
    {
        private const float MU = 255f;
        private static readonly float LogMu = Mathf.Log(1f + MU);

        /// <summary>
        /// -1~1 범위의 float PCM 샘플을 8-bit mu-law 프레임으로 인코딩한다.
        /// </summary>
        /// <param name="samples">입력 PCM 샘플.</param>
        /// <param name="sampleCount">인코딩할 샘플 수.</param>
        /// <param name="encoded">출력 프레임 버퍼.</param>
        public static void Encode(float[] samples, int sampleCount, byte[] encoded)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                float sample = Mathf.Clamp(samples[i], -1f, 1f);
                bool isNegative = sample < 0f;
                float magnitude = Mathf.Log(1f + MU * Mathf.Abs(sample)) / LogMu;
                int quantized = Mathf.Clamp(Mathf.RoundToInt(magnitude * 127f), 0, 127);
                encoded[i] = (byte)(isNegative ? quantized | 0x80 : quantized);
            }
        }

        /// <summary>
        /// 8-bit mu-law 프레임을 -1~1 범위의 float PCM 샘플로 디코딩한다.
        /// </summary>
        /// <param name="encoded">입력 프레임.</param>
        /// <param name="sampleCount">디코딩할 샘플 수.</param>
        /// <param name="samples">출력 PCM 버퍼.</param>
        public static void Decode(byte[] encoded, int sampleCount, float[] samples)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                byte value = encoded[i];
                bool isNegative = (value & 0x80) != 0;
                int quantized = value & 0x7F;
                float magnitude = (Mathf.Pow(1f + MU, quantized / 127f) - 1f) / MU;
                samples[i] = isNegative ? -magnitude : magnitude;
            }
        }
    }
}
