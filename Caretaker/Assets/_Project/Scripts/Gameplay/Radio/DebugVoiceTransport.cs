using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 실제 음성 SDK가 확정되기 전 송신 시작/중지를 검증하기 위한 개발용 전송 스텁이다.
    /// </summary>
    public sealed class DebugVoiceTransport : MonoBehaviour, IVoiceTransport
    {
        [SerializeField] private bool _logStateChanges = true;

        private bool _isTransmitting;

        /// <summary>
        /// 현재 송신 스텁이 활성 상태인지 반환한다.
        /// </summary>
        public bool IsTransmitting => _isTransmitting;

        /// <inheritdoc />
        public void StartTransmit()
        {
            if (_isTransmitting)
            {
                return;
            }

            _isTransmitting = true;
            if (_logStateChanges)
            {
                Debug.Log("[Radio] Voice transmit started.", this);
            }
        }

        /// <inheritdoc />
        public void StopTransmit()
        {
            if (!_isTransmitting)
            {
                return;
            }

            _isTransmitting = false;
            if (_logStateChanges)
            {
                Debug.Log("[Radio] Voice transmit stopped.", this);
            }
        }
    }
}
