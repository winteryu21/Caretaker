using System;

using UnityEngine;

namespace Caretaker.World
{
    /// <summary>
    /// 룸 전환 이벤트 발행, 룸 활성화/비활성화, 스폰 위치 제공을 담당한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.4 — 룸 / 카메라 / 미니맵 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class RoomManager : MonoBehaviour
    {
        /// <summary>룸 전환 시 발생한다. (playerId, newRoomId)</summary>
        public event Action<ulong, string> OnRoomChanged;

        // 1. Serialize 필드

        // 2. private 필드

        // 3. Unity 생명주기

        /// <summary>
        /// 플레이어의 룸 진입을 처리한다.
        /// </summary>
        /// <param name="playerId">진입한 플레이어 ID.</param>
        /// <param name="roomId">진입한 룸 ID.</param>
        public void ReportRoomEnter(ulong playerId, string roomId)
        {
            throw new System.NotImplementedException();
        }

        // private 메서드
    }
}
