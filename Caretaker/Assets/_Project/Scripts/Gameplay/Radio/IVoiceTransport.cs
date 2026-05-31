namespace Caretaker.Gameplay
{
    /// <summary>
    /// 실제 음성 전송 솔루션을 RadioSystem 뒤에 연결하기 위한 경계다.
    /// </summary>
    /// <remarks>
    /// 구현체는 Vivox, Steam Voice, Dissonance, Opus 기반 커스텀 전송 등으로 교체할 수 있다.
    /// </remarks>
    public interface IVoiceTransport
    {
        /// <summary>
        /// 로컬 마이크 송신을 시작한다.
        /// </summary>
        void StartTransmit();

        /// <summary>
        /// 로컬 마이크 송신을 중지한다.
        /// </summary>
        void StopTransmit();
    }
}
