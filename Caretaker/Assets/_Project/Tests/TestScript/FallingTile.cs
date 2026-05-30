using System.Collections;
using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    [Header("떨어지는 설정")]
    public float respawnTime = 3f;      // 다시 생성되기까지 시간

    [Header("흔들림 설정")]
    public float shakeDuration = 0.4f; // 흔들리는 시간
    public float shakeAmount = 0.05f;  // 흔들림 강도

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;

    private Vector3 startPosition;
    private bool activated = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        startPosition = transform.position;

        // 시작 시 고정
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (activated) return;

        if (!collision.gameObject.CompareTag("Player"))
            return;

        // 플레이어가 위에서 밟았을 때만 작동
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f)
            {
                activated = true;
                StartCoroutine(FallRoutine());
                break;
            }
        }
    }

    private IEnumerator FallRoutine()
    {
        // 흔들림
        yield return StartCoroutine(Shake());

        // 떨어지기 시작
        rb.bodyType = RigidbodyType2D.Dynamic;

        // 일정 시간 후 재생성
        yield return new WaitForSeconds(respawnTime);

        // 플랫폼 숨김
        col.enabled = false;
        sr.enabled = false;

        // 위치 초기화
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.position = startPosition;

        // 다시 고정
        rb.bodyType = RigidbodyType2D.Kinematic;

        // 잠시 후 재생성
        yield return new WaitForSeconds(1f);

        col.enabled = true;
        sr.enabled = true;

        activated = false;
    }

    private IEnumerator Shake()
    {
        Vector3 originalPos = startPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float offsetX = Random.Range(-shakeAmount, shakeAmount);

            transform.position = originalPos + new Vector3(offsetX, 0f, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
    }
}
