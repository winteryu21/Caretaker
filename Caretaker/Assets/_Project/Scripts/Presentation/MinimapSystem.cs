using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// 플레이어별 방문 룸만 표시하는 개인 미니맵 시스템.
    /// 정보 격리 규칙에 따라 상대 정보를 표시하지 않는다.
    /// </summary>
    /// <remarks>
    /// DSD §3.4 — 룸 / 카메라 / 미니맵 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class MinimapSystem : MonoBehaviour
    {
        // 1. Serialize 필드

        // 2. private 필드

        // 3. Unity 생명주기

        /// <summary>
        /// 방문한 룸을 미니맵에 표시한다.
        /// </summary>
        /// <param name="playerId">소유 플레이어 ID.</param>
        /// <param name="roomId">방문한 룸 ID.</param>
        public void RevealVisitedRoom(ulong playerId, string roomId)
        {
            throw new System.NotImplementedException();
        }

        // private 메서드
    }
}
