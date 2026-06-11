using System.Collections;
using UnityEngine;

namespace Caretaker.World
{
    public class MoveTile : MonoBehaviour
    {
        private enum MoveDirection
        {
            Left,
            Right,
            Up,
            Down
        }

        [Header("Move Settings")]
        [SerializeField] private MoveDirection moveDirection = MoveDirection.Right;
        [SerializeField] private float moveDistance = 2f;
        [SerializeField] private float moveDuration = 1f;
        [SerializeField] private float waitTime = 0f;
        [SerializeField] private float startDelay = 0f;

        private Vector3 originPosition;
        private Vector3 targetPosition;
        private Coroutine moveRoutine;

        private void Awake()
        {
            originPosition = transform.position;
            targetPosition = originPosition + GetDirectionVector() * moveDistance;
        }

        private void OnEnable()
        {
            moveRoutine = StartCoroutine(MoveLoop());
        }

        private void OnDisable()
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
                moveRoutine = null;
            }
        }

        private Vector3 GetDirectionVector()
        {
            switch (moveDirection)
            {
                case MoveDirection.Left:
                    return Vector3.left;

                case MoveDirection.Right:
                    return Vector3.right;

                case MoveDirection.Up:
                    return Vector3.up;

                case MoveDirection.Down:
                    return Vector3.down;

                default:
                    return Vector3.right;
            }
        }

        private IEnumerator MoveLoop()
        {
            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            while (true)
            {
                yield return MoveTo(targetPosition);

                if (waitTime > 0f)
                    yield return new WaitForSeconds(waitTime);

                yield return MoveTo(originPosition);

                if (waitTime > 0f)
                    yield return new WaitForSeconds(waitTime);
            }
        }

        private IEnumerator MoveTo(Vector3 destination)
        {
            Vector3 startPosition = transform.position;
            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / moveDuration);
                transform.position = Vector3.Lerp(startPosition, destination, t);

                yield return null;
            }

            transform.position = destination;
        }
    }
}