using UnityEngine;
using System.Collections;


public class Phase2Platform : MonoBehaviour
{
    [SerializeField] private float moveDistance = 2f;
    [SerializeField] private float moveDuration = 1f;

    private Vector3 startPos;
    private Vector3 endPos;

    private bool isMoving;

    private void Awake()
    {
        startPos = transform.position;
        endPos = startPos + Vector3.up * moveDistance;
    }

    public void StartMoving()
    {
        if (!isMoving)
        {
            isMoving = true;
            StartCoroutine(MoveLoop());
        }
    }

    public void StopMoving()
    {
        isMoving = false;
    }

    private IEnumerator MoveLoop()
    {
        while (isMoving)
        {
            yield return Move(startPos, endPos);
            yield return Move(endPos, startPos);
        }
    }

    private IEnumerator Move(Vector3 from, Vector3 to)
    {
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            transform.position =
                Vector3.Lerp(from, to, elapsed / moveDuration);

            yield return null;
        }

        transform.position = to;
    }
}
