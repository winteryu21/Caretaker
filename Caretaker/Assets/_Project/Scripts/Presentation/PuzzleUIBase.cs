using System;

using UnityEngine;

using Caretaker.World;

namespace Caretaker.Presentation
{
    // DEV-48 퍼즐 UI 구현 계약:
    // - M1/M2/M4 퍼즐마다 이 클래스를 상속한 구체 UI 클래스를 하나씩 만든다.
    //   예: 배선 조합 UI, 코드 입력 UI, 바이러스 입력 UI.
    // - 파생 컴포넌트는 UI 컨트롤러 오브젝트 또는 패널 루트에 붙인다.
    // - 확인 버튼이 있는 퍼즐은 버튼을 Submit()에 연결한다.
    // - 자동 검증 퍼즐은 토글/입력값 변경 콜백에서 TryCompletePuzzle()을 호출한다.
    // - IsCorrectSolution()에는 해당 퍼즐의 현재 입력값을 정답과 비교하는 로직만 둔다.
    // - CausalRuleSO 조건이 필요한 퍼즐은 _solvedConditionKey/_solvedConditionValue를 연결한다.
    //   조건 키/값은 각 퍼즐 구현자와 최신 퍼즐 명세가 결정한다.
    // - _solvedTrigger에는 퍼즐이 풀렸을 때 발화할 CausalTrigger를 연결한다.
    // - 파생 퍼즐 클래스에서 CausalTrigger.Fire()를 직접 호출하지 않는다. 이 베이스 클래스가 한 번만 호출한다.
    // - 확인 버튼 오답 UI 피드백이 필요하면 HandleIncorrectSolution()을 재정의한다.
    /// <summary>
    /// 로컬 퍼즐 입력을 검증한 뒤 인과 트리거를 발화하는 퍼즐 UI 베이스 클래스.
    /// </summary>
    /// <remarks>
    /// DEV-48 — 퍼즐 UI 프레임워크.
    /// M1/M2/M4의 구체 퍼즐 UI 클래스는 정답 판정과 화면 표현 세부사항만 구현한다.
    /// </remarks>
    public abstract class PuzzleUIBase : MonoBehaviour
    {
        [Header("화면")]
        [SerializeField] private GameObject _panelRoot;

        [Header("풀이 성공 결과")]
        [SerializeField] private CausalityManager _causalityManager;
        [SerializeField] private CausalTrigger _solvedTrigger;
        [SerializeField] private string _solvedConditionKey;
        [SerializeField] private string _solvedConditionValue;

        /// <summary>
        /// 퍼즐이 정답을 받아들였을 때 한 번 발생한다.
        /// </summary>
        public event Action<PuzzleUIBase> OnPuzzleSolved;

        /// <summary>
        /// 현재 퍼즐 UI가 열려 있는지 반환한다.
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// 퍼즐이 이미 풀렸는지 반환한다.
        /// </summary>
        public bool IsSolved { get; private set; }

        /// <summary>
        /// 완료 트리거가 비어 있을 때 경고를 출력할지 반환한다.
        /// </summary>
        protected virtual bool WarnWhenSolvedTriggerMissing => true;

        private void Awake()
        {
            if (_panelRoot == null)
            {
                _panelRoot = gameObject;
            }

            if (_solvedTrigger == null)
            {
                _solvedTrigger = GetComponent<CausalTrigger>();
            }

            IsOpen = _panelRoot.activeSelf;
        }

        private void OnValidate()
        {
            _solvedConditionKey = _solvedConditionKey?.Trim();
            _solvedConditionValue = _solvedConditionValue?.Trim();
        }

        /// <summary>
        /// 퍼즐 UI를 연다.
        /// </summary>
        public virtual void Open()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }

