using Caretaker.Shared;
using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// 두 시간대 카메라를 느린 플레이어 진행도에 맞추고 플레이어 간 최대 거리를 관리합니다.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class TimelineCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform _pastPlayer;
        [SerializeField] private Transform _futurePlayer;
        [SerializeField] private Transform _verticalFollowTarget;

        private Vector3 _basePosition;
        private TimelineRole _cameraTimelineRole;
        private PlayerMotor2D _futureMotor;
        private PlayerMotor2D _pastMotor;
        private float _futureProgressOriginX;
        private float _maximumPlayerSeparation;
        private float _pastProgressOriginX;
        private float _verticalOffset;
        private bool _controlsPlayerSpacing;
        private bool _isFollowing;

        /// <summary>두 플레이어와 현재 시간대 카메라의 기준 위치를 설정합니다.</summary>
        public void Configure(
            Transform pastPlayer,
            Transform futurePlayer,
            Vector3 basePosition,
            bool controlsPlayerSpacing = false,
            float maximumPlayerSeparation = 0f,
            TimelineRole cameraTimelineRole = TimelineRole.None,
            float pastProgressOriginX = 0f,
            float futureProgressOriginX = 0f)
        {
            Configure(
                pastPlayer,
                futurePlayer,
                null,
                basePosition,
                controlsPlayerSpacing,
                maximumPlayerSeparation,
                cameraTimelineRole,
                pastProgressOriginX,
                futureProgressOriginX);
        }

        /// <summary>두 플레이어와 현재 시간대 카메라의 기준 위치, 세로 추적 대상을 설정합니다.</summary>
        public void Configure(
            Transform pastPlayer,
            Transform futurePlayer,
            Transform verticalFollowTarget,
            Vector3 basePosition,
            bool controlsPlayerSpacing = false,
            float maximumPlayerSeparation = 0f,
            TimelineRole cameraTimelineRole = TimelineRole.None,
            float pastProgressOriginX = 0f,
            float futureProgressOriginX = 0f)
        {
            if (_controlsPlayerSpacing)
            {
                ClearPlayerSpacingBounds();
            }

            _pastPlayer = pastPlayer;
            _futurePlayer = futurePlayer;
            _verticalFollowTarget = verticalFollowTarget;
            _basePosition = basePosition;
            _controlsPlayerSpacing = controlsPlayerSpacing;
            _maximumPlayerSeparation = Mathf.Max(0f, maximumPlayerSeparation);
            _cameraTimelineRole = cameraTimelineRole;
            _pastProgressOriginX = pastProgressOriginX;
            _futureProgressOriginX = futureProgressOriginX;
            _verticalOffset = _verticalFollowTarget != null
                ? _basePosition.y - _verticalFollowTarget.position.y
                : 0f;
            _pastMotor = _pastPlayer != null ? _pastPlayer.GetComponent<PlayerMotor2D>() : null;
            _futureMotor = _futurePlayer != null ? _futurePlayer.GetComponent<PlayerMotor2D>() : null;
            _isFollowing = true;
            ApplyProgress();
        }

        /// <summary>카메라 추적과 플레이어 간격 제한을 중지합니다.</summary>
        public void StopFollowing()
        {
            if (_controlsPlayerSpacing)
            {
                ClearPlayerSpacingBounds();
            }

            _isFollowing = false;
            _controlsPlayerSpacing = false;
            _pastPlayer = null;
            _futurePlayer = null;
            _verticalFollowTarget = null;
            _pastMotor = null;
            _futureMotor = null;
            _cameraTimelineRole = TimelineRole.None;
            _pastProgressOriginX = 0f;
            _futureProgressOriginX = 0f;
            _verticalOffset = 0f;
        }

        private void FixedUpdate()
        {
            ApplyPlayerSpacingBounds();
        }

        private void LateUpdate()
        {
            ApplyProgress();
        }

        private void ApplyProgress()
        {
            if (!_isFollowing || _pastPlayer == null || _futurePlayer == null)
            {
                return;
            }

            float sharedProgressX = ResolveSharedProgressX();
            transform.position = new Vector3(
                ResolveCameraWorldX(sharedProgressX),
                ResolveCameraWorldY(),
                _basePosition.z);

            ApplyPlayerSpacingBounds();
        }

        private float ResolveSharedProgressX()
        {
            if (!UsesTimelineProgress())
            {
                return Mathf.Min(_pastPlayer.position.x, _futurePlayer.position.x);
            }

            return Mathf.Min(
                ResolvePlayerProgressX(_pastPlayer, TimelineRole.Past),
                ResolvePlayerProgressX(_futurePlayer, TimelineRole.Future));
        }

        private float ResolveCameraWorldX(float sharedProgressX)
        {
            return !UsesTimelineProgress()
                ? sharedProgressX
                : ResolveTimelineWorldX(_cameraTimelineRole, sharedProgressX);
        }

        private void ApplyPlayerSpacingBounds()
        {
            if (!_controlsPlayerSpacing || _pastPlayer == null || _futurePlayer == null)
            {
                return;
            }

            if (UsesTimelineProgress())
            {
                float pastProgressX = ResolvePlayerProgressX(_pastPlayer, TimelineRole.Past);
                float futureProgressX = ResolvePlayerProgressX(_futurePlayer, TimelineRole.Future);
                if (_cameraTimelineRole == TimelineRole.Past)
                {
                    SetTimelineBounds(_pastMotor, TimelineRole.Past, futureProgressX);
                }
                else if (_cameraTimelineRole == TimelineRole.Future)
                {
                    SetTimelineBounds(_futureMotor, TimelineRole.Future, pastProgressX);
                }

                return;
            }

            if (_cameraTimelineRole == TimelineRole.Past)
            {
                _pastMotor?.SetHorizontalBounds(
                    _futurePlayer.position.x - _maximumPlayerSeparation,
                    _futurePlayer.position.x + _maximumPlayerSeparation);
            }
            else if (_cameraTimelineRole == TimelineRole.Future)
            {
                _futureMotor?.SetHorizontalBounds(
                    _pastPlayer.position.x - _maximumPlayerSeparation,
                    _pastPlayer.position.x + _maximumPlayerSeparation);
            }
        }

        private void SetTimelineBounds(PlayerMotor2D motor, TimelineRole timelineRole, float peerProgressX)
        {
            motor?.SetHorizontalBounds(
                ResolveTimelineWorldX(timelineRole, peerProgressX - _maximumPlayerSeparation),
                ResolveTimelineWorldX(timelineRole, peerProgressX + _maximumPlayerSeparation));
        }

        private void ClearPlayerSpacingBounds()
        {
            _pastMotor?.ClearHorizontalBounds();
            _futureMotor?.ClearHorizontalBounds();
        }

        private bool UsesTimelineProgress()
        {
            return _cameraTimelineRole is TimelineRole.Past or TimelineRole.Future;
        }

        private float ResolvePlayerProgressX(Transform player, TimelineRole timelineRole)
        {
            return player.position.x - ResolveOriginX(timelineRole);
        }

        private float ResolveTimelineWorldX(TimelineRole timelineRole, float progressX)
        {
            return ResolveOriginX(timelineRole) + progressX;
        }

        private float ResolveOriginX(TimelineRole timelineRole)
        {
            return timelineRole == TimelineRole.Future
                ? _futureProgressOriginX
                : _pastProgressOriginX;
        }

        private float ResolveCameraWorldY()
        {
            return _verticalFollowTarget != null
                ? _verticalFollowTarget.position.y + _verticalOffset
                : _basePosition.y;
        }
    }
}
