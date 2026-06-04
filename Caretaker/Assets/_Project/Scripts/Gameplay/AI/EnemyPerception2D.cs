using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 거리, FOV, 웅크리기 거리 보정, 장애물 Raycast로 적의 시야 감지를 판정한다.
    /// </summary>
    /// <remarks>
    /// DSD 3.5 AI / 경보 시스템의 Unity 컴포넌트.
    /// </remarks>
    public class EnemyPerception2D : MonoBehaviour
    {
        private const float CROUCH_DETECTION_RANGE_MULTIPLIER = 0.5f;
        private const float MIN_FACING_SQR_MAGNITUDE = 0.0001f;

        [SerializeField] private EnemyTuningSO _tuning;
        [SerializeField] private LayerMask _obstructionLayers = Physics2D.DefaultRaycastLayers;
        [SerializeField] private Vector2 _eyeOffset = Vector2.zero;

        private readonly RaycastHit2D[] _obstructionHits = new RaycastHit2D[8];

        private Vector2 _facingDirection = Vector2.right;

        /// <summary>
        /// 적 시야각의 중심으로 사용할 월드 방향.
        /// </summary>
        public Vector2 FacingDirection => _facingDirection;

        /// <summary>
        /// 시야 판정에 사용할 튜닝 에셋을 설정한다.
        /// </summary>
        /// <param name="tuning">적 튜닝 데이터.</param>
        public void SetTuning(EnemyTuningSO tuning)
        {
            _tuning = tuning;
        }

        /// <summary>
        /// FOV 판정에 사용할 정면 방향을 설정한다.
        /// </summary>
        /// <param name="direction">월드 기준 바라보는 방향.</param>
        public void SetFacingDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude <= MIN_FACING_SQR_MAGNITUDE)
            {
                return;
            }

            _facingDirection = direction.normalized;
        }

        /// <summary>
        /// 거리, FOV, 웅크리기, 차폐 조건을 검사해 플레이어가 보이는지 반환한다.
        /// </summary>
        /// <param name="playerPosition">플레이어 월드 위치.</param>
        /// <param name="isCrouching">플레이어가 웅크리고 있는지 여부.</param>
        /// <returns>현재 플레이어가 보이면 true.</returns>
        public bool EvaluateSight(Vector2 playerPosition, bool isCrouching)
        {
            return EvaluateSight(playerPosition, isCrouching, null);
        }

        /// <summary>
        /// 거리, FOV, 웅크리기, 차폐 조건을 검사해 플레이어가 보이는지 반환한다.
        /// </summary>
        /// <param name="playerPosition">플레이어 월드 위치.</param>
        /// <param name="isCrouching">플레이어가 웅크리고 있는지 여부.</param>
        /// <param name="ignoredCollider">차폐물로 취급하지 않을 플레이어 콜라이더.</param>
        /// <returns>현재 플레이어가 보이면 true.</returns>
        public bool EvaluateSight(Vector2 playerPosition, bool isCrouching, Collider2D ignoredCollider)
        {
            if (_tuning == null)
            {
                return false;
            }

            Vector2 origin = (Vector2)transform.position + _eyeOffset;
            Vector2 toPlayer = playerPosition - origin;
            float distanceToPlayer = toPlayer.magnitude;
            if (distanceToPlayer <= 0f)
            {
                return true;
            }

            float sightDistance = Mathf.Max(0f, _tuning.SightDistance);
            if (isCrouching)
            {
                sightDistance *= CROUCH_DETECTION_RANGE_MULTIPLIER;
            }

            if (distanceToPlayer > sightDistance)
            {
                return false;
            }

            float halfFov = Mathf.Max(0f, _tuning.FovDegrees) * 0.5f;
            if (Vector2.Angle(_facingDirection, toPlayer) > halfFov)
            {
                return false;
            }

            return !IsSightObstructed(origin, toPlayer.normalized, distanceToPlayer, ignoredCollider);
        }

        private bool IsSightObstructed(Vector2 origin, Vector2 direction, float distance, Collider2D ignoredCollider)
        {
            ContactFilter2D contactFilter = new()
            {
                useLayerMask = true,
                layerMask = _obstructionLayers,
                useTriggers = false
            };

            int hitCount = Physics2D.Raycast(origin, direction, contactFilter, _obstructionHits, distance);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hitCollider = _obstructionHits[i].collider;
                if (hitCollider != null && hitCollider.transform != transform && hitCollider != ignoredCollider)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
