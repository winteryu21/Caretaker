using UnityEngine;

namespace Caretaker.World
{
    /// <summary>
    /// Past 월드의 클릭/사용 대상. Trigger ID와 상호작용 타입을 가진다.
    /// 플레이어 상호작용을 감지하고 CausalityManager에 트리거 요청을 전달한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.1 — 시간 인과 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class CausalTrigger : MonoBehaviour
    {
        [SerializeField] private string _triggerId;

        /// <summary>이 트리거의 고유 식별자.</summary>
        public string TriggerId => _triggerId;

        // Unity 생명주기

        // public 메서드

        // private 메서드
    }
}
