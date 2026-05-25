using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// Phase 3에서 상하 5:5 스플릿뷰를 관리한다.
    /// 메인 카메라 rect, viewport, UI anchor를 변경하고
    /// 보조 카메라를 활성화한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.8 — Phase 3 스플릿뷰 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class SplitViewManager : MonoBehaviour
    {
        // 1. Serialize 필드

        // 2. private 필드

        // 3. Unity 생명주기

        /// <summary>
        /// 스플릿뷰를 활성화한다.
        /// 메인 뷰 UI 영역을 조정하고 보조 뷰 RenderTexture를 활성화한다.
        /// </summary>
        public void EnableSplitView()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 스플릿뷰를 비활성화하고 단일 화면으로 복귀한다.
        /// </summary>
        public void DisableSplitView()
        {
            throw new System.NotImplementedException();
        }

        // private 메서드
    }
}
