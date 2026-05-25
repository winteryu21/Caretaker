using System;

using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

/* 액션을 읽어서 이동값, 점프 이벤트, 상호작용 요청으로 변환해서 PlayerController에 전달하는 역할. PlayerInput 컴포넌트와 함께 사용됩니다. */

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputReader : MonoBehaviour
{
    private const string INPUT_ACTIONS_ASSET_PATH = "Assets/InputSystem_Actions.inputactions";
    private const string PLAYER_ACTION_MAP = "Player";

    private InputAction _clickAction;
    private InputAction _crouchAction;
    private InputAction _interactAction;
    private InputAction _jumpAction;
    private InputAction _moveAction;
    private PlayerInput _playerInput;
    private string _selectedItemId;

    /// <summary>
    /// 플레이어가 점프 입력을 눌렀을 때 발생합니다.
    /// </summary>
    public event Action OnJumpPressed;

    /// <summary>
    /// 플레이어가 상호작용을 요청했을 때 발생합니다.
    /// </summary>
    public event Action<InteractionRequest> OnInteractionRequested;

    /// <summary>
    /// 현재 이동 입력값을 반환합니다.
    /// </summary>
    public Vector2 MoveInput { get; private set; }

    /// <summary>
    /// 웅크리기 입력이 현재 눌려 있는지 반환합니다.
    /// </summary>
    public bool IsCrouchPressed => _crouchAction.IsPressed();

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _moveAction = _playerInput.actions["Move"];
        _jumpAction = _playerInput.actions["Jump"];
        _crouchAction = _playerInput.actions["Crouch"];
        _clickAction = _playerInput.actions["Attack"];
        _interactAction = _playerInput.actions["Interact"];
    }

    private void OnEnable()
    {
        _jumpAction.performed += HandleJumpPerformed;
        _clickAction.performed += HandleClickPerformed;
        _interactAction.performed += HandleInteractPerformed;
    }

    private void Update()
    {
        MoveInput = _moveAction.ReadValue<Vector2>();
    }

    private void OnDisable()
    {
        _jumpAction.performed -= HandleJumpPerformed;
        _clickAction.performed -= HandleClickPerformed;
        _interactAction.performed -= HandleInteractPerformed;
    }

    private void Reset()
    {
        _playerInput = GetComponent<PlayerInput>();

#if UNITY_EDITOR
        _playerInput.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_ASSET_PATH);
        _playerInput.defaultActionMap = PLAYER_ACTION_MAP;
#endif
    }

    /// <summary>
    /// 현재 선택된 인벤토리 아이템 ID를 설정합니다.
    /// </summary>
    /// <param name="selectedItemId">선택된 아이템 ID입니다. 선택된 아이템이 없으면 null입니다.</param>
    public void SetSelectedItem(string selectedItemId)
    {
        _selectedItemId = selectedItemId;
    }

    private void HandleJumpPerformed(InputAction.CallbackContext context)
    {
        OnJumpPressed?.Invoke();
    }

    private void HandleClickPerformed(InputAction.CallbackContext context)
    {
        InteractionMode mode = string.IsNullOrEmpty(_selectedItemId) ? InteractionMode.Inspect : InteractionMode.UseItem;
        var request = new InteractionRequest(mode, Mouse.current.position.ReadValue(), _selectedItemId);
        OnInteractionRequested?.Invoke(request);
    }

    private void HandleInteractPerformed(InputAction.CallbackContext context)
    {
        var request = new InteractionRequest(InteractionMode.ImmediateInteract, Vector2.zero, null);
        OnInteractionRequested?.Invoke(request);
    }
}
