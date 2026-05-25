using System.Collections.Generic;

using Caretaker.Shared;

namespace Caretaker.World
{
    /// <summary>
    /// 인과 조건 검증, 규칙 실행, 완료 상태 계산을 담당한다.
    /// MonoBehaviour에 의존하지 않는 순수 C# 클래스.
    /// </summary>
    /// <remarks>
    /// DSD §3.1 — 시간 인과 시스템
    /// 계층: Domain Service
    /// </remarks>
    public class CausalityService
    {
        /// <summary>
        /// 트리거 요청을 검증하고 규칙을 실행한다.
        /// 거리, 역할, 페이즈, 필요 아이템 조건을 검증한다.
        /// </summary>
        /// <param name="triggerId">활성화된 트리거 ID.</param>
        /// <param name="actorRole">요청자의 시간대 역할.</param>
        /// <param name="ownedItemIds">요청자 소지 아이템 ID 목록.</param>
        /// <returns>인과 처리 결과.</returns>
        public CausalResult SubmitTrigger(string triggerId, TimelineRole actorRole, IReadOnlyList<string> ownedItemIds)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 주어진 Phase의 Major Interaction 완료 여부를 조회한다.
        /// </summary>
        /// <param name="phaseId">조회 대상 Phase.</param>
        /// <returns>완료 여부.</returns>
        public bool QueryMajorProgress(PhaseId phaseId)
        {
            throw new System.NotImplementedException();
        }
    }
}
