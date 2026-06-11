using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Caretaker.Presentation
{

    [AddComponentMenu("Caretaker/Puzzle/Circuit Puzzle Node UI")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class CircuitPuzzleNodeUI : MonoBehaviour, IPointerClickHandler
    {
        private const int STATE_COUNT = 5;
        private const float TEE_VISUAL_ROTATION_OFFSET_Z = 180f;
        private static int _lastHandledClickFrame = -1;

        [Header("Node")]
        [SerializeField] private bool _isDummyNode;
        [SerializeField] [Range(0, STATE_COUNT - 1)] private int _stateIndex;
        [SerializeField] private bool _useSpriteClickFallback = true;

        [Header("Visuals")]
        [Tooltip("Optional root used only as a fallback when no visual roots are assigned.")]
        [SerializeField] private RectTransform _visualRoot;
        [SerializeField] private GameObject _dummyRoot;
        [SerializeField] private GameObject _teeRoot;
        [SerializeField] private GameObject _crossRoot;

        [Header("Click Area")]
        [Tooltip("Sprite renderers used as click bounds. Empty means all child SpriteRenderers are used.")]
        [SerializeField] private SpriteRenderer[] _clickRenderers;

        private RectTransform _rectTransform;
        private Canvas _canvas;
        private Camera _eventCamera;

        public event Action<CircuitPuzzleNodeUI> OnDirectionChanged;

        public int StateIndex => _stateIndex;

        public bool IsDummyNode => _isDummyNode;

        public CircuitNodeDirection Direction { get; private set; }

        public CircuitNodeShape Shape { get; private set; }

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            CacheClickRenderers();
            CacheDefaultVisualRoot();
            ApplyState(false);
        }

        private void OnValidate()
        {
            _rectTransform = transform as RectTransform;
            CacheClickRenderers();
            CacheDefaultVisualRoot();
            _stateIndex = Mathf.Clamp(_stateIndex, 0, STATE_COUNT - 1);
            ApplyState(false);
        }

        private void Update()
        {
            if (!_useSpriteClickFallback ||
                Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame ||
                _lastHandledClickFrame == Time.frameCount)
            {
                return;
            }

            if (IsPointerInsideNode())
            {
                _lastHandledClickFrame = Time.frameCount;
                HandleClicked();
            }
        }

        public void AdvanceState()
        {
            if (_isDummyNode)
            {
                return;
            }

            SetStateIndex((_stateIndex + 1) % STATE_COUNT);
        }

        public void SetStateIndex(int stateIndex)
        {
            if (_isDummyNode)
            {
                _stateIndex = 0;
                ApplyState(false);
                return;
            }

            int clampedIndex = Mathf.Clamp(stateIndex, 0, STATE_COUNT - 1);
            if (_stateIndex == clampedIndex)
            {
                ApplyState(false);
                return;
            }

            _stateIndex = clampedIndex;
            ApplyState(true);
        }

        public bool HasPort(CircuitNodeDirection worldDirection)
        {
            if (_isDummyNode)
            {
                return true;
            }

            int rotationSteps = (int)Direction;

            return Shape switch
            {
                CircuitNodeShape.Tee => IsRotatedPort(CircuitNodeDirection.Left, worldDirection, rotationSteps)
                    || IsRotatedPort(CircuitNodeDirection.Up, worldDirection, rotationSteps)
                    || IsRotatedPort(CircuitNodeDirection.Right, worldDirection, rotationSteps),
                CircuitNodeShape.Cross => true,
                _ => false
            };
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_lastHandledClickFrame == Time.frameCount)
            {
                return;
            }

            _lastHandledClickFrame = Time.frameCount;
            HandleClicked();
        }

        public void HandleClicked()
        {
            AdvanceState();
        }

        private void CacheClickRenderers()
        {
            if (_clickRenderers == null || _clickRenderers.Length == 0)
            {
                _clickRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }
        }

        private void CacheDefaultVisualRoot()
        {
            if (_rectTransform == null)
            {
                _rectTransform = transform as RectTransform;
            }

            if (_visualRoot == null)
            {
                _visualRoot = _rectTransform;
            }
        }

        private bool IsPointerInsideNode()
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Camera eventCamera = GetEventCamera();

            if (_clickRenderers != null)
            {
                for (int i = 0; i < _clickRenderers.Length; i++)
                {
                    SpriteRenderer clickRenderer = _clickRenderers[i];
                    if (IsPointerInsideRenderer(clickRenderer, mouseScreenPosition, eventCamera))
                    {
                        return true;
                    }
                }
            }

            if (_clickRenderers != null && _clickRenderers.Length > 0)
            {
                return false;
            }

            return _visualRoot != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    _visualRoot,
                    mouseScreenPosition,
                    eventCamera);
        }

        private static bool IsPointerInsideRenderer(
            SpriteRenderer spriteRenderer,
            Vector2 mouseScreenPosition,
            Camera eventCamera)
        {
            if (spriteRenderer == null || !spriteRenderer.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (eventCamera == null)
            {
                return false;
            }

            Bounds bounds = spriteRenderer.bounds;
            Vector3 min = eventCamera.WorldToScreenPoint(bounds.min);
            Vector3 max = eventCamera.WorldToScreenPoint(bounds.max);
            Rect screenRect = Rect.MinMaxRect(
                Mathf.Min(min.x, max.x),
                Mathf.Min(min.y, max.y),
                Mathf.Max(min.x, max.x),
                Mathf.Max(min.y, max.y));

            return screenRect.Contains(mouseScreenPosition);
        }

        private void ApplyState(bool notifyChanged)
        {
            ResolveState(_stateIndex, out CircuitNodeShape shape, out CircuitNodeDirection direction);

            Shape = _isDummyNode ? CircuitNodeShape.Dummy : shape;
            Direction = _isDummyNode ? CircuitNodeDirection.Right : direction;
            ApplyVisual();

            if (notifyChanged)
            {
                OnDirectionChanged?.Invoke(this);
            }
        }

        private void ApplyVisual()
        {
            GameObject activeRoot = ApplyVisualAsset();
            float rotationZ = CircuitNodeDirectionUtility.GetVisualRotationZ(Direction);
            float teeRotationZ = rotationZ + TEE_VISUAL_ROTATION_OFFSET_Z;

            ApplyRootSpriteRotation(_dummyRoot, activeRoot == _dummyRoot ? rotationZ : 0f);
            ApplyRootSpriteRotation(_teeRoot, activeRoot == _teeRoot ? teeRotationZ : 0f);
            ApplyRootSpriteRotation(_crossRoot, activeRoot == _crossRoot ? rotationZ : 0f);

            if (activeRoot != null || _visualRoot == null)
            {
                return;
            }

            _visualRoot.localEulerAngles = new Vector3(
                0f,
                0f,
                rotationZ);
        }

        private GameObject ApplyVisualAsset()
        {
            bool isDummy = Shape == CircuitNodeShape.Dummy;
            bool isTee = Shape == CircuitNodeShape.Tee;
            bool isCross = Shape == CircuitNodeShape.Cross;

            SetVisualRootActive(_dummyRoot, isDummy);
            SetVisualRootActive(_teeRoot, isTee);
            SetVisualRootActive(_crossRoot, isCross);

            if (isDummy)
            {
                return _dummyRoot;
            }

            if (isTee)
            {
                return _teeRoot;
            }

            if (isCross)
            {
                return _crossRoot;
            }

            return null;
        }

        private static void ResolveState(
            int stateIndex,
            out CircuitNodeShape shape,
            out CircuitNodeDirection direction)
        {
            if (stateIndex == 0)
            {
                shape = CircuitNodeShape.Cross;
                direction = CircuitNodeDirection.Up;
                return;
            }

            if (stateIndex >= 1 && stateIndex <= 4)
            {
                shape = CircuitNodeShape.Tee;
                direction = (CircuitNodeDirection)(stateIndex - 1);
                return;
            }

            shape = CircuitNodeShape.Cross;
            direction = CircuitNodeDirection.Up;
        }

        private static void SetVisualRootActive(GameObject visualRoot, bool isActive)
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(isActive);
            }
        }

        private static void ApplyRootSpriteRotation(GameObject visualRoot, float rotationZ)
        {
            if (visualRoot == null)
            {
                return;
            }

            SpriteRenderer[] spriteRenderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];
                if (spriteRenderer != null)
                {
                    Transform spriteTransform = spriteRenderer.transform;
                    Vector3 localEulerAngles = spriteTransform.localEulerAngles;
                    localEulerAngles.z = rotationZ;
                    spriteTransform.localEulerAngles = localEulerAngles;
                }
            }
        }

        private Camera GetEventCamera()
        {
            if (_eventCamera != null)
            {
                return _eventCamera;
            }

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            if (_canvas == null)
            {
                _eventCamera = Camera.main;
                return _eventCamera;
            }

            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                _eventCamera = Camera.main;
                return _eventCamera;
            }

            _eventCamera = _canvas.worldCamera != null ? _canvas.worldCamera : Camera.main;
            return _eventCamera;
        }

        private static bool IsRotatedPort(
            CircuitNodeDirection baseDirection,
            CircuitNodeDirection worldDirection,
            int rotationSteps)
        {
            return CircuitNodeDirectionUtility.RotateClockwise(baseDirection, rotationSteps) == worldDirection;
        }
    }
}
