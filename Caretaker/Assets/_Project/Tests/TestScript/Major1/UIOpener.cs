using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

using Caretaker.Gameplay;
using Caretaker.Shared;
using Caretaker.World;

namespace Caretaker.Presentation
{
    [AddComponentMenu("Caretaker/Test/Major1/UI Opener")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InteractableObject))]
    public sealed class UIOpener : MonoBehaviour, IOperateAction
    {
        [Header("Puzzle")]
        [SerializeField] private PuzzleUIBase _puzzleUi;
        [SerializeField] private bool _closePuzzleOnAwake = true;

        [Header("Switch Puzzle")]
        [SerializeField] private SwitchGroup _switchGroup;

        private GameObject _runtimeCanvas;
        private GameObject _runtimeEventSystem;

        private void Awake()
        {
            EnsureOperateInteractionType();
            EnsurePuzzleUiInstance();

            if (_closePuzzleOnAwake && _puzzleUi != null)
            {
                _puzzleUi.Close();
            }
        }

        private void Reset()
        {
            EnsureOperateInteractionType();
        }

        private void OnValidate()
        {
            EnsureOperateInteractionType();
        }

        /// <summary>Opens the configured puzzle UI.</summary>
        /// <param name="actor">Player who operated the object.</param>
        /// <returns>True when the puzzle UI was opened.</returns>
        public bool Execute(PlayerController actor)
        {
            EnsurePuzzleUiInstance();

            if (_puzzleUi == null)
            {
                Debug.LogWarning("UIOpener needs a Puzzle UI reference.", this);
                return false;
            }

            if (_puzzleUi is SwitchPopupUI switchPopupUI)
            {
                switchPopupUI.Open(_switchGroup, actor);
                return true;
            }

            _puzzleUi.Open();
            return true;
        }

        private void EnsurePuzzleUiInstance()
        {
            if (_puzzleUi == null)
            {
                return;
            }

            if (!_puzzleUi.gameObject.scene.IsValid())
            {
                _puzzleUi = Instantiate(_puzzleUi);
            }

            Canvas ownerCanvas = _puzzleUi.GetComponentInParent<Canvas>();
            if (ownerCanvas == null)
            {
                ownerCanvas = CreateRuntimeCanvas();
                _puzzleUi.transform.SetParent(ownerCanvas.transform, false);
            }

            if (!ownerCanvas.TryGetComponent(out GraphicRaycaster _))
            {
                ownerCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            EnsureEventSystem();
        }

        private Canvas CreateRuntimeCanvas()
        {
            if (_runtimeCanvas == null)
            {
                _runtimeCanvas = new GameObject(
                    "M1PuzzleRuntimeCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
            }

            Canvas canvas = _runtimeCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler canvasScaler = _runtimeCanvas.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                if (!EventSystem.current.TryGetComponent(out BaseInputModule _))
                {
                    EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>();
                }

                return;
            }

            _runtimeEventSystem = new GameObject(
                "M1PuzzleEventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
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
