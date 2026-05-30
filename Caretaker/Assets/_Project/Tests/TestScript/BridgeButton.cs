using System.Collections;
using UnityEngine;

public class BridgeButton : MonoBehaviour
{
    [SerializeField]
    private GameObject targetObject; // 생성할 길
    [SerializeField]
    private float activeTime = 5f;

    private bool isActivated = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated) return;

        if (other.CompareTag("Player"))
        {
            StartCoroutine(ActivateObject());
        }
    }

    private IEnumerator ActivateObject()
    {
        isActivated = true;

        targetObject.SetActive(true);

        yield return new WaitForSeconds(activeTime);

        targetObject.SetActive(false);

        isActivated = false;
    }
}
