using Caretaker.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Caretaker.Tests.Editor
{
    public sealed class SplitViewTests
    {
        [Test]
        public void TimelineCameraRig_UsesSlowerPlayerProgress()
        {
            GameObject cameraObject = new("Camera");
            GameObject pastPlayer = new("PastPlayer");
            GameObject futurePlayer = new("FuturePlayer");

            try
            {
                pastPlayer.transform.position = new Vector3(12f, 0f, 0f);
                futurePlayer.transform.position = new Vector3(8f, 1000f, 0f);
                TimelineCameraRig rig = cameraObject.AddComponent<TimelineCameraRig>();

                rig.Configure(
                    pastPlayer.transform,
                    futurePlayer.transform,
                    new Vector3(0f, 1002f, -10f));

                Assert.That(cameraObject.transform.position.x, Is.EqualTo(8f));
                Assert.That(cameraObject.transform.position.y, Is.EqualTo(1002f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(pastPlayer);
                Object.DestroyImmediate(futurePlayer);
            }
        }

        [Test]
        public void RemoteTimelineView_UsesDirectCameraWithoutRenderTexture()
        {
            GameObject viewObject = new("RemoteView");
            GameObject templateObject = new("TemplateCamera");
            GameObject pastPlayer = new("PastPlayer");
            GameObject futurePlayer = new("FuturePlayer");

            try
            {
                RemoteTimelineView view = viewObject.AddComponent<RemoteTimelineView>();
                Camera template = templateObject.AddComponent<Camera>();
                view.Initialize();
                view.Show(
                    template,
                    pastPlayer.transform,
                    futurePlayer.transform,
                    new Rect(0f, 0f, 1f, 0.5f));

                Camera remoteCamera = viewObject.GetComponentInChildren<Camera>(true);
                Assert.That(remoteCamera, Is.Not.Null);
                Assert.That(remoteCamera.targetTexture, Is.Null);
                Assert.That(remoteCamera.rect, Is.EqualTo(new Rect(0f, 0f, 1f, 0.5f)));
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(templateObject);
                Object.DestroyImmediate(pastPlayer);
                Object.DestroyImmediate(futurePlayer);
            }
        }

        [Test]
        public void PersistentScene_HasSplitViewManager()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Persistent.unity", OpenSceneMode.Single);

            Assert.That(Object.FindAnyObjectByType<SplitViewManager>(), Is.Not.Null);
        }
    }
}
