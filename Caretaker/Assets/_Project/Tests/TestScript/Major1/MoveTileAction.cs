using System.Collections;
using Caretaker.Gameplay;
using UnityEngine;

namespace Caretaker.World
{
    public class MoveTileAction : MonoBehaviour, IOperateAction
    {
        private enum MoveAxis
        {
            VerticalDown,   // 시작 위치 → 아래 → 시작 위치 반복
            HorizontalRight // 시작 위치 → 오른쪽 → 시작 위치 반복
        }

        [Header("Target Tile")]
        [SerializeField] private Transform targetTile;

        [Header("Move Settings")]
        [SerializeField] private MoveAxis moveAxis = MoveAxis.VerticalDown;
        [SerializeField] private float moveDistance = 2f;
        [SerializeField] private float moveDuration = 1f;

        [Header("Options")]
        [SerializeField] private bool stopWhenOperateAgain = false;

        private Vector3 originPosition;
        private Vector3 movedPosition;

        private Coroutine moveRoutine;
        private bool isMoving;

        private void Awake()
        {
            if (targetTile == null)
            {
                Debug.LogError("[MoveTileAction] Target Tile이 연결되지 않았습니다.", this);
                return;
            }

            originPosition = targetTile.position;
            movedPosition = GetMovedPosition();
        }

        public bool Execute(PlayerController actor)
        {
            if (targetTile == null)
            {
                Debug.LogError("[MoveTileAction] Target Tile이 없어 타일을 움직일 수 없습니다.", this);
                return false;
            }

            if (isMoving)
            {
                if (stopWhenOperateAgain)
                {
                    StopMoving();
                    Debug.Log("[MoveTileAction] 타일 왕복 이동 정지");
                }

                return true;
            }

            moveRoutine = StartCoroutine(MoveLoop());
            isMoving = true;

            Debug.Log($"[MoveTileAction] 타일 반복 이동 시작: {targetTile.name}");
            return true;
        }

        private Vector3 GetMovedPosition()
        {
            switch (moveAxis)
            {
                case MoveAxis.VerticalDown:
                    return originPosition + Vector3.down * moveDistance;

                case MoveAxis.HorizontalRight:
                    return originPosition + Vector3.right * moveDistance;

                default:
                    return originPosition;
            }
        }

        private IEnumerator MoveLoop()
        {
            while (true)
            {
                yield return MoveTo(movedPosition);
                yield return MoveTo(originPosition);
            }
        }

        private IEnumerator MoveTo(Vector3 targetPosition)
        {
            Vector3 startPosition = targetTile.position;
            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / moveDuration);
                targetTile.position = Vector3.Lerp(startPosition, targetPosition, t);

                yield return null;
            }

            targetTile.position = targetPosition;
        }

        private void StopMoving()
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
                moveRoutine = null;
            }

            isMoving = false;
        }
    }
}