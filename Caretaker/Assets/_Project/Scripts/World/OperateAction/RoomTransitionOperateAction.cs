using UnityEngine;

using Caretaker.Gameplay;
using Caretaker.Shared;

namespace Caretaker.World
{
    /// <summary>
    /// 플레이어를 지정된 룸 진입 위치로 이동시키는 일반 조작입니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InteractableObject))]
    public sealed class RoomTransitionOperateAction : MonoBehaviour, IOperateAction
    {
        [Header("Destination")]
        [SerializeField] private Transform _destination;
        [SerializeField] private string _targetRoomId;
        [SerializeField] private RoomManager _roomManager;

        private void Awake()
        {
            EnsureOperateInteractionType();

            if (_roomManager == null)
            {
                _roomManager = FindAnyObjectByType<RoomManager>();
            }
        }

        private void Reset()
        {
            EnsureOperateInteractionType();
        }

        private void OnValidate()
        {
            _targetRoomId = _targetRoomId?.Trim();
            EnsureOperateInteractionType();
        }

        /// <summary>
        /// 플레이어를 목표 위치로 이동시키고 대상 룸 진입을 보고합니다.
        /// </summary>
        /// <param name="actor">이동할 플레이어입니다.</param>
        /// <returns>목표 위치로 이동했으면 true입니다.</returns>
        public bool Execute(PlayerController actor)
        {
            if (actor == null)
            {
                Debug.LogWarning("Room transition failed: actor is missing.", this);
                return false;
            }

            if (_destination == null)
            {
                Debug.LogWarning("Room transition failed: destination is missing.", this);
                return false;
            }

            Vector3 destinationPosition = _destination.position;
            if (actor.TryGetComponent(out Rigidbody2D rigidbody2D))
            {
                rigidbody2D.linearVelocity = Vector2.zero;
                rigidbody2D.position = destinationPosition;
            }

            actor.transform.position = destinationPosition;
            Physics2D.SyncTransforms();
            ReportRoomEnter(actor);
            return true;
        }

        private void EnsureOperateInteractionType()
        {
            if (TryGetComponent(out InteractableObject interactableObject))
            {
                interactableObject.EnsureInteractionType(InteractionType.Operate);
            }
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
