using System;

using UnityEngine;

using Caretaker.Gameplay;

namespace Caretaker.Presentation
{
    public enum SwitchGroup
    {
        A,
        B,
        C,
        D
    }

    public sealed class SwitchPuzzleManager : MonoBehaviour
    {
        [Serializable]
        private struct SwitchActivationStep
        {
            [SerializeField] private SwitchGroup _group;
            [SerializeField] [Range(0, 1)] private int _index;

            public SwitchActivationStep(SwitchGroup group, int index)
            {
                _group = group;
                _index = Mathf.Clamp(index, 0, 1);
            }

            public SwitchGroup Group => _group;

            public int Index => _index;
        }

        [Header("State")]
        [SerializeField] private Major1PowerRestorationController _controller;

        [Header("Answer")]
        [SerializeField] private bool _resetSwitchesOnWrongOrder;
        [SerializeField]
        private SwitchActivationStep[] _requiredActivationOrder =
        {
            new(SwitchGroup.A, 0),
            new(SwitchGroup.B, 0),
            new(SwitchGroup.C, 0),
            new(SwitchGroup.C, 1)
        };

        private readonly bool[,] _switchStates = new bool[4, 2];

        private PlayerController _lastActor;
        private int _activationOrderIndex;
        private bool _solved;

        /// <summary>Toggles a switch and checks both final state and activation order.</summary>
        /// <param name="group">Switch group to toggle.</param>
        /// <param name="index">Zero-based switch index within the group.</param>
        /// <param name="actor">Player who toggled the switch.</param>
        public void ToggleSwitch(SwitchGroup group, int index, PlayerController actor = null)
        {
            if (_solved || !IsValidIndex(index))
            {
                return;
            }

            _lastActor = actor != null ? actor : _lastActor;
            bool nextState = !_switchStates[(int)group, index];
            _switchStates[(int)group, index] = nextState;

            if (nextState && !TryAdvanceActivationOrder(group, index))
            {
                Debug.LogWarning($"Major1: 보조 전압기 스위치 순서가 틀렸습니다. group={group}, index={index + 1}", this);
                _activationOrderIndex = 0;

                if (_resetSwitchesOnWrongOrder)
                {
                    ResetSwitches();
                }
            }

            CheckAnswer();
        }

        /// <summary>Returns whether a switch is currently on.</summary>
        /// <param name="group">Switch group to query.</param>
        /// <param name="index">Zero-based switch index within the group.</param>
        public bool GetSwitchState(SwitchGroup group, int index)
        {
            return IsValidIndex(index) && _switchStates[(int)group, index];
        }

        private void CheckAnswer()
        {
            Debug.Log(
                $"A1:{_switchStates[(int)SwitchGroup.A, 0]}, " +
                $"A2:{_switchStates[(int)SwitchGroup.A, 1]}, " +
                $"B1:{_switchStates[(int)SwitchGroup.B, 0]}, " +
                $"B2:{_switchStates[(int)SwitchGroup.B, 1]}, " +
                $"C1:{_switchStates[(int)SwitchGroup.C, 0]}, " +
                $"C2:{_switchStates[(int)SwitchGroup.C, 1]}, " +
                $"D1:{_switchStates[(int)SwitchGroup.D, 0]}, " +
                $"D2:{_switchStates[(int)SwitchGroup.D, 1]}");

            bool correct =
                _switchStates[(int)SwitchGroup.A, 0] &&
                !_switchStates[(int)SwitchGroup.A, 1] &&
                _switchStates[(int)SwitchGroup.B, 0] &&
                !_switchStates[(int)SwitchGroup.B, 1] &&
                _switchStates[(int)SwitchGroup.C, 0] &&
                _switchStates[(int)SwitchGroup.C, 1] &&
                !_switchStates[(int)SwitchGroup.D, 0] &&
                !_switchStates[(int)SwitchGroup.D, 1] &&
                HasCompletedActivationOrder();

            if (!correct)
            {
                return;
            }

            _solved = true;
            Debug.Log("Major1: 보조 전압기 스위치 퍼즐 성공.", this);

            if (_controller == null)
            {
                _controller = FindFirstObjectByType<Major1PowerRestorationController>();
            }

            if (_controller != null)
            {
                _controller.MarkFutureSwitchSolved(_lastActor);
            }
        }

        private bool TryAdvanceActivationOrder(SwitchGroup group, int index)
        {
            if (_requiredActivationOrder == null || _requiredActivationOrder.Length == 0)
            {
                return true;
            }

            if (_activationOrderIndex >= _requiredActivationOrder.Length)
            {
                return false;
            }

            SwitchActivationStep expectedStep = _requiredActivationOrder[_activationOrderIndex];
            if (expectedStep.Group != group || expectedStep.Index != index)
            {
                return false;
            }

            _activationOrderIndex++;
            return true;
        }

        private bool HasCompletedActivationOrder()
        {
            return _requiredActivationOrder == null ||
                _requiredActivationOrder.Length == 0 ||
                _activationOrderIndex >= _requiredActivationOrder.Length;
        }

        private void ResetSwitches()
        {
            for (int groupIndex = 0; groupIndex < 4; groupIndex++)
            {
                for (int switchIndex = 0; switchIndex < 2; switchIndex++)
                {
                    _switchStates[groupIndex, switchIndex] = false;
                }
            }
        }

        private static bool IsValidIndex(int index)
        {
            return index >= 0 && index <= 1;
        }
    }
}
