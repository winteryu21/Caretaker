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
        private const string VISION_OBJECT_NAME = "VisionArea";
        private const string VISION_SHADER_NAME = "Sprites/Default";

        [Header("Detection")]
        [SerializeField] private EnemyTuningSO _tuning;
        [SerializeField] private LayerMask _obstructionLayers = Physics2D.DefaultRaycastLayers;
        [SerializeField] private Vector2 _eyeOffset = Vector2.zero;

        [Header("Vision Display")]
        [SerializeField] private bool _showVisionArea = true;
        [SerializeField] private Color _patrolVisionColor = new(0f, 0.4f, 1f, 0.25f);
        [SerializeField] private Color _chaseVisionColor = new(1f, 0f, 0f, 0.25f);
        [SerializeField] private Color _searchVisionColor = new(1f, 0.45f, 0f, 0.25f);
        [SerializeField] [Range(3, 64)] private int _visionSegments = 24;
        [SerializeField] private int _visionSortingOrder = -1;

        private readonly RaycastHit2D[] _obstructionHits = new RaycastHit2D[8];

        private Vector2 _facingDirection = Vector2.right;
        private EnemyStateMachine.EnemyState _visionState = EnemyStateMachine.EnemyState.Patrol;
        private GameObject _visionObject;
        private Material _visionMaterial;
        private Mesh _visionMesh;
        private MeshRenderer _visionRenderer;
        private Vector3[] _visionVertices;
        private int[] _visionTriangles;

        private void Awake()
        {
            CreateVisionArea();
            RefreshVisionArea();
        }

        private void OnDestroy()
        {
            if (_visionMesh != null)
            {
                DestroyVisionResource(_visionMesh);
            }

            if (_visionMaterial != null)
            {
                DestroyVisionResource(_visionMaterial);
            }
        }

        private void OnValidate()
        {
            _visionSegments = Mathf.Clamp(_visionSegments, 3, 64);

            if (Application.isPlaying)
            {
                RefreshVisionArea();
            }
        }

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
            RefreshVisionArea();
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

            Vector2 normalizedDirection = direction.normalized;
            if ((_facingDirection - normalizedDirection).sqrMagnitude <= MIN_FACING_SQR_MAGNITUDE)
            {
                return;
            }

            _facingDirection = normalizedDirection;
            RefreshVisionArea();
        }

        /// <summary>
        /// Changes the vision area color to match the enemy's current behavior state.
        /// </summary>
        /// <param name="state">Current enemy behavior state.</param>
        public void SetVisionState(EnemyStateMachine.EnemyState state)
        {
            if (_visionState == state)
            {
                return;
            }

            _visionState = state;
            RefreshVisionColor();
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

        private void CreateVisionArea()
        {
            if (_visionObject != null)
            {
                return;
            }

            Shader shader = Shader.Find(VISION_SHADER_NAME);
            if (shader == null)
            {
                Debug.LogWarning($"{nameof(EnemyPerception2D)} could not find shader '{VISION_SHADER_NAME}'.", this);
                return;
            }

            _visionObject = new GameObject(VISION_OBJECT_NAME);
            _visionObject.transform.SetParent(transform, false);

            MeshFilter meshFilter = _visionObject.AddComponent<MeshFilter>();
            _visionRenderer = _visionObject.AddComponent<MeshRenderer>();

            _visionMesh = new Mesh
            {
                name = $"{name} Vision Area"
            };
            _visionMesh.MarkDynamic();
            meshFilter.sharedMesh = _visionMesh;

            _visionMaterial = new Material(shader)
            {
                name = $"{name} Vision Material",
                color = GetVisionColor()
            };
            _visionRenderer.sharedMaterial = _visionMaterial;
        }

        private void RefreshVisionArea()
        {
            if (_visionObject == null)
            {
                if (!Application.isPlaying)
                {
                    return;
                }

                CreateVisionArea();
            }

            if (_visionObject == null || _visionMesh == null || _visionRenderer == null)
            {
                return;
            }

            bool shouldShow = _showVisionArea && _tuning != null && _tuning.SightDistance > 0f;
            _visionObject.SetActive(shouldShow);
            if (!shouldShow)
            {
                return;
            }

            RefreshVisionColor();
            _visionRenderer.sortingOrder = _visionSortingOrder;
            BuildVisionMesh();
        }

        private void RefreshVisionColor()
        {
            if (_visionMaterial != null)
            {
                _visionMaterial.color = GetVisionColor();
            }
        }

        private Color GetVisionColor()
        {
            return _visionState switch
            {
                EnemyStateMachine.EnemyState.Chase => _chaseVisionColor,
                EnemyStateMachine.EnemyState.Search => _searchVisionColor,
                EnemyStateMachine.EnemyState.Alert => _searchVisionColor,
                _ => _patrolVisionColor
            };
        }

        private void BuildVisionMesh()
        {
            int segmentCount = Mathf.Clamp(_visionSegments, 3, 64);
            EnsureVisionBuffers(segmentCount);

            Vector2 origin = (Vector2)transform.position + _eyeOffset;
            _visionVertices[0] = transform.InverseTransformPoint(origin);

            float sightDistance = Mathf.Max(0f, _tuning.SightDistance);
            float halfFov = Mathf.Clamp(_tuning.FovDegrees * 0.5f, 0f, 180f);
            float startAngle = Mathf.Atan2(_facingDirection.y, _facingDirection.x) * Mathf.Rad2Deg - halfFov;
            float angleStep = halfFov * 2f / segmentCount;

            for (int i = 0; i <= segmentCount; i++)
            {
                float angle = (startAngle + angleStep * i) * Mathf.Deg2Rad;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 worldPoint = origin + direction * sightDistance;
                _visionVertices[i + 1] = transform.InverseTransformPoint(worldPoint);
            }

            _visionMesh.Clear();
            _visionMesh.vertices = _visionVertices;
            _visionMesh.triangles = _visionTriangles;
            _visionMesh.RecalculateBounds();
        }

        private void EnsureVisionBuffers(int segmentCount)
        {
            int vertexCount = segmentCount + 2;
            int triangleIndexCount = segmentCount * 3;
            if (_visionVertices != null
                && _visionVertices.Length == vertexCount
                && _visionTriangles != null
                && _visionTriangles.Length == triangleIndexCount)
            {
                return;
            }

            _visionVertices = new Vector3[vertexCount];
            _visionTriangles = new int[triangleIndexCount];

            for (int i = 0; i < segmentCount; i++)
            {
                int triangleIndex = i * 3;
                _visionTriangles[triangleIndex] = 0;
                _visionTriangles[triangleIndex + 1] = i + 1;
                _visionTriangles[triangleIndex + 2] = i + 2;
            }
        }

        private static void DestroyVisionResource(Object resource)
        {
            if (Application.isPlaying)
            {
                Destroy(resource);
                return;
            }

            DestroyImmediate(resource);
        }
    }
}
