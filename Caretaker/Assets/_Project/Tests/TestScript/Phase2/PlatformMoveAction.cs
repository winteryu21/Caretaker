using Caretaker.Gameplay;
using Caretaker.World;
using UnityEngine;

public class PlatformMoveAction : MonoBehaviour, IOperateAction
{
    [SerializeField]
    private Phase2Platform[] platforms;

    public bool Execute(PlayerController actor)
    {
        foreach (Phase2Platform platform in platforms)
        {
            if (platform != null)
            {
                platform.StartMoving();
            }
        }

        return true;
    }
}