            IsOpen = true;
            HandleOpened();
        }

        /// <summary>
        /// 퍼즐 UI를 닫는다.
        /// </summary>
        public virtual void Close()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            IsOpen = false;
            HandleClosed();
        }

        /// <summary>
        /// 현재 퍼즐 입력을 검증하고 정답이면 퍼즐을 완료 처리한다.
        /// 확인 버튼이 있는 퍼즐은 이 메서드에 연결한다.
        /// </summary>
        public void Submit()
        {
            TryCompletePuzzle(true);
        }

        /// <summary>
        /// 현재 입력 상태를 검증하고 정답이면 퍼즐을 완료 처리한다.
        /// 자동 검증 퍼즐은 입력 변경 콜백에서 이 메서드를 호출한다.
        /// </summary>
        /// <returns>이번 호출에서 퍼즐이 새로 완료되었으면 true.</returns>
        protected bool TryCompletePuzzle()
        {
            return TryCompletePuzzle(false);
        }

        private bool TryCompletePuzzle(bool notifyIncorrectSolution)
        {
            if (IsSolved)
            {
                return false;
            }

            if (!IsCorrectSolution())
            {
                if (notifyIncorrectSolution)
                {
                    HandleIncorrectSolution();
                }

                return false;
            }

            CompletePuzzle();
            return true;
        }

        /// <summary>
        /// 현재 퍼즐별 입력값이 정답이면 true를 반환한다.
        /// </summary>
        protected abstract bool IsCorrectSolution();

        /// <summary>
        /// Open()이 표시 상태를 갱신한 뒤 호출된다.
        /// </summary>
        protected virtual void HandleOpened()
        {
        }

        /// <summary>
        /// Close()가 표시 상태를 갱신한 뒤 호출된다.
        /// </summary>
        protected virtual void HandleClosed()
        {
        }

        /// <summary>
        /// Submit()이 오답을 받았을 때 호출된다.
        /// </summary>
        protected virtual void HandleIncorrectSolution()
        {
        }

        /// <summary>
        /// 조건 상태 설정, 인과 트리거 발화, OnPuzzleSolved 처리가 끝난 뒤 호출된다.
        /// </summary>
        protected virtual void HandleSolved()
        {
        }

        private void CompletePuzzle()
        {
            IsSolved = true;

            if (!TrySubmitSolvedPuzzleTrigger())
            {
                ApplySolvedCondition();
                FireSolvedTrigger();
            }

            OnPuzzleSolved?.Invoke(this);
            HandleSolved();
        }

        private bool TrySubmitSolvedPuzzleTrigger()
        {
            if (_solvedTrigger == null || string.IsNullOrWhiteSpace(_solvedConditionKey))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(_solvedTrigger.TriggerId))
            {
                return false;
            }

            if (_causalityManager == null)
            {
                _causalityManager = FindAnyObjectByType<CausalityManager>();
            }

            if (_causalityManager == null)
            {
                return false;
            }

            _causalityManager.SubmitPuzzleSolvedTrigger(
                _solvedTrigger.TriggerId,
                _solvedConditionKey,
                _solvedConditionValue);
            return true;
        }

        private void ApplySolvedCondition()
        {
            if (string.IsNullOrWhiteSpace(_solvedConditionKey))
            {
                return;
            }

            if (_causalityManager == null)
            {
                _causalityManager = FindAnyObjectByType<CausalityManager>();
            }

            if (_causalityManager == null)
            {
                Debug.LogWarning(
                    $"Puzzle '{name}' solved but no CausalityManager was found for condition '{_solvedConditionKey}'.",
                    this);
                return;
            }

            _causalityManager.SetCondition(_solvedConditionKey, _solvedConditionValue);
        }

        private void FireSolvedTrigger()
        {
            if (_solvedTrigger == null)
            {
                if (WarnWhenSolvedTriggerMissing)
                {
                    Debug.LogWarning($"Puzzle '{name}' solved without a solved CausalTrigger.", this);
                }

                return;
            }

            _solvedTrigger.Fire();
        }
    }
}
