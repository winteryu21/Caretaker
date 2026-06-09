using Caretaker.Presentation;
using UnityEngine;

public class Major2Puzzle : PuzzleUIBase
{
    [SerializeField] private LaserController laserController;

    private bool goalReached;

    /// <summary>
    /// 레이저가 목표에 도착했을 때 호출
    /// </summary>
    public void NotifyGoalReached()
    {
        if (IsSolved)
            return;

        goalReached = true;

        TryCompletePuzzle();
    }

    /// <summary>
    /// PuzzleUIBase가 요구하는 정답 판정
    /// </summary>
    protected override bool IsCorrectSolution()
    {
        return goalReached;
    }

    /// <summary>
    /// UI가 열릴 때
    /// </summary>
    protected override void HandleOpened()
    {
        goalReached = false;

        if (laserController != null)
        {
            laserController.RestartLaser();
            laserController.enabled = true;
        }
    }

    /// <summary>
    /// UI가 닫힐 때
    /// </summary>
    protected override void HandleClosed()
    {
        if (laserController != null)
        {
            laserController.enabled = false;
        }
    }

    /// <summary>
    /// 퍼즐 성공 시
    /// </summary>
    protected override void HandleSolved()
    {
        Debug.Log("레이저 퍼즐 성공");

        if (laserController != null)
        {
            laserController.enabled = false;
        }

        Close();
    }

    /// <summary>
    /// Submit() 사용 시 오답 처리
    /// </summary>
    protected override void HandleIncorrectSolution()
    {
        Debug.Log("레이저 퍼즐 실패");
    }
}
    
