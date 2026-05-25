using System.Collections.Generic;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 현재 방과 인접 방 경보 대상을 계산한다.
    /// MonoBehaviour에 의존하지 않는 순수 C# 클래스.
    /// </summary>
    /// <remarks>
    /// DSD §3.5 — AI / 경보 시스템
    /// 계층: Domain Service
    /// </remarks>
    public class AlertService
    {
        /// <summary>
        /// 현재 방과 인접 방에 경보를 전파한다.
        /// </summary>
        /// <param name="sourceRoomId">감지가 발생한 룸 ID.</param>
        /// <returns>경보 대상 룸 ID 목록.</returns>
        public IReadOnlyList<string> RaiseAlert(string sourceRoomId)
        {
            throw new System.NotImplementedException();
        }
    }
}
