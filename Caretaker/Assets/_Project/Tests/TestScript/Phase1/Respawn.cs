using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// Returns a player touching this object to the Phase 1 scene start point.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Respawn : MonoBehaviour
    {
        private const string DEFAULT_START_POINT_NAME = "SceneStartPoint";

        [Header("Respawn")]
        [SerializeField] private Transform _startPoint;
        [SerializeField] private string _startPointName = DEFAULT_START_POINT_NAME;

        private void Awake()
        {
            ResolveStartPoint();
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_startPointName))
            {
                _startPointName = DEFAULT_START_POINT_NAME;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            RespawnPlayer(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            RespawnPlayer(collision.collider);
        }

        /// <summary>
        /// Returns the player associated with the collider to the configured start point.
        /// </summary>
        /// <param name="other">A collider belonging to the player.</param>
        public void RespawnPlayer(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                return;
            }

            if (_startPoint == null)
            {
                ResolveStartPoint();
            }

            if (_startPoint == null)
            {
                Debug.LogWarning(
                    $"Respawn failed: '{_startPointName}' was not found in scene '{gameObject.scene.name}'.",
                    this);
                return;
            }

            Vector2 startPosition = _startPoint.position;
            if (player.TryGetComponent(out Rigidbody2D rigidbody2D))
            {
                rigidbody2D.linearVelocity = Vector2.zero;
                rigidbody2D.angularVelocity = 0f;
                rigidbody2D.position = startPosition;
            }

            player.transform.position = startPosition;
            Physics2D.SyncTransforms();
        }

        private void ResolveStartPoint()
        {
            if (_startPoint != null)
            {
                return;
            }

            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
            {
                Transform[] children = rootObjects[rootIndex].GetComponentsInChildren<Transform>(true);
                for (int childIndex = 0; childIndex < children.Length; childIndex++)
                {
                    if (children[childIndex].name == _startPointName)
                    {
                        _startPoint = children[childIndex];
                        return;
                    }
                }
            }
        }
    }
}
