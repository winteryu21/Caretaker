using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// 실제 인과 변경 발생 시 양쪽 HUD에 추상 Pulse를 표시한다.
    /// Rule ID, Receiver ID, Room ID, 상태값 등 구체 정보는 표시하지 않는다.
    /// </summary>
    /// <remarks>
    /// DSD §3.10 — UI / HUD 시스템, §3.1 정보 공개 계약
    /// 계층: Unity Component
    /// </remarks>
    public class CausalityIndicatorPresenter : MonoBehaviour
    {
        // 1. Serialize 필드

        // 2. private 필드

        // 3. Unity 생명주기

        /// <summary>
        /// 인과 Pulse 애니메이션을 표시한다.
        /// </summary>
        public void ShowCausalityPulse()
        {
            throw new System.NotImplementedException();
        }

        // private 메서드
    }
}
