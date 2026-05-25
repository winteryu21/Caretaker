using UnityEngine;
using Unity.Netcode;

using Caretaker.Shared;

namespace Caretaker.Core
{
    /// <summary>
    /// Phase 전환, Major Interaction 완료 판정, 공동 실패 처리를 관리한다.
    /// Host Authority 기반으로 게임 상태 전이와 RPC 전파를 담당한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.9 — 게임 진행 / 체크포인트 시스템
    /// 계층: Network Boundary / Unity Component
    /// </remarks>
    public class GameFlowManager : NetworkBehaviour
    {
        // 1. 상수

        // 2. Serialize 필드

        // 3. private 필드

        // 4. 프로퍼티

        // 5. 이벤트

        // 6. Unity 생명주기

        // 7. public 메서드

        /// <summary>
        /// Major Interaction 완료를 수신하고 Phase 완료 조건을 검사한다.
        /// </summary>
        /// <param name="phaseId">현재 Phase.</param>
        /// <param name="majorId">완료된 Major Interaction ID.</param>
        public void NotifyMajorComplete(PhaseId phaseId, string majorId)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 공동 실패 후 체크포인트 복귀를 실행한다.
        /// </summary>
        /// <param name="reason">실패 사유.</param>
        public void RollbackToCheckpoint(string reason)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 대상 Phase로 전환한다.
        /// </summary>
        /// <param name="targetPhase">전환 대상 Phase.</param>
        public void TransitionPhase(PhaseId targetPhase)
        {
            throw new System.NotImplementedException();
        }

        // 8. private 메서드
    }
}
