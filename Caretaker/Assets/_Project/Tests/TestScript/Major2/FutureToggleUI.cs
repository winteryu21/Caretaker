using Caretaker.Gameplay;
using Caretaker.World;
using UnityEngine;

public class FutureToggleUI : MonoBehaviour, IOperateAction
{
    [SerializeField] private GameObject targetUI;

    public bool Execute(PlayerController actor)
    {
        Debug.Log("실행됨");
        if (targetUI == null)
            return false;

        targetUI.SetActive(!targetUI.activeSelf);

        return true;
    }
}
