using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class FallingRockTrigger : MonoBehaviour
{
    [Header("Target Rock (떨어뜨릴 오브젝트)")]
    [Tooltip("플레이어가 들어오면 이 오브젝트가 중력에 의해 떨어집니다.")]
    [SerializeField] private GameObject _rockObject;
    [SerializeField] private float _kinematicReturnDelay = 1f;
    private List<Rigidbody2D> _rockRigidbodies = new List<Rigidbody2D>();
    private void Awake()
    {
        if (_rockObject == null)
        {
            Debug.LogError("[AreaFallTrigger] RocksRoot가 지정되지 않았습니다.", this);
            return;
        }
        // 루트 아래 모든 자식 중 Rigidbody2D를 가진 오브젝트를 찾아 리스트에 저장
        _rockRigidbodies.Clear();
        foreach (Transform child in _rockObject.transform)
        {
            var rb = child.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                _rockRigidbodies.Add(rb);
                // 초기 상태를 Kinematic(고정)으로 설정 → 에디터에서 움직임을 미리 배치 가능
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
            }
            else
            {
                Debug.LogWarning(
                    $"[AreaFallTrigger] 자식 '{child.name}'에 Rigidbody2D가 없어 무시됩니다.", this);
            }
        }
        if (_rockRigidbodies.Count == 0)
            Debug.LogWarning("[AreaFallTrigger] RocksRoot 아래에 Rigidbody2D가 있는 자식이 없습니다.", this);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (_rockRigidbodies.Count == 0) return;

        Debug.Log("[AreaFallTrigger] Player entered – 모든 돌을 떨어뜨립니다.", this);
        // 모든 돌을 Dynamic 로 바꾸고 중력을 활성화
        foreach (var rb in _rockRigidbodies)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;               // 기본 중력 사용
        }

        StartCoroutine(ReturnToKinematicRoutine());
    }


    private IEnumerator ReturnToKinematicRoutine()
    {
        // 설정한 시간만큼 대기 (기본값 1초)
        yield return new WaitForSeconds(_kinematicReturnDelay);

        Debug.Log($"[AreaFallTrigger] {_kinematicReturnDelay}초 경과 – 모든 돌을 Kinematic으로 전환합니다.", this);

        foreach (var rb in _rockRigidbodies)
        {
            if (rb != null)
            {
                // 중요: Kinematic으로 바꿀 때 기존 속도가 남아있으면 미끄러지듯 움직일 수 있으므로 속도를 0으로 만듭니다.
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;

                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f; // Awake 때와 동일하게 중력도 0으로 복구


            }
        }
    }


}
