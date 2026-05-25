using UnityEngine;

/* Rigidbody2D 이동, 점프, 웅크리기, 바닥 체크만 담당합니다. */

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor2D : MonoBehaviour
{
    private const float CROUCH_SPEED_MULTIPLIER = 0.6f;
    private const float GROUND_CHECK_DISTANCE = 0.05f;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _sprintSpeedMultiplier = 1.5f;
    [SerializeField] private float _jumpHeight = 2f;

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

    private bool _isCrouching;
    private bool _jumpQueued;

    /// <summary>
    /// 현재 유효한 바닥에 닿아 있는지 반환합니다.
    /// </summary>
    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider2D>();
        _rigidbody2D = GetComponent<Rigidbody2D>();

        CacheColliderState();
        IsGrounded = CheckGrounded();
    }

    /// <summary>
    /// 다음 모터 틱에서 점프가 적용되도록 예약합니다.
    /// </summary>
    public void QueueJump()
    {
        _jumpQueued = true;
    }

    /// <summary>
    /// 현재 물리 틱에 대한 이동, 웅크리기, 점프 상태를 적용합니다.
    /// </summary>
    public void TickMotor(Vector2 moveInput, bool wantsToCrouch, bool wantsToSprint)
    {
        IsGrounded = CheckGrounded();

        bool shouldCrouch = wantsToCrouch && IsGrounded;
        if (!shouldCrouch && _isCrouching && !CanStandUp())
        {
            shouldCrouch = true;
        }

        ApplyCrouchState(shouldCrouch);
        ApplyHorizontalMovement(moveInput, shouldCrouch, wantsToSprint);

        if (ApplyJump())
        {
            IsGrounded = false;
        }
    }

    private void ApplyHorizontalMovement(Vector2 moveInput, bool isCrouching, bool wantsToSprint)
    {
        float moveSpeed = _moveSpeed;
        if (isCrouching)
        {
            moveSpeed *= CROUCH_SPEED_MULTIPLIER;
        }
        else if (wantsToSprint)
        {
            moveSpeed *= _sprintSpeedMultiplier;
        }

        Vector2 velocity = _rigidbody2D.linearVelocity;
        velocity.x = moveInput.x * moveSpeed;
        _rigidbody2D.linearVelocity = velocity;
    }

    private bool ApplyJump()
    {
        if (!_jumpQueued)
        {
            return false;
        }

        if (!IsGrounded)
        {
            _jumpQueued = false;
            return false;
        }

        float gravity = Mathf.Abs(Physics2D.gravity.y * _rigidbody2D.gravityScale);
        if (gravity <= 0f)
        {
            gravity = Mathf.Abs(Physics2D.gravity.y);
        }

        Vector2 velocity = _rigidbody2D.linearVelocity;
        velocity.y = Mathf.Sqrt(2f * gravity * _jumpHeight);
        _rigidbody2D.linearVelocity = velocity;
        _jumpQueued = false;
        return true;
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

    private bool CheckGrounded()
    {
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
}
