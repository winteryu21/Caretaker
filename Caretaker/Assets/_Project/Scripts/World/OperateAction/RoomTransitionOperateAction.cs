using System.Collections;

using UnityEngine;

using Caretaker.Gameplay;
using Caretaker.Presentation;
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

        [Header("Screen Fade")]
        [SerializeField] private bool _useScreenFade = true;
        [SerializeField] [Min(0f)] private float _fadeOutSeconds = 0.1f;
        [SerializeField] [Min(0f)] private float _fadeHoldSeconds = 0.04f;
        [SerializeField] [Min(0f)] private float _fadeInSeconds = 0.16f;

        private bool _isTransitioning;

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
            if (_isTransitioning)
            {
                return false;
            }

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

            StartCoroutine(TransitionActorRoutine(actor));
            return true;
        }

        private IEnumerator TransitionActorRoutine(PlayerController actor)
        {
            _isTransitioning = true;
            actor.SetInputBlocked(true);

            if (_useScreenFade)
            {
                yield return ScreenFadePresenter.GetOrCreate().FadeOut(_fadeOutSeconds);
            }

            if (actor == null || _destination == null)
            {
                if (_useScreenFade)
                {
                    yield return ScreenFadePresenter.GetOrCreate().FadeIn(_fadeInSeconds);
                }

                _isTransitioning = false;
                yield break;
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

            if (_useScreenFade && _fadeHoldSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(_fadeHoldSeconds);
            }

            if (_useScreenFade)
            {
                yield return ScreenFadePresenter.GetOrCreate().FadeIn(_fadeInSeconds);
            }

            actor.SetInputBlocked(false);
            _isTransitioning = false;
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
