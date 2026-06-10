using UnityEngine;

public class TogleSelf : MonoBehaviour
{
    [SerializeField] private GameObject targetChild;

    public void ToggleChild()
    {
        targetChild.SetActive(!targetChild.activeSelf);
    }
}
