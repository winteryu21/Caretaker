using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// Keeps a timeline camera aligned to the slower Phase 3 player.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class TimelineCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform _pastPlayer;
        [SerializeField] private Transform _futurePlayer;

        private Vector3 _basePosition;
        private bool _isFollowing;

        /// <summary>Configures the two progress targets and this timeline's camera origin.</summary>
        public void Configure(Transform pastPlayer, Transform futurePlayer, Vector3 basePosition)
        {
            _pastPlayer = pastPlayer;
            _futurePlayer = futurePlayer;
            _basePosition = basePosition;
            _isFollowing = true;
            ApplyProgress();
        }

        /// <summary>Stops overriding this camera's position.</summary>
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
