using Caretaker.Gameplay;
using Caretaker.World;
using System.Collections;
using UnityEngine;

public class MultiInteractionDoor : MonoBehaviour,IOperateAction
{
    [Header("Door")]
    [SerializeField] private Transform escapeDoor;

    [Header("Interaction")]
    [SerializeField] private int requiredCount = 10;

    [Header("Door Movement")]
    [SerializeField] private float moveDistance = 3f;
    [SerializeField] private float moveDuration = 1f;

    private int currentCount;
    private bool isOpened;
    private bool isOpening;

    public bool Execute(PlayerController actor)
    {
        if (isOpened || isOpening)
        {
            return false;
        }

        currentCount++;

        Debug.Log($"문 상호작용 횟수 : {currentCount}/{requiredCount}");

        if (currentCount >= requiredCount)
        {
            StartCoroutine(OpenDoor());
        }

        return true;
    }

    private IEnumerator OpenDoor()
    {
        isOpening = true;

        Vector3 startPos = escapeDoor.position;
        Vector3 targetPos = startPos + Vector3.down * moveDistance;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            escapeDoor.position = Vector3.Lerp(
                startPos,
                targetPos,
                elapsed / moveDuration);

            yield return null;
        }

        escapeDoor.position = targetPos;

        isOpened = true;
        isOpening = false;

        Debug.Log("문이 열렸습니다.");
    }
}
