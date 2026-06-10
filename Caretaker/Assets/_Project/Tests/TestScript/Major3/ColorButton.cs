using Caretaker.Gameplay;
using Caretaker.World;
using UnityEngine;
using System;

public class ColorButton : MonoBehaviour, IOperateAction
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Color originalColor;
    private bool isOn;

    public bool IsOn => isOn;

    public event Action OnStateChanged;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        originalColor = spriteRenderer.color;
    }

    public bool Execute(PlayerController actor)
    {
        isOn = !isOn;

        spriteRenderer.color =
            isOn ? Color.white : originalColor;

        OnStateChanged?.Invoke();

        return true;
    }
}
