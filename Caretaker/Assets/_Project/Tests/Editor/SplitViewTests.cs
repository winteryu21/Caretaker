using Caretaker.Presentation;
using Caretaker.Shared;
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

        [TestCase(TimelineRole.Past, -80.3f)]
        [TestCase(TimelineRole.Future, -23f)]
        public void TimelineCameraRig_UsesTimelineOriginsForCameraProgress(
            TimelineRole cameraTimelineRole,
            float expectedCameraX)
        {
            GameObject cameraObject = new("Camera");
            GameObject pastPlayer = new("PastPlayer");
            GameObject futurePlayer = new("FuturePlayer");

            try
            {
                pastPlayer.transform.position = new Vector3(-77.3f, 0f, 0f);
                futurePlayer.transform.position = new Vector3(-23f, 1000f, 0f);
                TimelineCameraRig rig = cameraObject.AddComponent<TimelineCameraRig>();

                rig.Configure(
                    pastPlayer.transform,
                    futurePlayer.transform,
                    new Vector3(0f, 1002f, -10f),
                    false,
                    0f,
                    cameraTimelineRole,
                    -83.3f,
                    -26f);

                Assert.That(cameraObject.transform.position.x, Is.EqualTo(expectedCameraX).Within(0.001f));
                Assert.That(cameraObject.transform.position.y, Is.EqualTo(1002f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(pastPlayer);
                Object.DestroyImmediate(futurePlayer);
            }
        }

        [TestCase(9.9f, 5f, 0f, 10f, 0.02f, 5f)]
        [TestCase(9.95f, 5f, 0f, 10f, 0.02f, 2.5f)]
        [TestCase(0.05f, -5f, 0f, 10f, 0.02f, -2.5f)]
        [TestCase(5f, -5f, 0f, 10f, 0.02f, -5f)]
        [TestCase(10.1f, -2f, 0f, 10f, 0.02f, -2f)]
        [TestCase(10.1f, 2f, 0f, 10f, 0.02f, 0f)]
        [TestCase(-0.1f, 2f, 0f, 10f, 0.02f, 2f)]
        [TestCase(-0.1f, -2f, 0f, 10f, 0.02f, 0f)]
        public void PlayerMotor2D_ClampsPredictedMovementAtBounds(
            float positionX,
            float velocityX,
            float minimumX,
            float maximumX,
            float deltaTime,
            float expected)
        {
            float result = PlayerMotor2D.ClampHorizontalVelocity(
                positionX,
                velocityX,
                minimumX,
                maximumX,
                deltaTime);

            Assert.That(result, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void SplitViewManager_UsesNarrowerCameraForMaximumSeparation()
        {
            float separation = SplitViewManager.ResolveMaximumPlayerSeparation(
                configuredSeparation: 7f,
                localHalfWidth: 8f,
                remoteHalfWidth: 5f,
                boundaryPadding: 1f);

            Assert.That(separation, Is.EqualTo(4f));
        }

        [Test]
        public void SplitViewManager_ZeroConfiguredSeparation_UsesVisibleCameraRange()
        {
            float separation = SplitViewManager.ResolveMaximumPlayerSeparation(
                configuredSeparation: 0f,
                localHalfWidth: 8f,
                remoteHalfWidth: 5f,
                boundaryPadding: 1f);

            Assert.That(separation, Is.EqualTo(4f));
        }

        [Test]
        public void SplitViewManager_ConfiguredSeparation_CapsVisibleCameraRange()
        {
            float separation = SplitViewManager.ResolveMaximumPlayerSeparation(
                configuredSeparation: 3f,
                localHalfWidth: 8f,
                remoteHalfWidth: 5f,
                boundaryPadding: 1f);

            Assert.That(separation, Is.EqualTo(3f));
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
