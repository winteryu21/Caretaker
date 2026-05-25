using UnityEngine;

namespace Caretaker.World
{
    /// <summary>
    /// Future 월드의 변경 대상. Receiver ID와 상태 적용 어댑터를 가진다.
    /// 인과 규칙 실행 결과를 받아 오브젝트 상태를 변경한다. (문 열림, 전원 공급 등)
    /// </summary>
    /// <remarks>
    /// DSD §3.1 — 시간 인과 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class CausalReceiver : MonoBehaviour
    {
        [SerializeField] private string _receiverId;

        /// <summary>이 리시버의 고유 식별자.</summary>
        public string ReceiverId => _receiverId;

        /// <summary>
        /// 인과 결과에 따른 상태를 적용한다.
        /// </summary>
        /// <param name="stateKey">변경할 상태 키.</param>
        /// <param name="stateValue">적용할 상태 값.</param>
        public void ApplyState(string stateKey, string stateValue)
        {
            throw new System.NotImplementedException();
        }
    }
}
