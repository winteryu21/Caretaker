using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// 시간대 카메라를 Phase 3의 느린 플레이어 진행도에 맞춘다.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class TimelineCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform _pastPlayer;
        [SerializeField] private Transform _futurePlayer;

        private Vector3 _basePosition;
        private bool _isFollowing;

        /// <summary>두 플레이어 진행도 대상과 현재 시간대 카메라의 기준 위치를 설정한다.</summary>
        public void Configure(Transform pastPlayer, Transform futurePlayer, Vector3 basePosition)
        {
            _pastPlayer = pastPlayer;
            _futurePlayer = futurePlayer;
            _basePosition = basePosition;
            _isFollowing = true;
            ApplyProgress();
        }

        /// <summary>카메라 위치 덮어쓰기를 중지한다.</summary>
        public void StopFollowing()
        {
            _isFollowing = false;
            _pastPlayer = null;
            _futurePlayer = null;
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
        }
    }
}
