using Caretaker.Shared;

namespace Caretaker.Core
{
    /// <summary>
    /// Phase 완료 조건과 다음 Phase를 계산한다.
    /// MonoBehaviour에 의존하지 않는 순수 C# 클래스.
    /// </summary>
    /// <remarks>
    /// DSD §3.9 — 게임 진행 / 체크포인트 시스템
    /// 계층: Domain Service
    /// </remarks>
    public class PhaseService
    {
        /// <summary>
        /// 주어진 Phase의 모든 Major Interaction이 완료되었는지 판정한다.
        /// </summary>
        /// <param name="phaseId">판정 대상 Phase.</param>
        /// <param name="completedMajorIds">완료된 Major Interaction ID 목록.</param>
        /// <returns>Phase 완료 여부.</returns>
        public bool IsPhaseComplete(PhaseId phaseId, string[] completedMajorIds)
        {
            if (completedMajorIds == null)
            {
                return false;
            }

            return phaseId switch
            {
                PhaseId.Phase1 => Contains(completedMajorIds, "M1") && Contains(completedMajorIds, "M2"),
                PhaseId.Phase2 => Contains(completedMajorIds, "M3") && Contains(completedMajorIds, "M4"),
                PhaseId.Phase3 => false,
                _ => false
            };
        }

        /// <summary>
        /// 완료된 Phase 다음으로 전환할 Phase를 반환한다.
        /// </summary>
        /// <param name="currentPhase">현재 Phase.</param>
        /// <returns>다음 Phase.</returns>
        public PhaseId GetNextPhase(PhaseId currentPhase)
        {
            return currentPhase switch
            {
                PhaseId.Phase1 => PhaseId.Phase2,
                PhaseId.Phase2 => PhaseId.Phase3,
                PhaseId.Phase3 => PhaseId.Phase3,
                _ => currentPhase
            };
        }

        private static bool Contains(string[] values, string expectedValue)
        {
            foreach (string value in values)
            {
                if (value == expectedValue)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
