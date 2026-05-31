using NUnit.Framework;

using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

using Caretaker.Gameplay;
using Caretaker.Shared;
using Caretaker.World;

namespace Caretaker.Tests.Editor
{
    public class LocalWorldPlayerSpawnerTests
    {
        [Test]
        public void SpawnLocalPlayer_CreatesRoomParticipantInLoadedPhaseScene()
        {
            const string phaseSceneName = "Phase1_Past";

            Scene phaseScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            phaseScene.name = phaseSceneName;
            GameObject prefab = new("NetworkPlayerPrefab");
            prefab.AddComponent<RoomParticipant>();
            GameObject spawnerObject = new("Spawner");
            LocalWorldPlayerSpawner spawner = spawnerObject.AddComponent<LocalWorldPlayerSpawner>();

            var serializedObject = new UnityEditor.SerializedObject(spawner);
            serializedObject.FindProperty("_playerPrefab").objectReferenceValue = prefab;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            GameObject player = spawner.SpawnLocalPlayer(PhaseId.Phase1, TimelineRole.Past, phaseSceneName);

            Assert.That(player, Is.Not.Null);
            Assert.That(player.name, Is.EqualTo("LocalWorldPlayer_Past_Phase1"));
            Assert.That(player.GetComponent<RoomParticipant>(), Is.Not.Null);
            Assert.That(player.scene.name, Is.EqualTo(phaseSceneName));

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(spawnerObject);
            Object.DestroyImmediate(prefab);
        }
    }
}
