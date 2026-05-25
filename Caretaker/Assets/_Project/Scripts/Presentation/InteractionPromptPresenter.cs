using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// 근접/E키/클릭 상호작용 프롬프트를 표시한다.
    /// 범위 밖이면 비활성화한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.10 — UI / HUD 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class InteractionPromptPresenter : MonoBehaviour
    {
        // 1. Serialize 필드

        // 2. private 필드

        // 3. Unity 생명주기

        /// <summary>
        /// 상호작용 프롬프트를 표시한다.
        /// </summary>
        /// <param name="promptText">표시할 프롬프트 텍스트. (예: "E — 조사")</param>
        public void ShowPrompt(string promptText)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 프롬프트를 숨긴다.
        /// </summary>
        public void HidePrompt()
        {
            throw new System.NotImplementedException();
        }

        // private 메서드
    }
}
