using Caretaker.Gameplay;
using TMPro;
using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// HUD 상태를 ViewModel 기반으로 렌더링한다.
    /// 정보 격리 필터를 적용하여 허용된 정보만 표시한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.10 — UI / HUD 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class HudPresenter : MonoBehaviour
    {
        [Header("Radio")]
        [SerializeField] private TMP_Text _radioStatusText;
        [SerializeField] private GameObject _radioActiveIndicator;

        private RadioState _radioState = RadioState.Idle;
        private ulong _radioTalkerId = RadioNetworkBridge.NO_TALKER_ID;
        private string _radioStatus = "Waiting for Radio";

        /// <summary>
        /// 현재 HUD에 표시 중인 무전기 상태 문구를 반환한다.
        /// </summary>
        public string RadioStatus => _radioStatus;

        private void Awake()
        {
            RenderRadioState();
        }

        /// <summary>
        /// 무전기 상태를 HUD에 표시한다.
        /// </summary>
        /// <param name="state">로컬 플레이어 기준 무전기 상태.</param>
        /// <param name="talkerId">현재 송신권 보유자 ID.</param>
        public void SetRadioState(RadioState state, ulong talkerId)
        {
            _radioState = state;
            _radioTalkerId = talkerId;
            _radioStatus = BuildRadioStatusText(state, talkerId);
            RenderRadioState();
        }

        private void RenderRadioState()
        {
            if (_radioStatusText != null)
            {
                _radioStatusText.text = _radioStatus;
            }

            if (_radioActiveIndicator != null)
            {
                _radioActiveIndicator.SetActive(_radioState is RadioState.Transmitting or RadioState.Receiving);
            }
        }

        private static string BuildRadioStatusText(RadioState state, ulong talkerId)
        {
            return state switch
            {
                RadioState.Transmitting => "Radio Tx",
                RadioState.Receiving => "Radio Rx",
                RadioState.Blocked => talkerId == RadioNetworkBridge.NO_TALKER_ID
                    ? "Radio Can't use"
                    : "Radio Occupied",
                _ => "Radio Waiting"
            };
        }
    }
}
