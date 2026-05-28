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
        [SerializeField] private RoomManager _roomManager;

        private Collider2D _collider2D;

        /// <summary>이 볼륨이 나타내는 룸의 고유 식별자.</summary>
        public string RoomId => _roomId;

        // Unity 생명주기 (OnTriggerEnter2D / OnTriggerExit2D)
        private void Awake()
        {
            _collider2D = GetComponent<Collider2D>();

            if (_roomManager == null)
            {
                _roomManager = GetComponentInParent<RoomManager>();
            }

            if (_collider2D != null && !_collider2D.isTrigger)
            {
                Debug.LogWarning($"RoomVolume '{name}' requires a trigger Collider2D.", this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!TryResolveParticipant(other, out RoomParticipant participant))
            {
                return;
            }

            _roomManager.ReportRoomEnter(participant.PlayerId, _roomId);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!TryResolveParticipant(other, out RoomParticipant participant))
            {
                return;
            }

            _roomManager.ReportRoomExit(participant.PlayerId, _roomId);
        }

        // private 메서드
        private bool TryResolveParticipant(Collider2D other, out RoomParticipant participant)
        {
            participant = null;

            if (_roomManager == null)
            {
                Debug.LogWarning($"RoomVolume '{name}' has no RoomManager.", this);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_roomId) || other == null)
            {
                return false;
            }

            participant = other.GetComponentInParent<RoomParticipant>();
            return participant != null;
        }
    }
}
