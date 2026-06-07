using UnityEngine;

using Caretaker.Gameplay;

namespace Caretaker.World
{
    /// <summary>
    /// 플레이어를 지정된 룸 진입 위치로 이동시키는 일반 조작입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomTransitionOperateAction : MonoBehaviour, IOperateAction
    {
        [Header("Destination")]
        [SerializeField] private Transform _destination;
        [SerializeField] private string _targetRoomId;
        [SerializeField] private RoomManager _roomManager;

        private void Awake()
        {
            if (_roomManager == null)
            {
                _roomManager = FindAnyObjectByType<RoomManager>();
            }
        }

        private void OnValidate()
        {
            _targetRoomId = _targetRoomId?.Trim();
        }

        /// <summary>
        /// 플레이어를 목표 위치로 이동시키고 대상 룸 진입을 보고합니다.
        /// </summary>
        /// <param name="actor">이동할 플레이어입니다.</param>
        /// <returns>목표 위치로 이동했으면 true입니다.</returns>
        public bool Execute(PlayerController actor)
        {
            if (actor == null || _destination == null)
            {
                return false;
            }

            if (actor.TryGetComponent(out Rigidbody2D rigidbody2D))
            {
                rigidbody2D.linearVelocity = Vector2.zero;
                rigidbody2D.position = _destination.position;
            }
            else
            {
                actor.transform.position = _destination.position;
            }

            ReportRoomEnter(actor);
            return true;
        }

        private void ReportRoomEnter(PlayerController actor)
        {
            if (_roomManager == null || string.IsNullOrWhiteSpace(_targetRoomId))
            {
                return;
            }

            RoomParticipant participant = actor.GetComponentInParent<RoomParticipant>();
            if (participant != null)
            {
                _roomManager.ReportRoomEnter(participant.PlayerId, _targetRoomId);
            }
        }
    }
}
