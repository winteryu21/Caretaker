using UnityEngine;

public class ColorPuzzleManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private ColorButton buttonA;
    [SerializeField] private ColorButton buttonB;
    [SerializeField] private ColorButton buttonC;
    [SerializeField] private ColorButton buttonD;
    [SerializeField] private ColorButton buttonE;

    [Header("Color Display")]
    [SerializeField] private SpriteRenderer colorDisplay;

    private void Awake()
    {
        buttonA.OnStateChanged += CheckCombination;
        buttonB.OnStateChanged += CheckCombination;
        buttonC.OnStateChanged += CheckCombination;
        buttonD.OnStateChanged += CheckCombination;
        buttonE.OnStateChanged += CheckCombination;
    }

    private void OnDestroy()
    {
        buttonA.OnStateChanged -= CheckCombination;
        buttonB.OnStateChanged -= CheckCombination;
        buttonC.OnStateChanged -= CheckCombination;
        buttonD.OnStateChanged -= CheckCombination;
        buttonE.OnStateChanged -= CheckCombination;
    }

    private void Start()
    {
        colorDisplay.color = Color.white;
    }

    private void CheckCombination()
    {
        int count = 0;

        if (buttonA.IsOn) count++;
        if (buttonB.IsOn) count++;
        if (buttonC.IsOn) count++;
        if (buttonD.IsOn) count++;
        if (buttonE.IsOn) count++;

        if (count != 2)
        {
            colorDisplay.color = Color.white;
            return;
        }

        if (buttonA.IsOn && buttonB.IsOn)
            colorDisplay.color = Color.yellow;

        else if (buttonA.IsOn && buttonC.IsOn)
            colorDisplay.color = Color.blue;

        else if (buttonA.IsOn && buttonD.IsOn)
            colorDisplay.color = new Color(0.5f, 0f, 1f);

        else if (buttonA.IsOn && buttonE.IsOn)
            colorDisplay.color = Color.green;

        else if (buttonB.IsOn && buttonC.IsOn)
            colorDisplay.color = Color.yellow;

        else if (buttonB.IsOn && buttonD.IsOn)
            colorDisplay.color = Color.red;

        else if (buttonB.IsOn && buttonE.IsOn)
            colorDisplay.color = new Color(0.5f, 0f, 1f);

        else if (buttonC.IsOn && buttonD.IsOn)
            colorDisplay.color = Color.green;

        else if (buttonC.IsOn && buttonE.IsOn)
            colorDisplay.color = Color.red;

        else if (buttonD.IsOn && buttonE.IsOn)
            colorDisplay.color = Color.blue;
    }
}
