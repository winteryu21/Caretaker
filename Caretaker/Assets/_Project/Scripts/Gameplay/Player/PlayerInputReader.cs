using System;

using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Caretaker.Shared;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// Input System 액션을 이동값, 점프 이벤트, 상호작용 요청으로 변환하여 PlayerController에 전달한다.
    /// PlayerInput 컴포넌트와 함께 사용된다.
    /// </summary>
    /// <remarks>
    /// DSD §3.3 — 플레이어 제어 및 상호작용 시스템
    /// 계층: Unity Component
    /// </remarks>
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
        private InputAction _sprintAction;
        private PlayerInput _playerInput;
        private string _selectedItemId;

        /// <summary>
        /// 플레이어가 점프 입력을 눌렀을 때 발생한다.
        /// </summary>
        public event Action OnJumpPressed;

        /// <summary>
        /// 플레이어가 상호작용을 요청했을 때 발생한다.
        /// </summary>
        public event Action<InteractionRequest> OnInteractionRequested;

        /// <summary>
        /// 현재 이동 입력값을 반환한다.
        /// </summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>
        /// 웅크리기 입력이 현재 눌려 있는지 반환한다.
        /// </summary>
        public bool IsCrouchPressed => _crouchAction.IsPressed();

        public bool IsSprintPressed => _sprintAction.IsPressed();

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _moveAction = _playerInput.actions["Move"];
            _jumpAction = _playerInput.actions["Jump"];
            _crouchAction = _playerInput.actions["Crouch"];
            _sprintAction = _playerInput.actions["Sprint"];
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
        /// 현재 선택된 인벤토리 아이템 ID를 설정한다.
        /// </summary>
        /// <param name="selectedItemId">선택된 아이템 ID. 선택된 아이템이 없으면 null.</param>
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
            InteractionType type = string.IsNullOrEmpty(_selectedItemId)
                ? InteractionType.Examine
                : InteractionType.UseItem;
            var request = new InteractionRequest(type, Mouse.current.position.ReadValue(), _selectedItemId);
            OnInteractionRequested?.Invoke(request);
        }

        private void HandleInteractPerformed(InputAction.CallbackContext context)
        {
            var request = new InteractionRequest(InteractionType.Operate, Vector2.zero, null);
            OnInteractionRequested?.Invoke(request);
        }
    }
}
