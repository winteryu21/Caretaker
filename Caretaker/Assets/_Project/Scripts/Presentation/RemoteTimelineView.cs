using Caretaker.Shared;
using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// 상대 시간대를 직접 렌더링하는 읽기 전용 보조 카메라를 관리한다.
    /// </summary>
    public sealed class RemoteTimelineView : MonoBehaviour
    {
        private Camera _camera;
        private TimelineCameraRig _cameraRig;

        /// <summary>비활성 상태의 보조 카메라를 생성한다.</summary>
        public void Initialize()
        {
            if (_camera != null)
            {
                return;
            }

            GameObject cameraObject = new("Remote Timeline Camera");
            cameraObject.transform.SetParent(transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.enabled = false;
            _cameraRig = cameraObject.AddComponent<TimelineCameraRig>();
        }

        /// <summary>상대 시간대를 지정한 viewport에 직접 렌더링한다.</summary>
        public void Show(
            Camera templateCamera,
            Transform pastPlayer,
            Transform futurePlayer,
            Rect viewport)
        {
            if (templateCamera == null)
            {
                Show(templateCamera, pastPlayer, futurePlayer, viewport, Vector3.zero);
                return;
            }

            Show(templateCamera, pastPlayer, futurePlayer, viewport, templateCamera.transform.position);
        }

        /// <summary>상대 시간대를 지정한 viewport와 카메라 기준 위치로 직접 렌더링한다.</summary>
        public void Show(
            Camera templateCamera,
            Transform pastPlayer,
            Transform futurePlayer,
            Rect viewport,
            Vector3 basePosition)
        {
            Show(
                templateCamera,
                pastPlayer,
                futurePlayer,
                viewport,
                basePosition,
                TimelineRole.None,
                0f,
                0f);
        }

        /// <summary>상대 시간대를 지정한 viewport와 시간대별 진행도 기준으로 직접 렌더링한다.</summary>
        public void Show(
            Camera templateCamera,
            Transform pastPlayer,
            Transform futurePlayer,
            Rect viewport,
            Vector3 basePosition,
            TimelineRole cameraTimelineRole,
            float pastProgressOriginX,
            float futureProgressOriginX)
        {
            if (_camera == null || templateCamera == null)
            {
                Debug.LogWarning("RemoteTimelineView requires an initialized camera and template.", this);
                return;
            }

            _camera.CopyFrom(templateCamera);
            _camera.targetTexture = null;
            _camera.rect = viewport;
            _camera.depth = 0f;
            _camera.transform.SetPositionAndRotation(
                basePosition,
                templateCamera.transform.rotation);
            _cameraRig.Configure(
                pastPlayer,
                futurePlayer,
                basePosition,
                false,
                0f,
                cameraTimelineRole,
                pastProgressOriginX,
                futureProgressOriginX);
            _camera.enabled = true;
        }

        /// <summary>보조 카메라를 비활성화한다.</summary>
        public void Hide()
        {
            if (_camera != null)
            {
                _camera.enabled = false;
            }

            _cameraRig?.StopFollowing();
        }
    }
}
