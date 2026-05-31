using NUnit.Framework;

using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

using Caretaker.Gameplay;
using Caretaker.Presentation;
using Caretaker.World;

namespace Caretaker.Tests.Editor
{
    public class NetworkPlayerPrefabTests
    {
        private const string NETWORK_PLAYER_PREFAB_PATH = "Assets/_Project/Prefabs/Player/NetworkPlayer.prefab";
        private const string NETWORK_LOBBY_SCENE_PATH = "Assets/_Project/Scenes/NetworkLobby.unity";
        private const string PERSISTENT_SCENE_PATH = "Assets/_Project/Scenes/Persistent.unity";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";

        [Test]
        public void NetworkPlayerPrefab_HasRequiredRuntimeComponents()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NETWORK_PLAYER_PREFAB_PATH);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<NetworkObject>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<NetworkPlayerOwnerGate>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerController>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerInputReader>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<InteractionProbe>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<RoomParticipant>(), Is.Not.Null);
        }

        [Test]
        public void NetworkPlayerPrefab_HasWhiteboxVisual()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NETWORK_PLAYER_PREFAB_PATH);

            Assert.That(prefab, Is.Not.Null);

            Transform visual = prefab.transform.Find("Visual");

            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.GetComponent<MeshRenderer>(), Is.Not.Null);
            Assert.That(visual.GetComponent<Collider>(), Is.Null);
        }

        [Test]
        public void NetworkPlayerPrefab_UsesOwnerAuthoritativeNetworkTransform()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NETWORK_PLAYER_PREFAB_PATH);

            Assert.That(prefab, Is.Not.Null);

            NetworkTransform networkTransform = prefab.GetComponent<NetworkTransform>();

            Assert.That(networkTransform, Is.Not.Null);
            Assert.That(networkTransform.AuthorityMode, Is.EqualTo(NetworkTransform.AuthorityModes.Owner));
        }

        [Test]
        public void NetworkPlayerPrefab_UsesProjectInputActions()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NETWORK_PLAYER_PREFAB_PATH);
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(inputActions, Is.Not.Null);

            PlayerInput playerInput = prefab.GetComponent<PlayerInput>();

            Assert.That(playerInput, Is.Not.Null);
            Assert.That(playerInput.actions, Is.SameAs(inputActions));
            Assert.That(playerInput.defaultActionMap, Is.EqualTo("Player"));
        }

        [Test]
        public void NetworkLobby_DoesNotAutoSpawnWorldPlayerPrefab()
        {
            var scene = EditorSceneManager.OpenScene(NETWORK_LOBBY_SCENE_PATH, OpenSceneMode.Single);
            NetworkManager networkManager = Object.FindAnyObjectByType<NetworkManager>();

            Assert.That(scene.IsValid(), Is.True);
            Assert.That(networkManager, Is.Not.Null);
            Assert.That(networkManager.NetworkConfig.PlayerPrefab, Is.Null);
        }

        [Test]
        public void PersistentScene_SpawnsLocalPlayerAfterPhaseSceneLoad()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NETWORK_PLAYER_PREFAB_PATH);

            Assert.That(prefab, Is.Not.Null);

            var scene = EditorSceneManager.OpenScene(PERSISTENT_SCENE_PATH, OpenSceneMode.Single);
            LocalWorldPlayerSpawner spawner = Object.FindAnyObjectByType<LocalWorldPlayerSpawner>();

            Assert.That(scene.IsValid(), Is.True);
            Assert.That(spawner, Is.Not.Null);

            var serializedObject = new SerializedObject(spawner);
            Assert.That(serializedObject.FindProperty("_playerPrefab").objectReferenceValue, Is.SameAs(prefab));
            Assert.That(Object.FindAnyObjectByType<LocalPlayerCameraFollow>(), Is.Not.Null);
        }
    }
}
