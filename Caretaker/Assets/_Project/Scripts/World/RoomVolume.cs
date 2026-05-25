using UnityEngine;

namespace Caretaker.World
{
    /// <summary>
    /// 룸 진입/이탈을 감지하는 Trigger Collider.
    /// RoomManager에 진입/이탈 이벤트를 보고한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.4 — 룸 / 카메라 / 미니맵 시스템
    /// 계층: Unity Component
    /// </remarks>
    [RequireComponent(typeof(Collider2D))]
    public class RoomVolume : MonoBehaviour
    {
        [SerializeField] private string _roomId;

        /// <summary>이 볼륨이 나타내는 룸의 고유 식별자.</summary>
        public string RoomId => _roomId;

        // Unity 생명주기 (OnTriggerEnter2D / OnTriggerExit2D)

        // private 메서드
    }
}
