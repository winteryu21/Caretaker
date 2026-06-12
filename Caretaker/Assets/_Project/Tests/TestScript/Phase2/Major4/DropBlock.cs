using UnityEngine;

public class DropBlock : MonoBehaviour
{
    [SerializeField] private float fallSpeed = 10f;
    [SerializeField] private float fallDistance = 8.5f;

    private bool isFalling = false;
    private Vector3 targetPos;

    public void StartFalling()
    {
        targetPos = transform.position + Vector3.down * fallDistance;
        isFalling = true;
    }

    void Update()
    {
        if (!isFalling) return;

        transform.position = Vector3.MoveTowards(transform.position,targetPos,fallSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            transform.position = targetPos;
            isFalling = false;
        }
    }
}
