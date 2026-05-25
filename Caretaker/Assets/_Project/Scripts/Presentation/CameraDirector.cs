using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// 룸 전환 시 카메라가 해당 룸 영역으로 이동하는 시스템.
    /// RoomManager의 RoomChanged 이벤트를 구독한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.4 — 룸 / 카메라 / 미니맵 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class CameraDirector : MonoBehaviour
    {
        // 1. Serialize 필드

        // 2. private 필드

        // 3. Unity 생명주기

        /// <summary>
        /// 지정된 룸으로 카메라를 전환한다.
        /// </summary>
        /// <param name="roomId">포커스할 룸 ID.</param>
        public void FocusRoom(string roomId)
        {
            throw new System.NotImplementedException();
        }

        // private 메서드
    }
}
