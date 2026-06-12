using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using Caretaker.Gameplay;
using Caretaker.Shared;

namespace Caretaker.World
{
    /// <summary>
    /// Opens a full-screen image popup through an operate interaction.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InteractableObject))]
    [AddComponentMenu("Caretaker/Operate Action/Image Popup Operate Action")]
    public sealed class ImagePopupOperateAction : MonoBehaviour, IOperateAction
    {
        [Header("Popup")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Image _popupImage;
        [SerializeField] private Sprite _popupSprite;
        [SerializeField] private Button _closeButton;

        [Header("Behavior")]
        [SerializeField] private bool _startClosed = true;
        [SerializeField] private bool _blockActorInput = true;
        [SerializeField] private bool _closeOnEscapeOrInteract = true;
        [SerializeField] private bool _closeWhenOperatedAgain = true;

        private PlayerController _blockedActor;
        private bool _blockedActorWasInputBlocked;
        private int _openedFrame = -1;

        /// <summary>
        /// Gets whether the popup panel is currently visible.
        /// </summary>
        public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

        private void Awake()
        {
            EnsureOperateInteractionType();
            RefreshImage();

            if (_startClosed)
            {
                SetPanelVisible(false);
            }
        }

        private void Reset()
        {
            EnsureOperateInteractionType();
            RefreshImage();
        }

        private void OnValidate()
        {
            EnsureOperateInteractionType();
            RefreshImage();
        }

        private void OnEnable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }
        }

        private void OnDisable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
            }

            Close();
        }

        private void Update()
        {
            if (!IsOpen || !_closeOnEscapeOrInteract || Time.frameCount == _openedFrame)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        /// <summary>
        /// Opens the configured popup panel.
        /// </summary>
        /// <param name="actor">The player that requested the interaction.</param>
        /// <returns>True when the popup state changed.</returns>
        public bool Execute(PlayerController actor)
        {
            if (_panelRoot == null)
            {
                Debug.LogWarning("Image popup operate action has no panel root assigned.", this);
                return false;
            }

            if (IsOpen)
            {
                if (!_closeWhenOperatedAgain)
                {
                    return false;
                }

                Close();
                return true;
            }

            Open(actor);
            return true;
        }

        /// <summary>
        /// Closes the popup panel and restores the actor input state.
        /// </summary>
        public void Close()
        {
            if (!IsOpen && _blockedActor == null)
            {
                return;
            }

            SetPanelVisible(false);
            RestoreActorInput();
            _openedFrame = -1;
        }

        private void Open(PlayerController actor)
        {
            RefreshImage();
            SetPanelVisible(true);
            BlockActorInput(actor);
            _openedFrame = Time.frameCount;
        }

        private void SetPanelVisible(bool isVisible)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(isVisible);
            }
        }

        private void BlockActorInput(PlayerController actor)
        {
            if (!_blockActorInput || actor == null)
            {
                return;
            }

            _blockedActor = actor;
            _blockedActorWasInputBlocked = actor.IsInputBlocked;

            if (!_blockedActorWasInputBlocked)
            {
                actor.SetInputBlocked(true);
            }
        }

        private void RestoreActorInput()
        {
            if (_blockedActor == null)
            {
                return;
            }

            if (!_blockedActorWasInputBlocked)
            {
                _blockedActor.SetInputBlocked(false);
            }

            _blockedActor = null;
            _blockedActorWasInputBlocked = false;
        }

        private void RefreshImage()
        {
            if (_popupImage == null)
            {
                return;
            }

            if (_popupSprite != null)
            {
                _popupImage.sprite = _popupSprite;
            }

            _popupImage.preserveAspect = true;
            _popupImage.enabled = _popupImage.sprite != null;
        }

        private void EnsureOperateInteractionType()
        {
            if (TryGetComponent(out InteractableObject interactableObject))
            {
                interactableObject.EnsureInteractionType(InteractionType.Operate);
            }
        }
    }
}
