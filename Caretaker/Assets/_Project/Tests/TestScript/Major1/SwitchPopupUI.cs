using UnityEngine;
using UnityEngine.UI;

namespace Caretaker.Presentation
{
    public class SwitchPopupUI : PuzzleUIBase
    {
        [SerializeField] private SwitchPuzzleManager puzzleManager;

        [Header("Switch Images")]
        [SerializeField] private Image switchImage1;
        [SerializeField] private Image switchImage2;

        [SerializeField] private Sprite offSprite;
        [SerializeField] private Sprite onSprite;

        private SwitchGroup currentGroup;

        private void Awake()
        {
            AutoCacheReferences();
        }

        public void Open(SwitchGroup group)
        {
            currentGroup = group;

            AutoCacheReferences();

            Open();
            Refresh();
        }

        public void OnClickSwitch1()
        {
            puzzleManager.ToggleSwitch(currentGroup, 0);
            Refresh();
        }

        public void OnClickSwitch2()
        {
            puzzleManager.ToggleSwitch(currentGroup, 1);
            Refresh();
        }

        protected override bool IsCorrectSolution()
        {
            return false;
        }

        private void Refresh()
        {
            if (puzzleManager == null)
            {
                Debug.LogError("SwitchPopupUI: Puzzle Manager가 연결되지 않았습니다.", this);
                return;
            }

            if (switchImage1 == null)
            {
                Debug.LogError("SwitchPopupUI: Switch Image 1이 연결되지 않았습니다.", this);
                return;
            }

            if (switchImage2 == null)
            {
                Debug.LogError("SwitchPopupUI: Switch Image 2가 연결되지 않았습니다.", this);
                return;
            }

            if (offSprite == null)
            {
                Debug.LogError("SwitchPopupUI: Off Sprite가 연결되지 않았습니다.", this);
                return;
            }

            if (onSprite == null)
            {
                Debug.LogError("SwitchPopupUI: On Sprite가 연결되지 않았습니다.", this);
                return;
            }

            switchImage1.sprite = puzzleManager.GetSwitchState(currentGroup, 0)
                ? onSprite
                : offSprite;

            switchImage2.sprite = puzzleManager.GetSwitchState(currentGroup, 1)
                ? onSprite
                : offSprite;
        }

        private void AutoCacheReferences()
        {
            if (puzzleManager == null)
            {
                puzzleManager = FindFirstObjectByType<SwitchPuzzleManager>();
            }
        }
    }
}