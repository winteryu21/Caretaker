using System.Collections.Generic;

using UnityEngine;

using Caretaker.Gameplay;
using Caretaker.Shared;
using Caretaker.World;

namespace Caretaker.Presentation
{
    /// <summary>
    /// Opens a puzzle UI when this control panel receives an interaction.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InteractableObject))]
    public sealed class ControlPanelPuzzleOpener : MonoBehaviour
    {
        [Header("Puzzle")]
        [SerializeField] private PuzzleUIBase _puzzleUi;
        [SerializeField] private InteractionType _openInteractionType = InteractionType.Operate;
        [SerializeField] private bool _closePuzzleOnAwake = true;

        private readonly List<PlayerController> _subscribedPlayers = new();
        private InteractableObject _interactableObject;

        private void Awake()
        {
            _interactableObject = GetComponent<InteractableObject>();
            CachePuzzleUi();

            if (_closePuzzleOnAwake && _puzzleUi != null)
            {
                _puzzleUi.Close();
            }
        }

        private void OnEnable()
        {
            SubscribeExistingPlayers();
        }

        private void Start()
        {
            SubscribeExistingPlayers();
        }

        private void OnDisable()
        {
            UnsubscribeAllPlayers();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (TryGetPlayerController(other, out PlayerController playerController))
            {
                SubscribePlayer(playerController);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (TryGetPlayerController(other, out PlayerController playerController))
            {
                UnsubscribePlayer(playerController);
            }
        }

        private void HandleInteractionResolved(InteractableObject target, InteractionType interactionType)
        {
            if (target != _interactableObject || interactionType != _openInteractionType)
            {
                return;
            }

            if (_puzzleUi == null)
            {
                CachePuzzleUi();

                if (_puzzleUi == null)
                {
                    Debug.LogWarning("Control panel interaction resolved, but Puzzle UI was not found.", this);
                    return;
                }
            }

            Debug.Log(
                $"Control panel opening puzzle UI: object={_interactableObject.ObjectId}, puzzle={_puzzleUi.name}",
                this);
            _puzzleUi.Open();
        }

        private void CachePuzzleUi()
        {
            if (_puzzleUi != null)
            {
                return;
            }

            _puzzleUi = FindFirstObjectByType<PuzzleUIBase>(FindObjectsInactive.Include);
        }

        private void SubscribeExistingPlayers()
        {
            PlayerController[] playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < playerControllers.Length; i++)
            {
                SubscribePlayer(playerControllers[i]);
            }
        }

        private void SubscribePlayer(PlayerController playerController)
        {
            if (playerController == null || _subscribedPlayers.Contains(playerController))
            {
                return;
            }

            playerController.OnInteractionResolved += HandleInteractionResolved;
            _subscribedPlayers.Add(playerController);
        }

        private void UnsubscribePlayer(PlayerController playerController)
        {
            if (playerController == null || !_subscribedPlayers.Remove(playerController))
            {
                return;
            }

            playerController.OnInteractionResolved -= HandleInteractionResolved;
        }

        private void UnsubscribeAllPlayers()
        {
            for (int i = 0; i < _subscribedPlayers.Count; i++)
            {
                PlayerController playerController = _subscribedPlayers[i];
                if (playerController != null)
                {
                    playerController.OnInteractionResolved -= HandleInteractionResolved;
                }
            }

            _subscribedPlayers.Clear();
        }

        private static bool TryGetPlayerController(Collider2D collider2D, out PlayerController playerController)
        {
            playerController = null;
            if (collider2D == null)
            {
                return false;
            }

            playerController = collider2D.GetComponentInParent<PlayerController>();
            return playerController != null;
        }
    }
}
