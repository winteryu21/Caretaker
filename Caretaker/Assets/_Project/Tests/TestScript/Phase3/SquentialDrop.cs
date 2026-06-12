using UnityEngine;
using System.Collections;
public class SquentialDrop : MonoBehaviour
{
    [SerializeField] private float fallDistance = 3f;
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private float delayBetweenBlocks = 0.2f;


   
    public void StartFalling()
    {
        StartCoroutine(FallBlocks());
    }

    private IEnumerator FallBlocks()
    {
        foreach (Transform block in transform)
        {
            StartCoroutine(FallAndDisappear(block));

            yield return new WaitForSeconds(delayBetweenBlocks);
        }
    }

    private IEnumerator FallAndDisappear(Transform block)
    {
        Vector3 startPos = block.position;
        Vector3 targetPos = startPos + Vector3.down * fallDistance;

        while (Vector3.Distance(block.position, targetPos) > 0.01f)
        {
            block.position = Vector3.MoveTowards(block.position, targetPos,fallSpeed * Time.deltaTime);

            yield return null;
        }

        block.gameObject.SetActive(false);
    }
}
