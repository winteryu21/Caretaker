using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 적의 Waypoint 순찰 이동과 바라보는 방향을 제어한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.5 — AI / 경보 시스템.
    /// 이후 FSM 작업에서 이 컴포넌트에 이동 목표를 전달할 수 있다.
    /// </remarks>
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
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

        [SerializeField] private EnemyMovementMode _movementMode = EnemyMovementMode.Grounded; // 지상형은 중력, 비행형은 Waypoint 전체 좌표를 따른다.
        [SerializeField] private EnemyTuningSO _tuning; // 이동 속도와 Waypoint 대기 시간을 제공하는 튜닝 데이터.
        [SerializeField] private Transform[] _patrolWaypoints; // 순서대로 순회할 순찰 지점 배열.
        [SerializeField] private LayerMask _groundLayers = Physics2D.DefaultRaycastLayers; // 지상형 적이 낙하 방지에 사용할 바닥 레이어.
        [SerializeField] [Min(0f)] private float _groundProbeForwardDistance = 0.15f; // 발끝보다 앞쪽을 얼마나 더 확인할지 정한다.
        [SerializeField] [Min(0.01f)] private float _groundProbeDownDistance = 0.4f; // 앞쪽 발밑 바닥을 찾기 위해 아래로 검사할 거리.
        [SerializeField] [Min(0.1f)] private float _stuckSkipSeconds = 1f; // 이 시간 동안 목표에 가까워지지 못하면 다음 Waypoint로 넘어간다.

        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[4];

        private Collider2D _collider2D; // 발밑 검사 시작점을 계산하기 위한 Collider2D 캐시.
        private Rigidbody2D _rigidbody2D; // 물리 기반 이동과 충돌 판정에 사용할 Rigidbody2D 캐시.
        private int _currentPatrolWaypointIndex; // 현재 목표 Waypoint 인덱스.
        private float _lastDistanceToWaypoint = float.PositiveInfinity; // 이전 tick에서 목표 Waypoint까지 남았던 거리.
        private float _timeWithoutWaypointProgress; // 목표 Waypoint에 가까워지지 못한 누적 시간.
        private float _waitTimeRemaining; // Waypoint 도착 후 남은 대기 시간.
        private float _facingSign = 1f; // 현재 이동 방향의 x축 부호.

        /// <summary>
        /// 현재 순찰 이동이 목표로 삼는 Waypoint 인덱스.
        /// </summary>
        public int CurrentPatrolWaypointIndex => _currentPatrolWaypointIndex;

        /// <summary>
        /// 적이 도착한 Waypoint에서 대기 중인지 반환한다.
        /// </summary>
        public bool IsWaitingAtWaypoint => _waitTimeRemaining > 0f;

        private void Awake()
        {
            EnsureComponentReferences();
            CacheFacingSign();
            ConfigureRigidbody();
        }

        private void OnValidate()
        {
            EnsureComponentReferences();
            CacheFacingSign();
        }

        private void FixedUpdate()
        {
            TickPatrol(Time.fixedDeltaTime);
        }

        /// <summary>
        /// 주어진 시간 간격만큼 Waypoint 순찰 이동을 진행한다.
        /// </summary>
        /// <param name="deltaTime">초 단위 시간 간격.</param>
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
            MoveToward(targetPosition, deltaTime);
        }

        // 튜닝 데이터와 Waypoint가 모두 설정된 경우에만 순찰을 허용한다.
        private bool CanPatrol()
        {
            return _tuning != null && _patrolWaypoints != null && _patrolWaypoints.Length > 0;
        }

        // 비어 있는 Waypoint 슬롯은 건너뛰고 실제 Transform이 있는 지점을 찾는다.
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

        // 도착한 지점에서 대기를 시작하고 다음 목표 Waypoint를 미리 가리킨다.
        private void ArriveAtWaypoint()
        {
            AdvanceWaypointIndex();
            StopPatrolMovement();
            _waitTimeRemaining = Mathf.Max(0f, _tuning.PatrolWaitTime);
        }

        // 현재 목표에 도달할 수 없으면 멈춘 뒤 다음 Waypoint를 목표로 바꾼다.
        private void SkipCurrentWaypoint()
        {
            Transform skippedWaypoint = _patrolWaypoints[_currentPatrolWaypointIndex];
            Debug.Log(
                $"{name} skipped unreachable waypoint {skippedWaypoint.name} after {_stuckSkipSeconds:0.##} seconds without progress.",
                this);

            AdvanceWaypointIndex();
            StopPatrolMovement();
        }

        // 마지막 Waypoint 이후에는 첫 Waypoint로 돌아가 순환 순찰한다.
        private void AdvanceWaypointIndex()
        {
            _currentPatrolWaypointIndex = (_currentPatrolWaypointIndex + 1) % _patrolWaypoints.Length;
            ResetWaypointProgressTimer();
        }

        // 순찰 이동은 Rigidbody2D 위치를 기준으로 계산한다.
        private Vector2 GetCurrentPosition()
        {
            return _rigidbody2D.position;
        }

        // 지상형은 x축만, 비행형은 Waypoint 전체 좌표를 향해 이동한다.
        private void MoveToward(Vector2 targetPosition, float deltaTime)
        {
            if (_movementMode == EnemyMovementMode.Flying)
            {
                MoveFlying(targetPosition, deltaTime);
                return;
            }

            MoveGrounded(targetPosition.x, deltaTime);
        }

        // 지상형 적은 x축 속도만 갱신하고 y축 움직임은 중력과 바닥 충돌에 맡긴다.
        private void MoveGrounded(float targetX, float deltaTime)
        {
            Vector2 currentPosition = GetCurrentPosition();
            float horizontalDistance = targetX - currentPosition.x;
            float direction = Mathf.Sign(horizontalDistance);
            float speed = Mathf.Max(0f, _tuning.MoveSpeed);
            float maxSpeedWithoutOvershoot = Mathf.Abs(horizontalDistance) / deltaTime;
            Vector2 velocity = _rigidbody2D.linearVelocity;

            if (!HasGroundAhead(direction))
            {
                StopPatrolMovement();
                return;
            }

            velocity.x = direction * Mathf.Min(speed, maxSpeedWithoutOvershoot);
            _rigidbody2D.linearVelocity = velocity;
            _rigidbody2D.WakeUp();
        }

        // 비행형 적은 중력 없이 Waypoint의 x/y 좌표를 모두 따라간다.
        private void MoveFlying(Vector2 targetPosition, float deltaTime)
        {
            Vector2 currentPosition = GetCurrentPosition();
            float stepDistance = Mathf.Max(0f, _tuning.MoveSpeed) * deltaTime;
            Vector2 nextPosition = Vector2.MoveTowards(currentPosition, targetPosition, stepDistance);

            _rigidbody2D.MovePosition(nextPosition);
            _rigidbody2D.WakeUp();
        }

        // 순찰하지 않는 동안에는 이전 이동 속도가 남지 않도록 멈춘다.
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

        // 지상형 적이 다음 발걸음에서 바닥을 잃는지 검사한다.
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

        // 지상형은 x축 거리만, 비행형은 2D 전체 거리를 도착 판정에 사용한다.
        private float GetDistanceToTarget(Vector2 toTarget)
        {
            if (_movementMode == EnemyMovementMode.Flying)
            {
                return toTarget.magnitude;
            }

            return Mathf.Abs(toTarget.x);
        }

        // 목표까지의 거리가 줄어들면 stuck 타이머를 리셋하고, 줄어들지 않을 때만 누적한다.
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

        // 새 목표 Waypoint로 바뀌면 이전 목표의 stuck 판정을 초기화한다.
        private void ResetWaypointProgressTimer()
        {
            _lastDistanceToWaypoint = float.PositiveInfinity;
            _timeWithoutWaypointProgress = 0f;
        }

        // 기본 스프라이트가 왼쪽을 바라보므로 이동 방향과 반대 부호를 적용해 좌우를 전환한다.
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
        }

        // 기존 프리팹 스케일의 좌우 방향을 보존하기 위해 현재 이동 방향 부호를 캐시한다.
        private void CacheFacingSign()
        {
            float localScaleX = transform.localScale.x;
            if (Mathf.Abs(localScaleX) > FACING_EPSILON)
            {
                _facingSign = Mathf.Sign(localScaleX);
            }
        }

        private void EnsureComponentReferences()
        {
            if (_collider2D == null)
            {
                _collider2D = GetComponent<Collider2D>();
            }

            if (_rigidbody2D == null)
            {
                _rigidbody2D = GetComponent<Rigidbody2D>();
            }
        }

        // 이동 모드에 맞춰 Rigidbody2D의 중력과 물리 타입을 설정한다.
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
