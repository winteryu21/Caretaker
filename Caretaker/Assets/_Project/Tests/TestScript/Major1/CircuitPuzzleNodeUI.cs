using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Caretaker.Presentation
{

    [AddComponentMenu("Caretaker/Puzzle/Circuit Puzzle Node UI")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class CircuitPuzzleNodeUI : MonoBehaviour, IPointerClickHandler
    {
        private const int STATE_COUNT = 5;
        private const float CLICK_DEBOUNCE_SECONDS = 0.12f;
        private const float TEE_VISUAL_ROTATION_OFFSET_Z = 180f;
        private static int _lastHandledClickFrame = -1;
        private static float _lastHandledClickTime = -CLICK_DEBOUNCE_SECONDS;

        [Header("Node")]
        [SerializeField] private bool _isDummyNode;
        [SerializeField] [Range(0, STATE_COUNT - 1)] private int _stateIndex;
        [SerializeField] private bool _useSpriteClickFallback = true;

        [Header("Visuals")]
        [Tooltip("Optional root used only as a fallback when no visual roots are assigned.")]
        [SerializeField] private RectTransform _visualRoot;
        [SerializeField] private Image _nodeImage;
        [SerializeField] private Sprite _dummySprite;
        [SerializeField] private Sprite _teeSprite;
        [SerializeField] private Sprite _crossSprite;
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
            CacheImage();
            CacheClickRenderers();
            CacheDefaultVisualRoot();
            ApplyState(false);
        }

        private void OnValidate()
        {
            _rectTransform = transform as RectTransform;
            CacheImage();
            CacheClickRenderers();
            CacheDefaultVisualRoot();
            _stateIndex = Mathf.Clamp(_stateIndex, 0, STATE_COUNT - 1);
            ApplyState(false);
        }

        private void Update()
        {
            if (!_useSpriteClickFallback ||
                Mouse.current == null ||
                !Mouse.current.leftButton.wasReleasedThisFrame ||
                !CanHandleClick())
            {
                return;
            }

            if (IsPointerInsideNode())
            {
                HandleClickIfAllowed();
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
            HandleClickIfAllowed();
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

        private void CacheImage()
        {
            if (_nodeImage == null)
            {
                _nodeImage = GetComponent<Image>();
            }

            if (_nodeImage == null)
            {
                _nodeImage = GetComponentInChildren<Image>(true);
            }
        }

        private void HandleClickIfAllowed()
        {
            if (!CanHandleClick())
            {
                return;
            }

            _lastHandledClickFrame = Time.frameCount;
            _lastHandledClickTime = Time.unscaledTime;
            HandleClicked();
        }

        private static bool CanHandleClick()
        {
            return _lastHandledClickFrame != Time.frameCount &&
                Time.unscaledTime - _lastHandledClickTime >= CLICK_DEBOUNCE_SECONDS;
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
            ApplyImageRotation(activeRoot == _teeRoot ? teeRotationZ : rotationZ);

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
            ApplyImageVisual(isDummy, isTee, isCross);

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

        private void ApplyImageVisual(bool isDummy, bool isTee, bool isCross)
        {
            if (_nodeImage == null)
            {
                return;
            }

            Sprite sprite = null;
            if (isDummy)
            {
                sprite = _dummySprite != null ? _dummySprite : GetFirstSprite(_dummyRoot);
            }
            else if (isTee)
            {
                sprite = _teeSprite != null ? _teeSprite : GetFirstSprite(_teeRoot);
            }
            else if (isCross)
            {
                sprite = _crossSprite != null ? _crossSprite : GetFirstSprite(_crossRoot);
            }

            if (sprite != null)
            {
                _nodeImage.sprite = sprite;
            }
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

        private void ApplyImageRotation(float rotationZ)
        {
            if (_nodeImage == null)
            {
                return;
            }

            RectTransform imageRectTransform = _nodeImage.rectTransform;
            Vector3 localEulerAngles = imageRectTransform.localEulerAngles;
            localEulerAngles.z = rotationZ;
            imageRectTransform.localEulerAngles = localEulerAngles;
        }

        private static Sprite GetFirstSprite(GameObject visualRoot)
        {
            if (visualRoot == null)
            {
                return null;
            }

            SpriteRenderer spriteRenderer = visualRoot.GetComponentInChildren<SpriteRenderer>(true);
            return spriteRenderer != null ? spriteRenderer.sprite : null;
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
                return null;
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
