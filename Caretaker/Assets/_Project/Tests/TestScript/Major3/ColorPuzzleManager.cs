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

        // 정확히 2개만 켜져 있어야 함
        if (count != 2)
        {
            colorDisplay.color = Color.white;
            return;
        }

        // 조합별 색상

        if (buttonA.IsOn && buttonB.IsOn)
            colorDisplay.color = Color.red;

        else if (buttonA.IsOn && buttonC.IsOn)
            colorDisplay.color = Color.blue;

        else if (buttonA.IsOn && buttonD.IsOn)
            colorDisplay.color = Color.green;

        else if (buttonA.IsOn && buttonE.IsOn)
            colorDisplay.color = Color.yellow;

        else if (buttonB.IsOn && buttonC.IsOn)
            colorDisplay.color = Color.cyan;

        else if (buttonB.IsOn && buttonD.IsOn)
            colorDisplay.color = Color.magenta;

        else if (buttonB.IsOn && buttonE.IsOn)
            colorDisplay.color = new Color(1f, 0.5f, 0f);

        else if (buttonC.IsOn && buttonD.IsOn)
            colorDisplay.color = Color.gray;

        else if (buttonC.IsOn && buttonE.IsOn)
            colorDisplay.color = Color.black;

        else if (buttonD.IsOn && buttonE.IsOn)
            colorDisplay.color = new Color(0.5f, 0f, 1f);
    }
}
 
