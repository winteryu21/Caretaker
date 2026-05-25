using System;

namespace Caretaker.Presentation
{
    /// <summary>
    /// 스플릿뷰 활성 여부, 분할 방향, 각 카메라 대상을 저장하는 런타임 상태.
    /// </summary>
    /// <remarks>DSD §4.3 — 런타임 상태 스키마 (SplitViewState)</remarks>
    [Serializable]
    public class SplitViewState
    {
        public bool IsActive;
        public string LocalTargetId;
        public string RemoteTargetId;
    }
}
