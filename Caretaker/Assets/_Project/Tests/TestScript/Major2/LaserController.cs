using System.Collections.Generic;
using UnityEngine;

public class LaserController : MonoBehaviour
{

    [SerializeField] private Major2Puzzle puzzleUI;

    //레이저 
    [SerializeField] private RectTransform laserContainer;
    [SerializeField] private RectTransform startPoint;
    [SerializeField] private RectTransform goalPoint;
    [SerializeField] private GameObject segmentPrefab;


    //벽문 생성 해제
    [SerializeField] private GameObject doorA1;
    [SerializeField] private GameObject doorA2;

    [SerializeField] private GameObject doorB1;
    [SerializeField] private GameObject doorB2;

    [SerializeField] private float growSpeed = 200f;


    [SerializeField] private Transform wallRoot;

    private RectTransform[] walls;
    private readonly List<GameObject> segments = new();



    private RectTransform currentSegment;

    private Vector2 currentPosition;

    private Vector2 direction = Vector2.right;

    private void Start()
    {
        walls = wallRoot.GetComponentsInChildren<RectTransform>();
        RestartLaser();
    }

    private void Update()
    {

        GrowCurrentSegment();

        if (HitWall())
        {
            Debug.Log("벽 충돌!");

            RestartLaser();

            return;
        }

        if (ReachedGoal())
        {
            OnGoalReached();
        }
    }

    private void GrowCurrentSegment()
    {
        if (currentSegment == null)
            return;

        Vector2 size = currentSegment.sizeDelta;

        size.x += growSpeed * Time.deltaTime;

        currentSegment.sizeDelta = size;
    }

    private void CreateSegment()
    {
        GameObject obj =
            Instantiate(segmentPrefab, laserContainer);

        currentSegment =
            obj.GetComponent<RectTransform>();

        currentSegment.anchoredPosition =
            currentPosition;

        currentSegment.sizeDelta =
            new Vector2(0, 8);

        float angle = Mathf.Atan2(
            direction.y,
            direction.x)
            * Mathf.Rad2Deg;

        currentSegment.localRotation =
            Quaternion.Euler(0, 0, angle);

        segments.Add(obj);
    }

    private void ChangeDirection(Vector2 newDirection)
    {
        currentPosition = GetCurrentLaserTip();

        direction = newDirection;

        CreateSegment();
    }

    private Vector2 GetCurrentLaserTip()
    {
        if (currentSegment == null)
            return currentPosition;

        float length = currentSegment.sizeDelta.x;

        return currentPosition + direction * length;
    }

    public void MoveUp()
    {
        ChangeDirection(Vector2.up);
    }

    public void MoveDown()
    {
        ChangeDirection(Vector2.down);
    }

    public void MoveLeft()
    {
        ChangeDirection(Vector2.left);
    }

    public void MoveRight()
    {
        ChangeDirection(Vector2.right);
    }



    public void RestartLaser()
    {
        foreach (GameObject obj in segments)
        {
            Destroy(obj);
        }

        segments.Clear();

        currentPosition =
            startPoint.anchoredPosition;

        direction = Vector2.right;

        CreateSegment();
    }


    private bool ReachedGoal()
    {
        Vector2 laserTip = GetCurrentLaserTip();

        float distance = Vector2.Distance(laserTip, goalPoint.anchoredPosition);

        return distance < 15f;
    }

    private void OnGoalReached()
    {
        Debug.Log("도착!");

        puzzleUI.NotifyGoalReached();

        enabled = false;
    }


    private bool HitWall()
    {
        Vector2 tip = GetCurrentLaserTip();

        foreach (RectTransform wall in walls)
        {
            Vector2 center = wall.anchoredPosition;
            Vector2 size = wall.rect.size;

            if (tip.x >= center.x - size.x * 0.5f &&
                tip.x <= center.x + size.x * 0.5f &&
                tip.y >= center.y - size.y * 0.5f &&
                tip.y <= center.y + size.y * 0.5f)
            {
                return true;
            }
        }

        return false;
    }



}
