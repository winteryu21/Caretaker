using System;

using UnityEngine;

using Caretaker.Shared;
using Caretaker.World;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 입력과 모터를 연결하고, 상호작용 요청을 상호작용 서비스에 전달합니다.
    /// </summary>
    /// <remarks>DSD §3.3 - 플레이어 제어 시스템</remarks>
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerMotor2D))]
    [RequireComponent(typeof(InteractionProbe))]
    [RequireComponent(typeof(InventoryController))]
    public class PlayerController : MonoBehaviour
    {
        private PlayerInputReader _inputReader;
        private InteractionProbe _interactionProbe;
        private InteractionService _interactionService;
        private InventoryController _inventoryController;
        private PlayerMotor2D _motor2D;

        /// <summary>
        /// 유효한 상호작용 대상과 타입이 확정되고 실행되었을 때 발생합니다.
        /// </summary>
        public event Action<InteractableObject, InteractionType> OnInteractionResolved;

        /// <summary>
        /// 인벤토리 슬롯 선택 입력이 확정되었을 때 발생합니다. 슬롯 인덱스는 0부터 시작합니다.
        /// </summary>
        public event Action<int> OnInventorySlotSelected;

        private void Awake()
        {
            _inputReader = GetComponent<PlayerInputReader>();
            _motor2D = GetComponent<PlayerMotor2D>();
            _interactionProbe = GetComponent<InteractionProbe>();
            _inventoryController = GetComponent<InventoryController>();
            _interactionService = new InteractionService();
        }

        private void OnEnable()
        {
            _inputReader.OnInteractionRequested += HandleInteractionRequested;
            _inputReader.OnInventorySlotSelected += HandleInventorySlotSelected;
        }

        // 플레이어의 이동과 점프, 웅크리기, 달리기 입력을 모터2D에 전달합니다.
        private void FixedUpdate()
        {
            _motor2D.TickMotor(
                _inputReader.MoveInput,
                _inputReader.ConsumeJumpPressed(),
                _inputReader.IsJumpPressed,
                _inputReader.IsCrouchPressed,
                _inputReader.IsSprintPressed);
        }

        private void OnDisable()
        {
            _inputReader.OnInteractionRequested -= HandleInteractionRequested;
            _inputReader.OnInventorySlotSelected -= HandleInventorySlotSelected;
        }

        // 플레이어 입력 요청을 서비스에 전달해 실제 상호작용 여부를 판정합니다.
        private void HandleInteractionRequested(InteractionRequest request)
        {
            if (request.Type == InteractionType.UseItem)
            {
                HandleUseItemRequested();
                return;
            }

            if (!_interactionService.TryProcessInteraction(
                    request,
                    _interactionProbe.HoverTarget,
                    _interactionProbe.ProximityTarget,
                    _interactionProbe.ProximityInteractionType,
                    this,
                    _interactionProbe.InteractionRadius,
                    out InteractableObject resolvedTarget,
                    out InteractionType resolvedType))
            {
                return;
            }

            if (resolvedType == InteractionType.Operate)
            {
                ActivateCausalTriggerIfPresent(resolvedTarget);
            }

            OnInteractionResolved?.Invoke(resolvedTarget, resolvedType);
        }

        private void HandleInventorySlotSelected(int slotIndex)
        {
            OnInventorySlotSelected?.Invoke(slotIndex);
        }

        private void HandleUseItemRequested()
        {
            if (_inventoryController == null || _interactionProbe.ProximityTarget == null)
            {
                return;
            }

            InteractableObject target = _interactionProbe.ProximityTarget;
            float distance = target.GetDistanceFrom(transform.position);
            if (distance < 0f || distance > _interactionProbe.InteractionRadius)
            {
                return;
            }

            if (!target.HasRequiredItem || target.IsRequiredItemSatisfied)
            {
                return;
            }

            if (!_inventoryController.UseSelectedItemOn(target))
            {
                return;
            }

            target.MarkRequiredItemSatisfied();
            ActivateCausalTriggerIfPresent(target);
            OnInteractionResolved?.Invoke(target, InteractionType.UseItem);
        }

        private void ActivateCausalTriggerIfPresent(InteractableObject target)
        {
            if (target == null)
            {
                return;
            }

            if (target.TryGetComponent(out CausalTrigger causalTrigger))
            {
                causalTrigger.Activate();
            }
        }
    }
}
