using Caretaker.Gameplay;
using Caretaker.Shared;
using TMPro;
using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// 로컬 플레이어의 조사/전투 모드를 HUD에 표시합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActionModeHudPresenter : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private LocalWorldPlayerSpawner _playerSpawner;

        [Header("View")]
        [SerializeField] private TMP_Text _modeText;
        [SerializeField] private GameObject _investigationIndicator;
        [SerializeField] private GameObject _combatIndicator;
        [SerializeField] private Color _investigationColor = new(0.35f, 0.85f, 1f);
        [SerializeField] private Color _combatColor = new(1f, 0.3f, 0.2f);

        private PlayerController _playerController;

        private void Awake()
        {
            Render(PlayerActionMode.Investigation);
        }

        private void OnEnable()
        {
            LocalWorldPlayerSpawner.OnCurrentPlayerChanged += HandleCurrentPlayerChanged;
            NetworkPlayerOwnerGate.OnLocalOwnerPlayerSpawned += HandleCurrentPlayerChanged;
            NetworkPlayerOwnerGate.OnLocalOwnerPlayerDespawned += HandleLocalOwnerPlayerDespawned;

            if (_playerSpawner != null)
            {
                BindPlayer(_playerSpawner.CurrentPlayer);
            }
        }

        private void OnDisable()
        {
            LocalWorldPlayerSpawner.OnCurrentPlayerChanged -= HandleCurrentPlayerChanged;
            NetworkPlayerOwnerGate.OnLocalOwnerPlayerSpawned -= HandleCurrentPlayerChanged;
            NetworkPlayerOwnerGate.OnLocalOwnerPlayerDespawned -= HandleLocalOwnerPlayerDespawned;
            UnbindPlayer();
        }

        private void HandleCurrentPlayerChanged(GameObject player)
        {
            BindPlayer(player);
        }

        private void HandleLocalOwnerPlayerDespawned(GameObject player)
        {
            if (_playerController != null && _playerController.gameObject == player)
            {
                UnbindPlayer();
            }
        }

        private void BindPlayer(GameObject player)
        {
            UnbindPlayer();

            if (player == null || !player.TryGetComponent(out _playerController))
            {
                return;
            }

            _playerController.OnActionModeChanged += HandleActionModeChanged;
            Render(_playerController.ActionMode);
        }

        private void UnbindPlayer()
        {
            if (_playerController != null)
            {
                _playerController.OnActionModeChanged -= HandleActionModeChanged;
                _playerController = null;
            }
        }

        private void HandleActionModeChanged(PlayerActionMode actionMode)
        {
            Render(actionMode);
        }

        private void Render(PlayerActionMode actionMode)
        {
            bool isCombat = actionMode == PlayerActionMode.Combat;

            if (_modeText != null)
            {
                _modeText.text = isCombat ? "COMBAT MODE" : "INVESTIGATION MODE";
                _modeText.color = isCombat ? _combatColor : _investigationColor;
            }

            if (_investigationIndicator != null)
            {
                _investigationIndicator.SetActive(!isCombat);
            }

            if (_combatIndicator != null)
            {
                _combatIndicator.SetActive(isCombat);
            }
        }
    }
}
