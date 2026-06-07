using UnityEngine;
using Caretaker.World;


public class PastPlatformTriggerTest : MonoBehaviour
{
    [SerializeField]
    private CausalTrigger _causalTrigger;
    private void Awake()
    {
        _causalTrigger = GetComponent<CausalTrigger>();
        if (_causalTrigger == null)
            Debug.LogError("[PastPlatformTrigger] CausalTrigger 컴포넌트가 없어요.", this);
    }
    // 2D 트리거 콜백 – 플레이어가 들어오면 인과 트리거를 발동
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player 태그가 붙은 오브젝트만 작동하도록 필터
        if (other.CompareTag("Player"))
        {
            Debug.Log("[PastPlatformTrigger] Player entered – Fire()", this);
            _causalTrigger?.Fire();   // 인과 시스템에 트리거 전송
        }
    }
 }

