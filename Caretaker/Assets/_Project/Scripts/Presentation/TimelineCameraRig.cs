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

        private Vector3 _basePosition;
        private PlayerMotor2D _futureMotor;
        private PlayerMotor2D _pastMotor;
        private float _maximumPlayerSeparation;
        private bool _controlsPlayerSpacing;
        private bool _isFollowing;

        /// <summary>두 플레이어와 현재 시간대 카메라의 기준 위치를 설정합니다.</summary>
        public void Configure(
            Transform pastPlayer,
            Transform futurePlayer,
            Vector3 basePosition,
            bool controlsPlayerSpacing = false,
            float maximumPlayerSeparation = 0f)
        {
            _pastPlayer = pastPlayer;
            _futurePlayer = futurePlayer;
            _basePosition = basePosition;
            _controlsPlayerSpacing = controlsPlayerSpacing;
            _maximumPlayerSeparation = Mathf.Max(0f, maximumPlayerSeparation);
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
                _pastMotor?.ClearHorizontalBounds();
                _futureMotor?.ClearHorizontalBounds();
            }

            _isFollowing = false;
            _controlsPlayerSpacing = false;
            _pastPlayer = null;
            _futurePlayer = null;
            _pastMotor = null;
            _futureMotor = null;
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

            float sharedProgressX = Mathf.Min(_pastPlayer.position.x, _futurePlayer.position.x);
            transform.position = new Vector3(
                sharedProgressX,
                _basePosition.y,
                _basePosition.z);

            ApplyPlayerSpacingBounds();
        }

        private void ApplyPlayerSpacingBounds()
        {
            if (!_controlsPlayerSpacing)
            {
                return;
            }

            _pastMotor?.SetHorizontalBounds(
                _futurePlayer.position.x - _maximumPlayerSeparation,
                _futurePlayer.position.x + _maximumPlayerSeparation);
            _futureMotor?.SetHorizontalBounds(
                _pastPlayer.position.x - _maximumPlayerSeparation,
                _pastPlayer.position.x + _maximumPlayerSeparation);
        }
    }
}
