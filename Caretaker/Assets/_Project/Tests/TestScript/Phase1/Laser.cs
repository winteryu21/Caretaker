using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

using Caretaker.Gameplay;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class Laser : MonoBehaviour
{
    private const int MAX_HIT_COUNT = 32;
    private const float MIN_HIT_DISTANCE = 0.001f;
    private const float DEFAULT_WIDTH = 0.1f;
    private const float TILE_SAMPLE_INTERVAL = 0.05f;
    private const int TILE_DISTANCE_REFINEMENT_COUNT = 6;

    [Header("Laser Settings")]
    [FormerlySerializedAs("direction")]
    [SerializeField] private Vector2 _direction = Vector2.down;
    [FormerlySerializedAs("maxDistance")]
    [SerializeField, Min(0f)] private float _maxDistance = 20f;
    [SerializeField, Min(0.001f)] private float _width = DEFAULT_WIDTH;
    [SerializeField] private Color _color = Color.red;

    private readonly RaycastHit2D[] _hits = new RaycastHit2D[MAX_HIT_COUNT];

    private LineRenderer _lineRenderer;
    private ContactFilter2D _contactFilter;
    private Material _runtimeMaterial;
    private Respawn _respawn;
    private Tilemap[] _tilemaps;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        DrawLaser();
    }

    private void Initialize()
    {
        if (_lineRenderer == null)
        {
            _lineRenderer = GetComponent<LineRenderer>();
        }

        if (_respawn == null)
        {
            _respawn = GetComponent<Respawn>();
        }

        _lineRenderer.positionCount = 2;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = _width;
        _lineRenderer.endWidth = _width;
        _lineRenderer.startColor = _color;
        _lineRenderer.endColor = _color;
        _lineRenderer.sortingOrder = 10;

        if (_runtimeMaterial == null)
        {
            CreateRuntimeMaterial();
        }

        _tilemaps = transform.root.GetComponentsInChildren<Tilemap>(true);

        _contactFilter = new ContactFilter2D
        {
            useLayerMask = false,
            useTriggers = false
        };
    }

    private void Update()
    {
        DrawLaser();
    }

    private void OnDestroy()
    {
        if (_runtimeMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_runtimeMaterial);
        }
        else
        {
            DestroyImmediate(_runtimeMaterial);
        }
    }

    private void DrawLaser()
    {
        if (_lineRenderer == null)
        {
            return;
        }

        Vector2 startPosition = transform.position;
        Vector2 direction = GetDirection();
        float colliderDistance = FindColliderDistance(startPosition, direction);
        float tilemapDistance = FindTilemapDistance(startPosition, direction);
        float laserLength = Mathf.Min(colliderDistance, tilemapDistance);
        Vector2 endPosition = startPosition + direction * laserLength;

        _lineRenderer.SetPosition(0, startPosition);
        _lineRenderer.SetPosition(1, endPosition);

        if (Application.isPlaying)
        {
            TryRespawnPlayer(startPosition, direction, laserLength);
        }
    }

    private void TryRespawnPlayer(Vector2 startPosition, Vector2 direction, float laserLength)
    {
        if (_respawn == null || laserLength <= 0f)
        {
            return;
        }

        int hitCount = Physics2D.CircleCast(
            startPosition,
            _width * 0.5f,
            direction,
            _contactFilter,
            _hits,
            laserLength);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = _hits[i].collider;
            if (hitCollider == null
                || hitCollider.GetComponentInParent<PlayerController>() == null)
            {
                continue;
            }

            _respawn.RespawnPlayer(hitCollider);
            return;
        }
    }

    private float FindColliderDistance(Vector2 startPosition, Vector2 direction)
    {
        int hitCount = Physics2D.Raycast(
            startPosition,
            direction,
            _contactFilter,
            _hits,
            _maxDistance);

        float nearestDistance = _maxDistance;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = _hits[i].collider;
            if (_hits[i].distance <= MIN_HIT_DISTANCE
                || !IsTileCollider(hitCollider)
                || _hits[i].distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = _hits[i].distance;
        }

        return nearestDistance;
    }

    private float FindTilemapDistance(Vector2 startPosition, Vector2 direction)
    {
        if (_tilemaps == null || _tilemaps.Length == 0)
        {
            return _maxDistance;
        }

        bool startedInsideTile = HasTileAtWorldPosition(startPosition);
        float previousDistance = 0f;

        for (float distance = TILE_SAMPLE_INTERVAL;
             distance <= _maxDistance;
             distance += TILE_SAMPLE_INTERVAL)
        {
            Vector2 samplePosition = startPosition + direction * distance;
            bool hasTile = HasTileAtWorldPosition(samplePosition);

            if (!startedInsideTile && hasTile)
            {
                return RefineTileDistance(
                    startPosition,
                    direction,
                    previousDistance,
                    distance);
            }

            if (startedInsideTile && !hasTile)
            {
                startedInsideTile = false;
            }

            previousDistance = distance;
        }

        return _maxDistance;
    }

    private float RefineTileDistance(
        Vector2 startPosition,
        Vector2 direction,
        float clearDistance,
        float blockedDistance)
    {
        for (int i = 0; i < TILE_DISTANCE_REFINEMENT_COUNT; i++)
        {
            float middleDistance = (clearDistance + blockedDistance) * 0.5f;
            Vector2 samplePosition = startPosition + direction * middleDistance;

            if (HasTileAtWorldPosition(samplePosition))
            {
                blockedDistance = middleDistance;
            }
            else
            {
                clearDistance = middleDistance;
            }
        }

        return blockedDistance;
    }

    private bool HasTileAtWorldPosition(Vector2 worldPosition)
    {
        for (int i = 0; i < _tilemaps.Length; i++)
        {
            Tilemap tilemap = _tilemaps[i];
            if (tilemap == null || !tilemap.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3Int cellPosition = tilemap.WorldToCell(worldPosition);
            if (tilemap.HasTile(cellPosition))
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 GetDirection()
    {
        return _direction.sqrMagnitude > 0f
            ? _direction.normalized
            : Vector2.down;
    }

    private static bool IsTileCollider(Collider2D hitCollider)
    {
        return hitCollider is TilemapCollider2D
            || hitCollider is CompositeCollider2D
            || hitCollider.GetComponentInParent<Caretaker.World.MoveTile>() != null;
    }

    private void CreateRuntimeMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            Debug.LogError("Laser could not find a compatible sprite shader.", this);
            return;
        }

        _runtimeMaterial = new Material(shader)
        {
            name = "Laser Runtime Material",
            color = _color
        };

        _lineRenderer.material = _runtimeMaterial;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 startPosition = transform.position;
        Vector2 direction = _direction.sqrMagnitude > 0f
            ? _direction.normalized
            : Vector2.down;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(startPosition, startPosition + direction * _maxDistance);
    }
}
