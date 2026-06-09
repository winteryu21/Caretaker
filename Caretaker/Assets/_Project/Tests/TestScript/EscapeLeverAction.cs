using Caretaker.Gameplay;
using Caretaker.World;
using UnityEngine;

public class EscapeLeverAction : MonoBehaviour, IOperateAction
{
    [Header("Lever")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private Sprite offSprite;
    [SerializeField] private Sprite onSprite;

    [SerializeField] private bool isOn;

    public bool Execute(PlayerController actor)
    {
        isOn = !isOn;

        RefreshVisual();

        return true;
    }


    private void RefreshVisual()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite = isOn? onSprite: offSprite;
    }

    public bool GetState()
    {
        return isOn;
    }
}
