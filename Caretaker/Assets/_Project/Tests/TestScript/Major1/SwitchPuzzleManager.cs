using UnityEngine;

namespace Caretaker.Presentation
{
    public enum SwitchGroup
    {
        A,
        B,
        C,
        D
    }

    public class SwitchPuzzleManager : MonoBehaviour
    {
        private bool[,] switchStates = new bool[4, 2];

        private bool solved;

        public void ToggleSwitch(SwitchGroup group, int index)
        {
            if (solved)
                return;

            switchStates[(int)group, index] = !switchStates[(int)group, index];

            CheckAnswer();
        }

        public bool GetSwitchState(SwitchGroup group, int index)
        {
            return switchStates[(int)group, index];
        }

        private void CheckAnswer()
        {
            Debug.Log(
                $"A1:{switchStates[(int)SwitchGroup.A, 0]}, " +
                $"A2:{switchStates[(int)SwitchGroup.A, 1]}, " +
                $"B1:{switchStates[(int)SwitchGroup.B, 0]}, " +
                $"B2:{switchStates[(int)SwitchGroup.B, 1]}, " +
                $"C1:{switchStates[(int)SwitchGroup.C, 0]}, " +
                $"C2:{switchStates[(int)SwitchGroup.C, 1]}, " +
                $"D1:{switchStates[(int)SwitchGroup.D, 0]}, " +
                $"D2:{switchStates[(int)SwitchGroup.D, 1]}"
            );

            bool correct =
                switchStates[(int)SwitchGroup.A, 0] &&
                !switchStates[(int)SwitchGroup.A, 1] &&
                switchStates[(int)SwitchGroup.B, 0] &&
                !switchStates[(int)SwitchGroup.B, 1] &&
                switchStates[(int)SwitchGroup.C, 0] &&
                switchStates[(int)SwitchGroup.C, 1] &&
                !switchStates[(int)SwitchGroup.D, 0] &&
                !switchStates[(int)SwitchGroup.D, 1];

            if (!correct)
                return;

            solved = true;

            Debug.Log("퍼즐 성공! 전력이 복구 됩니다.");
        }
    }
}