using Caretaker.Gameplay;
using UnityEngine;
using System.Collections;
using System.IO;
using Caretaker.World;

public class BridgeButtonAction : MonoBehaviour, IOperateAction
{
    [SerializeField] private GameObject bridge;

    [SerializeField] private float activeTime = 15f;

    private Coroutine currentRoutine;

    public bool Execute(PlayerController actor)
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(BridgeRoutine());

        return true;
    }

    private IEnumerator BridgeRoutine()
    {
        bridge.SetActive(true);

        yield return new WaitForSeconds(activeTime);

        bridge.SetActive(false);

        currentRoutine = null;
    }
}
