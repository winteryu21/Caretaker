using System.Collections.Generic;

namespace Caretaker.World
{
    /// <summary>
    /// 현재 룸, 방문 룸, 인접 룸 계산을 담당한다.
    /// MonoBehaviour에 의존하지 않는 순수 C# 클래스.
    /// </summary>
    /// <remarks>
    /// DSD §3.4 — 룸 / 카메라 / 미니맵 시스템
    /// 계층: Domain Service
    /// </remarks>
    public class RoomService
    {
        /// <summary>
        /// 룸 그래프에서 인접 룸 ID 목록을 조회한다.
        /// </summary>
        /// <param name="roomId">기준 룸 ID.</param>
        /// <returns>인접 룸 ID 목록.</returns>
        public IReadOnlyList<string> GetAdjacentRooms(string roomId)
        {
            throw new System.NotImplementedException();
        }
    }
}
