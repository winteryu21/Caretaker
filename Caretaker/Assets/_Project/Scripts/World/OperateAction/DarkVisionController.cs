using UnityEngine;

using Caretaker.Gameplay;
using Caretaker.Shared;

namespace Caretaker.World
{
    /// <summary>
    /// Applies a local dark-vision effect by zooming the camera and masking darkness around the operating player.
    /// </summary>
    [DefaultExecutionOrder(200)]
    [AddComponentMenu("Caretaker/Operate Action/Dark Vision Controller")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InteractableObject))]
    public sealed class DarkVisionController : MonoBehaviour, IOperateAction
    {
        public enum DarkVisionMode
        {
            Enable,
            Disable,
            Toggle
        }

        private const string FOG_OVERLAY_NAME = "DarkVisionFogOverlay";
        private const string VISION_HOLE_NAME = "DarkVisionHole";
        private const int CIRCLE_TEXTURE_RESOLUTION = 256;

        [Header("Mode")]
        [SerializeField] private DarkVisionMode _mode = DarkVisionMode.Enable;

        [Header("Vision")]
        [SerializeField] private float _visionRadius = 2.5f;
        [SerializeField] private Vector2 _visionCenterOffset = new(0f, 0.75f);
        [SerializeField] [Range(0f, 1f)] private float _darkness = 0.85f;
        [SerializeField] private float _transitionSpeed = 5f;

        [Header("Camera")]
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private bool _captureCurrentSizeAsNormal = true;
        [SerializeField] private float _normalOrthographicSize = 5f;
        [SerializeField] private float _restrictedOrthographicSize = 3f;
        [SerializeField] private float _zoomSpeed = 4f;

        [Header("Rendering")]
        [SerializeField] private string _sortingLayerName = "Default";
        [SerializeField] private int _fogSortingOrder = 1000;
        [SerializeField] private Vector2 _fogWorldSize = new(200f, 200f);
        [SerializeField] private float _effectZ = 0f;

        private static GameObject _fogOverlay;
        private static GameObject _visionHole;
        private static SpriteRenderer _fogRenderer;
        private static SpriteMask _visionMask;
        private static Camera _activeCamera;
        private static Transform _followTarget;
        private static float _normalCameraSize;
        private static float _targetCameraSize;
        private static float _currentAlpha;
        private static float _targetAlpha;
        private static bool _hasNormalCameraSize;
        private static bool _isRestricted;

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
            _visionRadius = Mathf.Max(0.01f, _visionRadius);
            _transitionSpeed = Mathf.Max(0.01f, _transitionSpeed);
            _normalOrthographicSize = Mathf.Max(0.01f, _normalOrthographicSize);
            _restrictedOrthographicSize = Mathf.Max(0.01f, _restrictedOrthographicSize);
            _zoomSpeed = Mathf.Max(0.01f, _zoomSpeed);
            _fogWorldSize.x = Mathf.Max(1f, _fogWorldSize.x);
            _fogWorldSize.y = Mathf.Max(1f, _fogWorldSize.y);
            EnsureOperateInteractionType();
        }

        private void Update()
        {
            TickEffect(Time.deltaTime);
        }

        private void LateUpdate()
        {
            FollowTargets();
        }

        /// <summary>
        /// Applies or clears the dark vision effect for the player who operated this object.
        /// </summary>
        /// <param name="actor">Player that operated the object.</param>
        /// <returns>True when the effect command was applied.</returns>
        public bool Execute(PlayerController actor)
        {
            if (actor == null)
            {
                return false;
            }

            return _mode switch
            {
                DarkVisionMode.Enable => SetRestricted(true, actor.transform),
                DarkVisionMode.Disable => SetRestricted(false, actor.transform),
                DarkVisionMode.Toggle => SetRestricted(!_isRestricted, actor.transform),
                _ => false
            };
        }

        private bool SetRestricted(bool restricted, Transform followTarget)
        {
            Camera camera = ResolveCamera();
            if (camera == null || !camera.orthographic)
            {
                Debug.LogWarning("Dark vision requires an orthographic camera.", this);
                return false;
            }

            EnsureEffectObjects();
            CacheNormalCameraSize(camera);

            _activeCamera = camera;
            _followTarget = followTarget;
            _isRestricted = restricted;
            _targetAlpha = restricted ? _darkness : 0f;
            _targetCameraSize = restricted ? _restrictedOrthographicSize : _normalCameraSize;

            if (restricted)
            {
                SetEffectObjectsActive(true);
                FollowTargets();
            }

            return true;
        }

        private void TickEffect(float deltaTime)
        {
            if (_fogRenderer == null && _activeCamera == null)
            {
                return;
            }

            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, _transitionSpeed * deltaTime);

            if (_fogRenderer != null)
            {
                Color color = _fogRenderer.color;
                color.a = _currentAlpha;
                _fogRenderer.color = color;
            }

            if (_activeCamera != null)
            {
                _activeCamera.orthographicSize = Mathf.MoveTowards(
                    _activeCamera.orthographicSize,
                    _targetCameraSize,
                    _zoomSpeed * deltaTime);
            }

            if (!_isRestricted && _currentAlpha <= 0.01f)
            {
                SetEffectObjectsActive(false);
            }
        }

        private void FollowTargets()
        {
            if (_followTarget != null && _visionHole != null)
            {
                _visionHole.transform.position = new Vector3(
                    _followTarget.position.x + _visionCenterOffset.x,
                    _followTarget.position.y + _visionCenterOffset.y,
                    _effectZ);
            }

            if (_activeCamera != null && _fogOverlay != null)
            {
                _fogOverlay.transform.position = new Vector3(
                    _activeCamera.transform.position.x,
                    _activeCamera.transform.position.y,
                    _effectZ);
            }
        }

        private void CacheNormalCameraSize(Camera camera)
        {
            if (_hasNormalCameraSize && _activeCamera == camera)
            {
                return;
            }

            _normalCameraSize = _captureCurrentSizeAsNormal
                ? camera.orthographicSize
                : _normalOrthographicSize;
            _targetCameraSize = _normalCameraSize;
            _hasNormalCameraSize = true;
        }

        private void EnsureEffectObjects()
        {
            if (_fogOverlay == null || _fogRenderer == null)
            {
                CreateFogOverlay();
            }

            if (_visionHole == null || _visionMask == null)
            {
                CreateVisionHole();
            }
        }

        private void CreateFogOverlay()
        {
            _fogOverlay = new GameObject(FOG_OVERLAY_NAME);
            _fogRenderer = _fogOverlay.AddComponent<SpriteRenderer>();
            _fogRenderer.sprite = CreateSolidSprite(Color.black);
            _fogRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            _fogRenderer.sortingLayerName = _sortingLayerName;
            _fogRenderer.sortingOrder = _fogSortingOrder;
            _fogOverlay.transform.localScale = new Vector3(_fogWorldSize.x, _fogWorldSize.y, 1f);

            Color color = _fogRenderer.color;
            color.a = 0f;
            _fogRenderer.color = color;
            _fogOverlay.SetActive(false);
        }

        private void CreateVisionHole()
        {
            _visionHole = new GameObject(VISION_HOLE_NAME);
            _visionMask = _visionHole.AddComponent<SpriteMask>();
            _visionMask.sprite = CreateCircleMaskSprite(_visionRadius);
            _visionHole.SetActive(false);
        }

        private void SetEffectObjectsActive(bool active)
        {
            if (_fogOverlay != null)
            {
                _fogOverlay.SetActive(active);
            }

            if (_visionHole != null)
            {
                _visionHole.SetActive(active);
            }
        }

        private Camera ResolveCamera()
        {
            if (_targetCamera != null)
            {
                return _targetCamera;
            }

            _targetCamera = Camera.main;
            return _targetCamera;
        }

        private static Sprite CreateSolidSprite(Color color)
        {
            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private static Sprite CreateCircleMaskSprite(float visionRadius)
        {
            Texture2D texture = new(CIRCLE_TEXTURE_RESOLUTION, CIRCLE_TEXTURE_RESOLUTION, TextureFormat.RGBA32, false);
            Vector2 center = new(CIRCLE_TEXTURE_RESOLUTION * 0.5f, CIRCLE_TEXTURE_RESOLUTION * 0.5f);
            float maxRadius = CIRCLE_TEXTURE_RESOLUTION * 0.5f;
            float edgeSoftness = 0.15f;

            for (int y = 0; y < CIRCLE_TEXTURE_RESOLUTION; y++)
            {
                for (int x = 0; x < CIRCLE_TEXTURE_RESOLUTION; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / maxRadius;
                    float alpha = CalculateMaskAlpha(distance, edgeSoftness);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            float pixelsPerUnit = CIRCLE_TEXTURE_RESOLUTION / (visionRadius * 2f);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, CIRCLE_TEXTURE_RESOLUTION, CIRCLE_TEXTURE_RESOLUTION),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
        }

        private static float CalculateMaskAlpha(float normalizedDistance, float edgeSoftness)
        {
            if (normalizedDistance <= 1f - edgeSoftness)
            {
                return 1f;
            }

            if (normalizedDistance <= 1f)
            {
                float edgeProgress = (normalizedDistance - (1f - edgeSoftness)) / edgeSoftness;
                return 1f - Mathf.SmoothStep(0f, 1f, edgeProgress);
            }

            return 0f;
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
