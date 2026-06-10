using UnityEngine;
using UnityEngine.U2D.IK;
using System.Collections;
public class LeverPuzzleManager : MonoBehaviour
{
    [Header("Levers")]
    [SerializeField] private EscapeLeverAction leverA;
    [SerializeField] private EscapeLeverAction leverB;
    [SerializeField] private EscapeLeverAction leverC;
    [SerializeField] private EscapeLeverAction leverD;

    [Header("Door")]
    [SerializeField] private Transform escapeDoor;

    [SerializeField] private float moveDistance = 3f;
    [SerializeField] private float moveDuration = 1f;

    private bool solved;

    private void Update()
    {
        if (solved)
            return;

        CheckAnswer();
    }

    private void CheckAnswer()
    {
        bool correct =leverA.GetState() &&leverB.GetState() &&!leverC.GetState() &&leverD.GetState();

        if (!correct)
            return;

        solved = true;

        Debug.Log("퍼즐 성공!");

        StartCoroutine(OpenDoor());
    }

    private IEnumerator OpenDoor()
    {
        Vector3 startPos = escapeDoor.position;
        Vector3 targetPos = startPos + Vector3.down * moveDistance;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            escapeDoor.position = Vector3.Lerp(startPos,targetPos,elapsed / moveDuration);

            yield return null;
        }

        escapeDoor.position = targetPos;
    }
}
