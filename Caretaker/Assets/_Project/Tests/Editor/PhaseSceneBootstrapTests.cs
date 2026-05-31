using NUnit.Framework;

using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Caretaker.Tests.Editor
{
    public class PhaseSceneBootstrapTests
    {
        [TestCase("Assets/_Project/Scenes/Phase1_Past.unity", "MergePastPhase1")]
        [TestCase("Assets/_Project/Scenes/Phase1_Future.unity", "MergeFuturePhase1")]
        public void Phase1Scene_HasMapPrefabAndTilemapCollision(string scenePath, string mapRootName)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);
            Assert.That(GameObject.Find(mapRootName), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<TilemapCollider2D>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<CompositeCollider2D>(), Is.Not.Null);
            Assert.That(GameObject.Find("DebugGroundCollider"), Is.Null);
            Assert.That(GameObject.Find("DebugGroundVisual"), Is.Null);
        }
    }
}
