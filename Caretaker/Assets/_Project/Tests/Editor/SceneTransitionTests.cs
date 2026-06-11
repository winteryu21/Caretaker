using NUnit.Framework;

using Caretaker.Core;
using Caretaker.Gameplay;
using Caretaker.Shared;
using Caretaker.World;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Caretaker.Tests.Editor
{
    public class SceneTransitionTests
    {
        [Test]
        public void TryTransitionActor_MovesActorToLoadedSceneSpawnPoint()
        {
            const string targetSceneName = "SceneTransitionTarget";
            const string spawnPointName = "EntryPoint";
            Vector3 spawnPosition = new(7f, 3f, 0f);

            Scene sourceScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            sourceScene.name = "SceneTransitionSource";
            PlayerController actor = CreateInactiveActor("Actor");
            SceneTransitionManager transitionManager = CreateTransitionManager("SceneTransitionManager");
            SceneManager.MoveGameObjectToScene(actor.gameObject, sourceScene);
            SceneManager.MoveGameObjectToScene(transitionManager.gameObject, sourceScene);

            Scene targetScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            targetScene.name = targetSceneName;
            GameObject spawnPoint = new(spawnPointName);
            spawnPoint.transform.position = spawnPosition;
            SceneManager.MoveGameObjectToScene(spawnPoint, targetScene);

            try
            {
                bool requested = transitionManager.TryTransitionActor(
                    actor,
                    targetSceneName,
                    spawnPointName,
                    unloadActorScene: false);

                Assert.That(requested, Is.True);
                Assert.That(actor.gameObject.scene.name, Is.EqualTo(targetSceneName));
                Assert.That(actor.transform.position, Is.EqualTo(spawnPosition));
            }
            finally
            {
                Object.DestroyImmediate(actor.gameObject);
                Object.DestroyImmediate(transitionManager.gameObject);
                Object.DestroyImmediate(spawnPoint);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void OperateAction_AddsOperateTypeAndUsesSceneTransitionManager()
        {
            const string targetSceneName = "SceneTransitionOperateTarget";
            const string spawnPointName = "OperateSpawn";
            Vector3 spawnPosition = new(-2f, 4f, 0f);

            Scene sourceScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            sourceScene.name = "SceneTransitionOperateSource";
            PlayerController actor = CreateInactiveActor("Actor");
            SceneTransitionManager transitionManager = CreateTransitionManager("SceneTransitionManager");
            InteractableObject interactable = CreateSceneTransitionInteractable(
                "Portal",
                targetSceneName,
                spawnPointName,
                transitionManager);

            SceneManager.MoveGameObjectToScene(actor.gameObject, sourceScene);
            SceneManager.MoveGameObjectToScene(transitionManager.gameObject, sourceScene);
            SceneManager.MoveGameObjectToScene(interactable.gameObject, sourceScene);

            Scene targetScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            targetScene.name = targetSceneName;
            GameObject spawnPoint = new(spawnPointName);
            spawnPoint.transform.position = spawnPosition;
            SceneManager.MoveGameObjectToScene(spawnPoint, targetScene);

            try
            {
                Assert.That(interactable.IsInteractable(InteractionType.Operate), Is.True);

                bool executed = interactable.RunInteraction(InteractionType.Operate, actor);

                Assert.That(executed, Is.True);
                Assert.That(actor.gameObject.scene.name, Is.EqualTo(targetSceneName));
                Assert.That(actor.transform.position, Is.EqualTo(spawnPosition));
            }
            finally
            {
                Object.DestroyImmediate(actor.gameObject);
                Object.DestroyImmediate(transitionManager.gameObject);
                Object.DestroyImmediate(interactable.gameObject);
                Object.DestroyImmediate(spawnPoint);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void TryTransitionActor_RejectsMissingTargetSceneName()
        {
            PlayerController actor = CreateInactiveActor("Actor");
            SceneTransitionManager transitionManager = CreateTransitionManager("SceneTransitionManager");

            try
            {
                bool requested = transitionManager.TryTransitionActor(actor, " ", "SpawnPoint");

                Assert.That(requested, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(actor.gameObject);
                Object.DestroyImmediate(transitionManager.gameObject);
            }
        }

        private static SceneTransitionManager CreateTransitionManager(string name)
        {
            GameObject gameObject = new(name);
            return gameObject.AddComponent<SceneTransitionManager>();
        }

        private static InteractableObject CreateSceneTransitionInteractable(
            string name,
            string targetSceneName,
            string spawnPointName,
            SceneTransitionManager transitionManager)
        {
            GameObject gameObject = new(name);
            gameObject.AddComponent<CircleCollider2D>();
            InteractableObject interactable = gameObject.AddComponent<InteractableObject>();
            SceneTransitionOperateAction action = gameObject.AddComponent<SceneTransitionOperateAction>();

            SerializedObject serializedObject = new(action);
            serializedObject.FindProperty("_targetSceneName").stringValue = targetSceneName;
            serializedObject.FindProperty("_spawnPointName").stringValue = spawnPointName;
            serializedObject.FindProperty("_transitionManager").objectReferenceValue = transitionManager;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return interactable;
        }

        private static PlayerController CreateInactiveActor(string name)
        {
            GameObject gameObject = new(name);
            gameObject.SetActive(false);
            gameObject.AddComponent<NetworkObject>();
            gameObject.AddComponent<PlayerInput>();
            gameObject.AddComponent<Rigidbody2D>();
            gameObject.AddComponent<BoxCollider2D>();
            gameObject.AddComponent<PlayerInputReader>();
            gameObject.AddComponent<global::PlayerMotor2D>();
            gameObject.AddComponent<InteractionProbe>();
            gameObject.AddComponent<InventoryController>();
            return gameObject.AddComponent<PlayerController>();
        }
    }
}
