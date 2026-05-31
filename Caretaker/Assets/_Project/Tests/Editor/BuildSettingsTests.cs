using NUnit.Framework;

using UnityEditor;
using UnityEngine;

namespace Caretaker.Tests.Editor
{
    public class BuildSettingsTests
    {
        [Test]
        public void EnabledBuildSettingScenes_ExistOnDisk()
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled)
                {
                    continue;
                }

                Assert.That(scene.path, Is.Not.Empty);
                Assert.That(
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path),
                    Is.Not.Null,
                    $"Build Settings references a missing scene: {scene.path}");
            }
        }
    }
}
