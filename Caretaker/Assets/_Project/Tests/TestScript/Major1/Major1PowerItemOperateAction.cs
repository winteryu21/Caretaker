using UnityEngine;

using Caretaker.Gameplay;
using Caretaker.World;

namespace Caretaker.Presentation
{
    [AddComponentMenu("Caretaker/Test/Major1/Power Item Operate Action")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InteractableObject))]
    public sealed class Major1PowerItemOperateAction : MonoBehaviour, IOperateAction
    {
        private enum PowerItemActionType
        {
            BatteryBreakerPanel,
            MainPowerCable
        }

        [Header("Action")]
        [SerializeField] private PowerItemActionType _actionType = PowerItemActionType.BatteryBreakerPanel;
        [SerializeField] private Major1PowerRestorationController _controller;

        [Header("Panel")]
        [SerializeField] private PuzzleUIBase _panelToOpen;

        private void Awake()
        {
            EnsureOperateInteractionType();
        }

        private void Reset()
        {
            EnsureOperateInteractionType();
        }

        private void OnValidate()
        {
            EnsureOperateInteractionType();
        }

        /// <summary>Runs the configured Major 1 item-gated power action.</summary>
        /// <param name="actor">Player operating the target.</param>
        /// <returns>True when the action succeeds.</returns>
        public bool Execute(PlayerController actor)
        {
            CacheController();

            if (_controller == null)
            {
                Debug.LogWarning("Major1PowerItemOperateAction needs a controller.", this);
                return false;
            }

            bool succeeded = _actionType switch
            {
                PowerItemActionType.BatteryBreakerPanel => _controller.InsertBattery(actor),
                PowerItemActionType.MainPowerCable => _controller.TryConnectCable(actor),
                _ => false
            };

            if (succeeded && _panelToOpen != null)
            {
                _panelToOpen.Open();
            }

            return succeeded;
        }

        private void CacheController()
        {
            if (_controller == null)
            {
                _controller = FindFirstObjectByType<Major1PowerRestorationController>();
            }
        }

        private void EnsureOperateInteractionType()
        {
            if (TryGetComponent(out InteractableObject interactableObject))
            {
                interactableObject.EnsureInteractionType(Caretaker.Shared.InteractionType.Operate);
            }
        }
    }
}
