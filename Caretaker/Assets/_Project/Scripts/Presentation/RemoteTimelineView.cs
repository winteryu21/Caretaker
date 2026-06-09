using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// Owns the read-only camera used to render the other timeline directly.
    /// </summary>
    public sealed class RemoteTimelineView : MonoBehaviour
    {
        private Camera _camera;
        private TimelineCameraRig _cameraRig;

        /// <summary>Creates the disabled auxiliary camera.</summary>
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

        /// <summary>Renders the remote timeline directly into the supplied viewport.</summary>
        public void Show(
            Camera templateCamera,
            Transform pastPlayer,
            Transform futurePlayer,
            Rect viewport)
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
                templateCamera.transform.position,
                templateCamera.transform.rotation);
            _cameraRig.Configure(pastPlayer, futurePlayer, templateCamera.transform.position);
            _camera.enabled = true;
        }

        /// <summary>Disables the auxiliary camera.</summary>
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
