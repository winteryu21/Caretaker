using Caretaker.Shared;

namespace Caretaker.Core
{
    /// <summary>
    /// NetworkRole(Host/Client)과 TimelineRole(Past/Future)의 매핑을 관리한다.
    /// 두 역할은 독립적이다.
    /// </summary>
    /// <remarks>
    /// DSD §3.2 — 네트워크 동기화 시스템
    /// 계층: Domain Service / Runtime State
    /// </remarks>
    public class SessionRoleManager
    {
        /// <summary>
        /// 플레이어에게 시간대 역할을 배정한다.
        /// </summary>
        /// <param name="playerId">플레이어 식별자.</param>
        /// <param name="role">배정할 시간대 역할.</param>
        public void AssignTimelineRole(ulong playerId, TimelineRole role)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 플레이어의 시간대 역할을 조회한다.
        /// </summary>
        /// <param name="playerId">플레이어 식별자.</param>
        /// <returns>배정된 시간대 역할.</returns>
        public TimelineRole GetTimelineRole(ulong playerId)
        {
            throw new System.NotImplementedException();
        }
    }
}
