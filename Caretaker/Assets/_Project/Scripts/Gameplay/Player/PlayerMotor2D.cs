using UnityEngine;

/* Rigidbody2D 이동, 점프, 웅크리기, 바닥 체크만 담당합니다. */

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor2D : MonoBehaviour
{
    private const float CROUCH_SPEED_MULTIPLIER = 0.6f;
    private const float DEFAULT_JUMP_RELEASE_VELOCITY_MULTIPLIER = 0.5f;
    private const float GROUND_CHECK_DISTANCE = 0.05f;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _sprintSpeedMultiplier = 1.5f;
    [SerializeField] private float _jumpHeight = 2f;

    [Header("Horizontal Tuning")]
    [SerializeField] private float _groundAcceleration = 40f;
    [SerializeField] private float _groundDeceleration = 28f;
    [SerializeField] private float _groundTurnSpeed = 48f;
    [SerializeField] private float _airAcceleration = 20f;
    [SerializeField] private float _airDeceleration = 8f;
    [SerializeField] private float _airTurnSpeed = 24f;

    [Header("Jump Assist")]
    [SerializeField] private float _coyoteTimeDuration = 0.1f;
    [SerializeField] private float _jumpBufferDuration = 0.1f;
    [SerializeField] [Range(0.1f, 1f)] private float _jumpReleaseVelocityMultiplier = DEFAULT_JUMP_RELEASE_VELOCITY_MULTIPLIER;

    [Header("Gravity Tuning")]
    [SerializeField] [Min(1f)] private float _jumpStartGravityMultiplier = 1f;
    [SerializeField] [Min(1f)] private float _jumpPeakGravityMultiplier = 2.4f;
    [SerializeField] [Min(0.01f)] private float _jumpGravityRampDuration = 0.35f;

    [Header("Crouch")]
    [SerializeField] [Range(0.3f, 1f)] private float _crouchColliderHeightScale = 0.6f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask _groundLayers = Physics2D.DefaultRaycastLayers;

    private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[4];

    private Vector2 _crouchingColliderOffset;
    private Vector2 _crouchingColliderSize;
    private Vector2 _standingColliderOffset;
    private Vector2 _standingColliderSize;

    private BoxCollider2D _boxCollider;
    private Rigidbody2D _rigidbody2D;

    private float _coyoteTimeRemaining;
    private float _horizontalMaximumX;
    private float _horizontalMinimumX;
    private float _jumpBufferRemaining;
    private float _jumpAirTime;
    private bool _hasHorizontalBounds;
    private bool _isCrouching;
    private bool _isJumpGravityActive;
    private bool _isJumpGroundedLockActive;
    private bool _jumpCutConsumed;
    private bool _jumpCutAvailable;

    /// <summary>
    /// 현재 유효한 바닥 위에 서 있는지 반환합니다.
    /// </summary>
    public bool IsGrounded { get; private set; }

    /// <summary>
    /// 플레이어가 현재 웅크리고 있는지 여부.
    /// </summary>
    public bool IsCrouching => _isCrouching;

    /// <summary>현재 이동 속도는 유지하면서 플레이어의 수평 이동 범위를 제한합니다.</summary>
    public void SetHorizontalBounds(float minimumX, float maximumX)
    {
        _horizontalMinimumX = Mathf.Min(minimumX, maximumX);
        _horizontalMaximumX = Mathf.Max(minimumX, maximumX);
        _hasHorizontalBounds = true;

        if (_rigidbody2D != null && _rigidbody2D.simulated)
        {
            Vector2 position = _rigidbody2D.position;
            position.x = ClampHorizontalPosition(
                position.x,
                _horizontalMinimumX,
                _horizontalMaximumX);
            _rigidbody2D.position = position;
        }
    }

    /// <summary>설정된 수평 이동 범위를 해제합니다.</summary>
    public void ClearHorizontalBounds()
    {
        _hasHorizontalBounds = false;
    }

    /// <summary>다음 물리 프레임에 경계를 넘지 않도록 수평 속도를 제한합니다.</summary>
    public static float ClampHorizontalVelocity(
        float positionX,
        float velocityX,
        float minimumX,
        float maximumX,
        float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return 0f;
        }

        if (positionX < minimumX)
        {
            return velocityX > 0f ? velocityX : 0f;
        }

        if (positionX > maximumX)
        {
            return velocityX < 0f ? velocityX : 0f;
        }

        float predictedX = positionX + (velocityX * deltaTime);
        if (predictedX < minimumX)
        {
            return (minimumX - positionX) / deltaTime;
        }

        if (predictedX > maximumX)
        {
            return (maximumX - positionX) / deltaTime;
        }

        return velocityX;
    }

    /// <summary>현재 X 위치를 설정된 수평 경계 안으로 제한합니다.</summary>
    public static float ClampHorizontalPosition(
        float positionX,
        float minimumX,
        float maximumX)
    {
        return Mathf.Clamp(
            positionX,
            Mathf.Min(minimumX, maximumX),
            Mathf.Max(minimumX, maximumX));
    }

    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider2D>();
        _rigidbody2D = GetComponent<Rigidbody2D>();

        ConfigureRigidbodyConstraints();
        CacheColliderState();
        IsGrounded = PerformGroundCheck();
        if (IsGrounded)
        {
            _coyoteTimeRemaining = _coyoteTimeDuration;
        }
    }

    private void OnValidate()
    {
        ConfigureRigidbodyConstraints();
    }

    /// <summary>
    /// 현재 물리 틱 입력을 바탕으로 이동 상태를 갱신합니다.
    /// </summary>
    public void TickMotor(Vector2 moveInput, bool jumpPressed, bool isJumpHeld, bool crouchHeld, bool sprintHeld)
    {
        // 짧게 눌린 입력도 이번 물리 틱까지 유지되도록 먼저 버퍼링합니다.
        BufferJumpInput(jumpPressed);
        // 이동과 점프 규칙을 계산하기 전에 공통 이동 상태를 먼저 갱신합니다.
        UpdateGroundState();
        UpdateJumpState();

        bool shouldCrouch = ResolveCrouchState(crouchHeld);
        ApplyCrouchState(shouldCrouch);
        ApplyHorizontalMovement(moveInput, shouldCrouch, sprintHeld);

        if (TryApplyJump())
        {
            IsGrounded = false;
        }

        ApplyJumpCut(isJumpHeld);
        ApplyJumpGravity(isJumpHeld);
    }

    // ======== 내부 로직 =======
    // 점프 관련
    private void BufferJumpInput(bool jumpPressed)
    {
        if (jumpPressed)
        {
            _jumpBufferRemaining = _jumpBufferDuration;
        }
    }

    private void UpdateGroundState()
    {
        IsGrounded = PerformGroundCheck();
    }

    private void UpdateJumpState()
    {
        float deltaTime = Time.fixedDeltaTime;

        if (IsGrounded)
        {
            // 착지하면 점프 전용 보조 상태를 모두 초기화하고 코요테 타임을 다시 채웁니다.
            ResetAirborneState();
            _coyoteTimeRemaining = _coyoteTimeDuration;
        }
        else
        {
            _coyoteTimeRemaining = Mathf.Max(0f, _coyoteTimeRemaining - deltaTime);

            // 지면 잠금은 점프 직후 상승 중 재접지를 막기 위한 용도만 가집니다.
            if (_isJumpGroundedLockActive && _rigidbody2D.linearVelocity.y <= 0f)
            {
                _isJumpGroundedLockActive = false;
            }
        }

        _jumpBufferRemaining = Mathf.Max(0f, _jumpBufferRemaining - deltaTime);
    }

    private bool TryApplyJump()
    {
        if (_jumpBufferRemaining <= 0f)
        {
            return false;
        }

        if (!CanUseBufferedJump())
        {
            return false;
        }

        Vector2 velocity = _rigidbody2D.linearVelocity;
        velocity.y = CalculateJumpLaunchSpeed();
        _rigidbody2D.linearVelocity = velocity;

        BeginJumpArc();
        return true;
    }

    private bool CanUseBufferedJump()
    {
        return IsGrounded || _coyoteTimeRemaining > 0f;
    }

    private float CalculateJumpLaunchSpeed()
    {
        float gravity = Mathf.Abs(Physics2D.gravity.y * _rigidbody2D.gravityScale);
        if (gravity <= 0f)
        {
            gravity = Mathf.Abs(Physics2D.gravity.y);
        }

        return Mathf.Sqrt(2f * gravity * _jumpHeight);
    }

    private void BeginJumpArc()
    {
        // 점프에 성공하면 새 공중 궤적이 시작되며 중력 램프와 지면 잠금도 함께 시작됩니다.
        _coyoteTimeRemaining = 0f;
        _jumpBufferRemaining = 0f;
        _jumpAirTime = 0f;
        _isJumpGravityActive = true;
        _isJumpGroundedLockActive = true;
        _jumpCutAvailable = true;
        _jumpCutConsumed = false;
    }

    private void ApplyJumpCut(bool isJumpHeld)
    {
        if (isJumpHeld || _jumpCutConsumed || !_jumpCutAvailable)
        {
            return;
        }

        Vector2 velocity = _rigidbody2D.linearVelocity;
        if (velocity.y <= 0f)
        {
            return;
        }

        velocity.y *= _jumpReleaseVelocityMultiplier;
        _rigidbody2D.linearVelocity = velocity;
        // 점프 컷은 한 번만 적용하고, 이후 궤적은 중력 램프가 이어서 정리합니다.
        _jumpCutAvailable = false;
        _jumpCutConsumed = true;
    }

    private void ApplyJumpGravity(bool isJumpHeld)
    {
        if (IsGrounded || !_isJumpGravityActive)
        {
            return;
        }

        Vector2 gravity = Physics2D.gravity * _rigidbody2D.gravityScale;
        if (Mathf.Approximately(gravity.y, 0f))
        {
            return;
        }

        Vector2 velocity = _rigidbody2D.linearVelocity;
        _jumpAirTime += Time.fixedDeltaTime;

        // 공중 시간이 길어질수록 중력을 키워서 점프 초반은 부드럽고 후반은 더 강하게 끌어당깁니다.
        float gravityMultiplier = GetJumpGravityMultiplier(velocity.y, isJumpHeld);
        velocity += gravity * ((gravityMultiplier - 1f) * Time.fixedDeltaTime);
        _rigidbody2D.linearVelocity = velocity;
    }

    private float GetJumpGravityMultiplier(float verticalSpeed, bool isJumpHeld)
    {
        float rampProgress = Mathf.Clamp01(_jumpAirTime / _jumpGravityRampDuration);
        float gravityMultiplier = Mathf.Lerp(
            _jumpStartGravityMultiplier,
            _jumpPeakGravityMultiplier,
            rampProgress);

        if (verticalSpeed > 0f && !isJumpHeld)
        {
            gravityMultiplier = Mathf.Max(gravityMultiplier, _jumpPeakGravityMultiplier);
        }

        return gravityMultiplier;
    }

     private void ResetAirborneState()
    {
        _jumpAirTime = 0f;
        _isJumpGravityActive = false;
        _isJumpGroundedLockActive = false;
        _jumpCutAvailable = false;
        _jumpCutConsumed = false;
    }

    // 이동 관련
    private void ApplyHorizontalMovement(Vector2 moveInput, bool isCrouching, bool wantsToSprint)
    {
        Vector2 velocity = _rigidbody2D.linearVelocity;
        float moveSpeed = GetHorizontalMoveSpeed(isCrouching, wantsToSprint, Mathf.Abs(velocity.x));
        float targetSpeed = moveInput.x * moveSpeed;
        // 즉시 속도를 바꾸지 않고 목표 속도로 수렴시켜 가벼운 관성을 남깁니다.
        float acceleration = GetHorizontalAcceleration(velocity.x, targetSpeed);

        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
        if (_hasHorizontalBounds)
        {
            velocity.x = ClampHorizontalVelocity(
                _rigidbody2D.position.x,
                velocity.x,
                _horizontalMinimumX,
                _horizontalMaximumX,
                Time.fixedDeltaTime);
        }
        _rigidbody2D.linearVelocity = velocity;
    }

    private float GetHorizontalMoveSpeed(bool isCrouching, bool wantsToSprint, float currentSpeed)
    {
        float moveSpeed = _moveSpeed;
        if (isCrouching)
        {
            moveSpeed *= CROUCH_SPEED_MULTIPLIER;
        }
        else if (wantsToSprint && IsGrounded)
        {
            moveSpeed *= _sprintSpeedMultiplier;
        }

        if (!IsGrounded)
        {
            moveSpeed = Mathf.Max(moveSpeed, currentSpeed);
        }

        return moveSpeed;
    }

    private float GetHorizontalAcceleration(float currentSpeed, float targetSpeed)
    {
        if (Mathf.Approximately(targetSpeed, 0f))
        {
            return IsGrounded ? _groundDeceleration : _airDeceleration;
        }

        bool isTurning = !Mathf.Approximately(currentSpeed, 0f) && Mathf.Sign(currentSpeed) != Mathf.Sign(targetSpeed);
        if (isTurning)
        {
            return IsGrounded ? _groundTurnSpeed : _airTurnSpeed;
        }

        return IsGrounded ? _groundAcceleration : _airAcceleration;
    }

    // 웅크리기 관련
    private bool ResolveCrouchState(bool crouchHeld)
    {
        bool shouldCrouch = crouchHeld && IsGrounded;
        if (!shouldCrouch && _isCrouching && !CanStandUp())
        {
            shouldCrouch = true;
        }

        return shouldCrouch;
    }

    private void ApplyCrouchState(bool isCrouching)
    {
        if (_isCrouching == isCrouching)
        {
            return;
        }

        _isCrouching = isCrouching;
        _boxCollider.size = isCrouching ? _crouchingColliderSize : _standingColliderSize;
        _boxCollider.offset = isCrouching ? _crouchingColliderOffset : _standingColliderOffset;
    }

    // 충돌/지면 체크 관련
    private void CacheColliderState()
    {
        _standingColliderSize = _boxCollider.size;
        _standingColliderOffset = _boxCollider.offset;

        float crouchingHeight = _standingColliderSize.y * _crouchColliderHeightScale;
        _crouchingColliderSize = new Vector2(_standingColliderSize.x, crouchingHeight);

        float standingBottom = _standingColliderOffset.y - (_standingColliderSize.y * 0.5f);
        float crouchingOffsetY = standingBottom + (crouchingHeight * 0.5f);
        _crouchingColliderOffset = new Vector2(_standingColliderOffset.x, crouchingOffsetY);
    }

    private bool PerformGroundCheck()
    {
        // 점프 직후 상승 중에는 바닥 접촉을 무시해서 의도치 않은 재접지를 막습니다.
        if (_isJumpGroundedLockActive)
        {
            return false;
        }

        ContactFilter2D contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _groundLayers,
            useTriggers = false
        };

        return _rigidbody2D.Cast(Vector2.down, contactFilter, _groundHits, GROUND_CHECK_DISTANCE) > 0;
    }

    private bool CanStandUp()
    {
        Vector2 standingCenter = (Vector2)_rigidbody2D.position + _standingColliderOffset;
        Vector2 standingSize = _standingColliderSize;
        float standingAngle = _rigidbody2D.rotation;

        Collider2D blockingCollider = Physics2D.OverlapBox(
            standingCenter,
            standingSize,
            standingAngle,
            _groundLayers);

        return blockingCollider == null || blockingCollider == _boxCollider;
    }

    private void ConfigureRigidbodyConstraints()
    {
        if (_rigidbody2D == null)
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        if (_rigidbody2D != null)
        {
            _rigidbody2D.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }
    }
}
