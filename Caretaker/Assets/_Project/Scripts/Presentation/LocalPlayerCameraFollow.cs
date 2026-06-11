using Caretaker.Gameplay;
using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// Keeps the active camera centered on the local world player.
    /// </summary>
    /// <remarks>DSD §3.4 - temporary local camera follow until room-bounded camera is wired.</remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Camera))]
    public sealed class LocalPlayerCameraFollow : MonoBehaviour
    {
        [SerializeField] private LocalWorldPlayerSpawner _playerSpawner;
        [SerializeField] private Vector3 _offset = new(0f, 1.5f, -10f);
        [SerializeField] private float _smoothTime = 0.12f;
        [SerializeField] private float _snapDistance = 8f;

        private Vector3 _velocity;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void LateUpdate()
        {
            Transform target = _playerSpawner != null && _playerSpawner.CurrentPlayer != null
                ? _playerSpawner.CurrentPlayer.transform
                : null;

            if (target == null)
            {
                return;
            }

            Vector3 targetPosition = target.position + _offset;
            if (_snapDistance > 0f && Vector3.SqrMagnitude(transform.position - targetPosition) > _snapDistance * _snapDistance)
            {
                _velocity = Vector3.zero;
                transform.position = targetPosition;
                return;
            }

            transform.position = _smoothTime > 0f
                ? Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _smoothTime)
                : targetPosition;
        }

        private void ResolveDependencies()
        {
            if (_playerSpawner == null)
            {
                _playerSpawner = FindAnyObjectByType<LocalWorldPlayerSpawner>();
            }
        }
    }
}
