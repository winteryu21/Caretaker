using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 적의 순찰, 추격, 탐색, 바라보는 방향을 제어한다.
    /// </summary>
    /// <remarks>
    /// DSD 3.5 AI / 경보 시스템의 Unity 컴포넌트.
    /// </remarks>
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(EnemyPerception2D))]
    public class EnemyController : MonoBehaviour
    {
        private const float ARRIVAL_DISTANCE = 0.05f;
        private const float FACING_EPSILON = 0.001f;
        private const float PROGRESS_EPSILON = 0.005f;

        private enum EnemyMovementMode
        {
            Grounded,
            Flying
        }

        [SerializeField] private EnemyMovementMode _movementMode = EnemyMovementMode.Grounded;
        [SerializeField] private EnemyTuningSO _tuning;
        [SerializeField] private Transform[] _patrolWaypoints;
        [SerializeField] private PlayerMotor2D _targetPlayer;
        [SerializeField] private LayerMask _groundLayers = Physics2D.DefaultRaycastLayers;
        [SerializeField] [Min(0f)] private float _searchPatrolRadius = 1.5f;
        [SerializeField] [Min(0f)] private float _groundProbeForwardDistance = 0.15f;
        [SerializeField] [Min(0.01f)] private float _groundProbeDownDistance = 0.4f;
        [SerializeField] [Min(0.1f)] private float _stuckSkipSeconds = 1f;

        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[4];
        private readonly EnemyStateMachine _stateMachine = new();

        private Collider2D _collider2D;
        private Collider2D _targetCollider;
        private EnemyPerception2D _perception;
        private Rigidbody2D _rigidbody2D;
        private Vector2 _searchCenterPosition;
        private Vector2 _searchTargetPosition;
        private Vector2 _lastKnownPlayerPosition;
        private int _currentPatrolWaypointIndex;
        private float _lastDistanceToWaypoint = float.PositiveInfinity;
        private float _timeWithoutWaypointProgress;
        private float _waitTimeRemaining;
        private float _facingSign = 1f;
        private float _searchDirectionSign = 1f;

        /// <summary>
        /// 현재 순찰 이동이 목표로 삼는 Waypoint 인덱스.
        /// </summary>
        public int CurrentPatrolWaypointIndex => _currentPatrolWaypointIndex;

        /// <summary>
        /// 적이 도착한 Waypoint에서 대기 중인지 여부.
        /// </summary>
        public bool IsWaitingAtWaypoint => _waitTimeRemaining > 0f;

        /// <summary>
        /// 현재 AI 상태.
        /// </summary>
        public EnemyStateMachine.EnemyState CurrentState => _stateMachine.CurrentState;

        private void Awake()
        {
            EnsureComponentReferences();
            CacheTargetCollider();
            CacheFacingSign();
            ConfigureRigidbody();
            SyncPerceptionTuning();
            SyncPerceptionFacing();
        }

        private void OnEnable()
        {
            LocalWorldPlayerSpawner.OnCurrentPlayerChanged += HandleCurrentPlayerChanged;
            TryBindCurrentPlayer();
        }

        private void OnDisable()
        {
            LocalWorldPlayerSpawner.OnCurrentPlayerChanged -= HandleCurrentPlayerChanged;
        }

        private void OnValidate()
        {
            EnsureComponentReferences();
            CacheTargetCollider();
            CacheFacingSign();
            SyncPerceptionTuning();
            SyncPerceptionFacing();
        }

        private void FixedUpdate()
        {
            TickEnemy(Time.fixedDeltaTime);
        }

        /// <summary>
        /// 이 적이 감지하고 추격할 플레이어를 지정한다.
        /// </summary>
        /// <param name="targetPlayer">타겟 플레이어 모터.</param>
        public void SetTargetPlayer(PlayerMotor2D targetPlayer)
        {
            _targetPlayer = targetPlayer;
            CacheTargetCollider();
        }

        private void TickEnemy(float deltaTime)
        {
            EnsureComponentReferences();
            if (_rigidbody2D == null || deltaTime <= 0f)
            {
                return;
            }

            EnemyStateMachine.EnemyState previousState = _stateMachine.CurrentState;
            bool canSeePlayer = TryEvaluateTargetSight();
            EnemyStateMachine.EnemyState state = _stateMachine.TickState(
                canSeePlayer,
                deltaTime,
                GetSearchDuration());

            if (state == EnemyStateMachine.EnemyState.Search && previousState != EnemyStateMachine.EnemyState.Search)
            {
                BeginSearch();
            }

            switch (state)
            {
                case EnemyStateMachine.EnemyState.Chase:
                    TickChase(deltaTime);
                    break;
                case EnemyStateMachine.EnemyState.Search:
                    TickSearch(deltaTime);
                    break;
                default:
                    TickPatrol(deltaTime);
                    break;
            }
        }

        private bool TryEvaluateTargetSight()
        {
            if (_targetPlayer == null || _perception == null)
            {
                return false;
            }

            Vector2 playerPosition = _targetPlayer.transform.position;
            bool canSeePlayer = _perception.EvaluateSight(playerPosition, _targetPlayer.IsCrouching, _targetCollider);
            if (canSeePlayer)
            {
                _lastKnownPlayerPosition = playerPosition;
            }

            return canSeePlayer;
        }

        private void TickChase(float deltaTime)
        {
            if (_targetPlayer == null || _tuning == null)
            {
                StopPatrolMovement();
                return;
            }

            Vector2 targetPosition = _targetPlayer.transform.position;
            _lastKnownPlayerPosition = targetPosition;
            MoveToward(targetPosition, deltaTime, Mathf.Max(0f, _tuning.ChaseSpeed));
        }

        private void BeginSearch()
        {
            _searchCenterPosition = _lastKnownPlayerPosition;
            _searchDirectionSign = Mathf.Approximately(_facingSign, 0f) ? 1f : _facingSign;
            _searchTargetPosition = GetSearchPatrolTarget();
        }

        private void TickSearch(float deltaTime)
        {
            if (_tuning == null)
            {
                StopPatrolMovement();
                return;
            }

            Vector2 currentPosition = GetCurrentPosition();
            Vector2 toSearchTarget = _searchTargetPosition - currentPosition;
            if (GetDistanceToTarget(toSearchTarget) <= ARRIVAL_DISTANCE)
            {
                _searchDirectionSign *= -1f;
                _searchTargetPosition = GetSearchPatrolTarget();
            }

            MoveToward(_searchTargetPosition, deltaTime, Mathf.Max(0f, _tuning.MoveSpeed));
        }

        private Vector2 GetSearchPatrolTarget()
        {
            return _searchCenterPosition + Vector2.right * (_searchPatrolRadius * _searchDirectionSign);
        }

        /// <summary>
        /// 주어진 시간 간격만큼 Waypoint 순찰 이동을 진행한다.
        /// </summary>
        /// <param name="deltaTime">초 단위 경과 시간.</param>
        private void TickPatrol(float deltaTime)
        {
            EnsureComponentReferences();
            if (_rigidbody2D == null)
            {
                return;
            }

            if (!CanPatrol() || deltaTime <= 0f)
            {
                StopPatrolMovement();
                return;
            }

            if (_waitTimeRemaining > 0f)
            {
                _waitTimeRemaining = Mathf.Max(0f, _waitTimeRemaining - deltaTime);
                StopPatrolMovement();
                return;
            }

            Transform targetWaypoint = GetValidCurrentWaypoint();
            if (targetWaypoint == null)
            {
                StopPatrolMovement();
                return;
            }

            Vector2 currentPosition = GetCurrentPosition();
            Vector2 targetPosition = targetWaypoint.position;
            Vector2 toTarget = targetPosition - currentPosition;

            if (GetDistanceToTarget(toTarget) <= ARRIVAL_DISTANCE)
            {
                ArriveAtWaypoint();
                return;
            }

            UpdateWaypointProgressTimer(GetDistanceToTarget(toTarget), deltaTime);
            if (_timeWithoutWaypointProgress >= _stuckSkipSeconds)
            {
                SkipCurrentWaypoint();
                return;
            }

            UpdateFacing(toTarget.x);
            MoveToward(targetPosition, deltaTime, Mathf.Max(0f, _tuning.MoveSpeed));
        }

        private bool CanPatrol()
        {
            return _tuning != null && _patrolWaypoints != null && _patrolWaypoints.Length > 0;
        }

        private Transform GetValidCurrentWaypoint()
        {
            int waypointCount = _patrolWaypoints.Length;
            _currentPatrolWaypointIndex = Mathf.Clamp(_currentPatrolWaypointIndex, 0, waypointCount - 1);

            for (int checkedCount = 0; checkedCount < waypointCount; checkedCount++)
            {
                Transform waypoint = _patrolWaypoints[_currentPatrolWaypointIndex];
                if (waypoint != null)
                {
                    return waypoint;
                }

                AdvanceWaypointIndex();
            }

            return null;
        }

        private void ArriveAtWaypoint()
        {
            AdvanceWaypointIndex();
            StopPatrolMovement();
            _waitTimeRemaining = Mathf.Max(0f, _tuning.PatrolWaitTime);
        }

        private void SkipCurrentWaypoint()
        {
            Transform skippedWaypoint = _patrolWaypoints[_currentPatrolWaypointIndex];
            Debug.Log(
                $"{name} skipped unreachable waypoint {skippedWaypoint.name} after {_stuckSkipSeconds:0.##} seconds without progress.",
                this);

            AdvanceWaypointIndex();
            StopPatrolMovement();
        }

        private void AdvanceWaypointIndex()
        {
            _currentPatrolWaypointIndex = (_currentPatrolWaypointIndex + 1) % _patrolWaypoints.Length;
            ResetWaypointProgressTimer();
        }

        private Vector2 GetCurrentPosition()
        {
            return _rigidbody2D.position;
        }

        private void MoveToward(Vector2 targetPosition, float deltaTime, float speed)
        {
            if (_movementMode == EnemyMovementMode.Flying)
            {
                MoveFlying(targetPosition, deltaTime, speed);
                return;
            }

            MoveGrounded(targetPosition.x, deltaTime, speed);
        }

        private void MoveGrounded(float targetX, float deltaTime, float speed)
        {
            Vector2 currentPosition = GetCurrentPosition();
            float horizontalDistance = targetX - currentPosition.x;
            float direction = Mathf.Sign(horizontalDistance);
            float maxSpeedWithoutOvershoot = Mathf.Abs(horizontalDistance) / deltaTime;
            Vector2 velocity = _rigidbody2D.linearVelocity;

            if (Mathf.Abs(horizontalDistance) <= ARRIVAL_DISTANCE || !HasGroundAhead(direction))
            {
                StopPatrolMovement();
                return;
            }

            UpdateFacing(horizontalDistance);
            velocity.x = direction * Mathf.Min(speed, maxSpeedWithoutOvershoot);
            _rigidbody2D.linearVelocity = velocity;
            _rigidbody2D.WakeUp();
        }

        private void MoveFlying(Vector2 targetPosition, float deltaTime, float speed)
        {
            Vector2 currentPosition = GetCurrentPosition();
            float stepDistance = speed * deltaTime;
            Vector2 nextPosition = Vector2.MoveTowards(currentPosition, targetPosition, stepDistance);

            UpdateFacing(targetPosition.x - currentPosition.x);
            _rigidbody2D.MovePosition(nextPosition);
            _rigidbody2D.WakeUp();
        }

        private void StopPatrolMovement()
        {
            Vector2 velocity = _rigidbody2D.linearVelocity;

            if (_movementMode == EnemyMovementMode.Flying)
            {
                velocity = Vector2.zero;
            }
            else
            {
                velocity.x = 0f;
            }

            _rigidbody2D.linearVelocity = velocity;
        }

        private bool HasGroundAhead(float direction)
        {
            if (_movementMode == EnemyMovementMode.Flying || _collider2D == null)
            {
                return true;
            }

            Bounds bounds = _collider2D.bounds;
            if (!HasGroundBelow(bounds))
            {
                return true;
            }

            Vector2 origin = new(
                bounds.center.x + direction * (bounds.extents.x + _groundProbeForwardDistance),
                bounds.min.y + FACING_EPSILON);

            int hitCount = Physics2D.RaycastNonAlloc(origin, Vector2.down, _groundHits, _groundProbeDownDistance, _groundLayers);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hitCollider = _groundHits[i].collider;
                if (hitCollider != null && hitCollider != _collider2D)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasGroundBelow(Bounds bounds)
        {
            Vector2 origin = new(bounds.center.x, bounds.min.y + FACING_EPSILON);
            int hitCount = Physics2D.RaycastNonAlloc(origin, Vector2.down, _groundHits, _groundProbeDownDistance, _groundLayers);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hitCollider = _groundHits[i].collider;
                if (hitCollider != null && hitCollider != _collider2D)
                {
                    return true;
                }
            }

            return false;
        }

        private float GetDistanceToTarget(Vector2 toTarget)
        {
            if (_movementMode == EnemyMovementMode.Flying)
            {
                return toTarget.magnitude;
            }

            return Mathf.Abs(toTarget.x);
        }

        private void UpdateWaypointProgressTimer(float distanceToTarget, float deltaTime)
        {
            if (distanceToTarget < _lastDistanceToWaypoint - PROGRESS_EPSILON)
            {
                _timeWithoutWaypointProgress = 0f;
            }
            else
            {
                _timeWithoutWaypointProgress += deltaTime;
            }

            _lastDistanceToWaypoint = distanceToTarget;
        }

        private void ResetWaypointProgressTimer()
        {
            _lastDistanceToWaypoint = float.PositiveInfinity;
            _timeWithoutWaypointProgress = 0f;
        }

        private void UpdateFacing(float horizontalDirection)
        {
            if (Mathf.Abs(horizontalDirection) <= FACING_EPSILON)
            {
                return;
            }

            _facingSign = Mathf.Sign(horizontalDirection);
            Vector3 localScale = transform.localScale;
            localScale.x = Mathf.Abs(localScale.x) * -_facingSign;
            transform.localScale = localScale;
            SyncPerceptionFacing();
        }

        private void CacheFacingSign()
        {
            float localScaleX = transform.localScale.x;
            if (Mathf.Abs(localScaleX) > FACING_EPSILON)
            {
                _facingSign = -Mathf.Sign(localScaleX);
            }
        }

        private void SyncPerceptionFacing()
        {
            if (_perception != null)
            {
                _perception.SetFacingDirection(Vector2.right * _facingSign);
            }
        }

        private void SyncPerceptionTuning()
        {
            if (_perception != null)
            {
                _perception.SetTuning(_tuning);
            }
        }

        private float GetSearchDuration()
        {
            return _tuning != null ? Mathf.Max(0f, _tuning.LoseSightSeconds) : 0f;
        }

        private void CacheTargetCollider()
        {
            _targetCollider = _targetPlayer != null ? _targetPlayer.GetComponent<Collider2D>() : null;
        }

        private void TryBindCurrentPlayer()
        {
            LocalWorldPlayerSpawner playerSpawner = FindAnyObjectByType<LocalWorldPlayerSpawner>();
            if (playerSpawner == null || playerSpawner.CurrentPlayer == null)
            {
                SetTargetPlayer(null);
                return;
            }

            TryBindPlayer(playerSpawner.CurrentPlayer);
        }

        private void HandleCurrentPlayerChanged(GameObject currentPlayer)
        {
            TryBindPlayer(currentPlayer);
        }

        private void TryBindPlayer(GameObject currentPlayer)
        {
            if (currentPlayer == null || currentPlayer.scene != gameObject.scene)
            {
                SetTargetPlayer(null);
                return;
            }

            if (currentPlayer.TryGetComponent(out PlayerMotor2D playerMotor))
            {
                SetTargetPlayer(playerMotor);
                return;
            }

            SetTargetPlayer(null);
        }

        private void EnsureComponentReferences()
        {
            if (_collider2D == null)
            {
                _collider2D = GetComponent<Collider2D>();
            }

            if (_perception == null)
            {
                _perception = GetComponent<EnemyPerception2D>();
            }

            if (_rigidbody2D == null)
            {
                _rigidbody2D = GetComponent<Rigidbody2D>();
            }
        }

        private void ConfigureRigidbody()
        {
            if (!TryGetComponent(out Rigidbody2D body))
            {
                return;
            }

            body.bodyType = _movementMode == EnemyMovementMode.Flying
                ? RigidbodyType2D.Kinematic
                : RigidbodyType2D.Dynamic;
            body.gravityScale = _movementMode == EnemyMovementMode.Flying ? 0f : 1f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }
}
