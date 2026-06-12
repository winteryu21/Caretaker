using System.Reflection;

using NUnit.Framework;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

using Caretaker.Gameplay;
using Caretaker.Shared;

namespace Caretaker.Tests.Editor
{
    public class EnemyControllerTests
    {
        private const BindingFlags INSTANCE_PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int FLYING_MOVEMENT_MODE_INDEX = 1;

        [Test]
        public void TickPatrol_SetsHorizontalVelocityTowardNextWaypointAfterInitialArrival()
        {
            EnemyController controller = CreateController(
                Vector3.zero,
                CreateTuning(2f, 0f),
                CreateWaypoint("WaypointA", Vector3.zero),
                CreateWaypoint("WaypointB", Vector3.right));

            TickPatrol(controller, 0.1f);
            TickPatrol(controller, 0.25f);

            Assert.That(controller.CurrentPatrolWaypointIndex, Is.EqualTo(1));
            Assert.That(controller.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(2f).Within(0.001f));
            Assert.That(controller.transform.localScale.x, Is.LessThan(0f));

            DestroyController(controller);
        }

        [Test]
        public void TickPatrol_WrapsFromLastWaypointToFirst()
        {
            EnemyController controller = CreateController(
                Vector3.zero,
                CreateTuning(1f, 0f),
                CreateWaypoint("WaypointA", Vector3.zero),
                CreateWaypoint("WaypointB", new Vector3(2f, 0f, 0f)));

            TickPatrol(controller, 0.1f);
            controller.GetComponent<Rigidbody2D>().position = new Vector2(2f, 0f);
            TickPatrol(controller, 0.1f);

            Assert.That(controller.CurrentPatrolWaypointIndex, Is.EqualTo(0));

            DestroyController(controller);
        }

        [Test]
        public void TickPatrol_WaitsAtWaypointBeforeMovingAgain()
        {
            EnemyController controller = CreateController(
                Vector3.zero,
                CreateTuning(1f, 0.5f),
                CreateWaypoint("WaypointA", Vector3.zero),
                CreateWaypoint("WaypointB", Vector3.right));

            TickPatrol(controller, 0.1f);
            TickPatrol(controller, 0.25f);

            Assert.That(controller.IsWaitingAtWaypoint, Is.True);
            Assert.That(controller.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(0f).Within(0.001f));

            TickPatrol(controller, 0.25f);
            TickPatrol(controller, 0.25f);

            Assert.That(controller.IsWaitingAtWaypoint, Is.False);
            Assert.That(controller.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(1f).Within(0.001f));

            DestroyController(controller);
        }

        [Test]
        public void TickPatrol_SkipsWaypointAfterStuckTime()
        {
            EnemyController controller = CreateController(
                Vector3.zero,
                CreateTuning(1f, 0f),
                0,
                0.5f,
                CreateWaypoint("WaypointA", Vector3.zero),
                CreateWaypoint("WaypointB", new Vector3(10f, 0f, 0f)));

            TickPatrol(controller, 0.1f);
            TickPatrol(controller, 0.25f);
            TickPatrol(controller, 0.25f);
            TickPatrol(controller, 0.25f);

            Assert.That(controller.CurrentPatrolWaypointIndex, Is.EqualTo(0));

            DestroyController(controller);
        }

        [Test]
        public void GroundedMode_StopsBeforePlatformEdge()
        {
            EnemyController controller = CreateController(
                new Vector3(0f, 0.55f, 0f),
                CreateTuning(2f, 0f),
                CreateWaypoint("WaypointA", new Vector3(0f, 0.55f, 0f)),
                CreateWaypoint("WaypointB", new Vector3(2f, 0.55f, 0f)));
            BoxCollider2D enemyCollider = controller.GetComponent<BoxCollider2D>();
            enemyCollider.size = Vector2.one;
            GameObject ground = new("ShortPlatform");
            BoxCollider2D groundCollider = ground.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2(1f, 0.1f);
            ground.transform.position = Vector3.zero;
            InvokeOnValidate(controller);

            TickPatrol(controller, 0.1f);
            TickPatrol(controller, 0.1f);

            Assert.That(controller.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(0f).Within(0.001f));

            Object.DestroyImmediate(ground);
            DestroyController(controller);
        }

        [Test]
        public void TickPatrol_MissingConfigurationDoesNotMoveOrThrow()
        {
            GameObject gameObject = new("Enemy");
            EnemyController controller = gameObject.AddComponent<EnemyController>();

            Assert.DoesNotThrow(() => TickPatrol(controller, 1f));
            Assert.That(gameObject.GetComponent<Rigidbody2D>().position, Is.EqualTo(Vector2.zero));

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void FlyingMode_UsesKinematicBodyWithoutGravity()
        {
            EnemyController controller = CreateController(
                Vector3.zero,
                CreateTuning(2f, 0f),
                FLYING_MOVEMENT_MODE_INDEX,
                CreateWaypoint("WaypointA", Vector3.zero),
                CreateWaypoint("WaypointB", Vector3.up));

            Rigidbody2D body = controller.GetComponent<Rigidbody2D>();

            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
            Assert.That(body.gravityScale, Is.EqualTo(0f).Within(0.001f));

            DestroyController(controller);
        }

        [Test]
        public void OnEnable_AssignsCurrentPlayerFromSpawnerInSameScene()
        {
            const string phaseSceneName = "Phase1_Past";

            Scene phaseScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            phaseScene.name = phaseSceneName;
            GameObject playerPrefab = new("NetworkPlayerPrefab");
            playerPrefab.AddComponent<PlayerMotor2D>();
            GameObject spawnerObject = new("Spawner");
            LocalWorldPlayerSpawner spawner = spawnerObject.AddComponent<LocalWorldPlayerSpawner>();
            SerializedObject spawnerSerializedObject = new(spawner);
            spawnerSerializedObject.FindProperty("_playerPrefab").objectReferenceValue = playerPrefab;
            spawnerSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            GameObject player = spawner.SpawnLocalPlayer(PhaseId.Phase1, TimelineRole.Past, phaseSceneName);

            GameObject enemyObject = new("Enemy");
            EnemyController enemy = enemyObject.AddComponent<EnemyController>();

            Assert.That(GetTargetPlayer(enemy), Is.SameAs(player.GetComponent<PlayerMotor2D>()));

            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(spawnerObject);
            Object.DestroyImmediate(playerPrefab);
        }

        [Test]
        public void SpawnLocalPlayer_AssignsPlayerToAlreadyEnabledEnemy()
        {
            const string phaseSceneName = "Phase1_Past";

            Scene phaseScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            phaseScene.name = phaseSceneName;
            GameObject enemyObject = new("Enemy");
            EnemyController enemy = enemyObject.AddComponent<EnemyController>();
            GameObject playerPrefab = new("NetworkPlayerPrefab");
            playerPrefab.AddComponent<PlayerMotor2D>();
            GameObject spawnerObject = new("Spawner");
            LocalWorldPlayerSpawner spawner = spawnerObject.AddComponent<LocalWorldPlayerSpawner>();
            SerializedObject spawnerSerializedObject = new(spawner);
            spawnerSerializedObject.FindProperty("_playerPrefab").objectReferenceValue = playerPrefab;
            spawnerSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            GameObject player = spawner.SpawnLocalPlayer(PhaseId.Phase1, TimelineRole.Past, phaseSceneName);

            Assert.That(GetTargetPlayer(enemy), Is.SameAs(player.GetComponent<PlayerMotor2D>()));

            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(spawnerObject);
            Object.DestroyImmediate(playerPrefab);
        }

        [Test]
        public void SpawnLocalPlayer_ReplacesAlreadyEnabledEnemyTarget()
        {
            const string phaseSceneName = "Phase1_Past";

            Scene phaseScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            phaseScene.name = phaseSceneName;
            GameObject enemyObject = new("Enemy");
            EnemyController enemy = enemyObject.AddComponent<EnemyController>();
            GameObject playerPrefab = new("NetworkPlayerPrefab");
            playerPrefab.AddComponent<PlayerMotor2D>();
            GameObject spawnerObject = new("Spawner");
            LocalWorldPlayerSpawner spawner = spawnerObject.AddComponent<LocalWorldPlayerSpawner>();
            SerializedObject spawnerSerializedObject = new(spawner);
            spawnerSerializedObject.FindProperty("_playerPrefab").objectReferenceValue = playerPrefab;
            spawnerSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            GameObject firstPlayer = spawner.SpawnLocalPlayer(PhaseId.Phase1, TimelineRole.Past, phaseSceneName);
            GameObject secondPlayer = spawner.SpawnLocalPlayer(PhaseId.Phase1, TimelineRole.Past, phaseSceneName);

            Assert.That(GetTargetPlayer(enemy), Is.SameAs(secondPlayer.GetComponent<PlayerMotor2D>()));
            Assert.That(GetTargetPlayer(enemy), Is.Not.SameAs(firstPlayer.GetComponent<PlayerMotor2D>()));

            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(firstPlayer);
            Object.DestroyImmediate(secondPlayer);
            Object.DestroyImmediate(spawnerObject);
            Object.DestroyImmediate(playerPrefab);
        }

        [Test]
        public void TickEnemy_DetectsChasesSearchesThenReturnsToPatrol()
        {
            EnemyController controller = CreateController(
                Vector3.zero,
                CreateTuning(1f, 0f, 10f, 45f, 4f, 10f),
                CreateWaypoint("WaypointA", Vector3.zero),
                CreateWaypoint("WaypointB", Vector3.right));
            controller.transform.localScale = new Vector3(-1f, 1f, 1f);
            PlayerMotor2D player = CreatePlayer(new Vector3(5f, 0f, 0f));

            SerializedObject perceptionObject = new(controller.GetComponent<EnemyPerception2D>());
            perceptionObject.FindProperty("_tuning").objectReferenceValue = CreateTuning(1f, 0f, 10f, 45f, 4f, 10f);
            perceptionObject.FindProperty("_obstructionLayers").intValue = 0;
            perceptionObject.ApplyModifiedPropertiesWithoutUndo();
            controller.SetTargetPlayer(player);
            InvokeOnValidate(controller);

            TickEnemy(controller, 0.1f);

            Assert.That(controller.CurrentState, Is.EqualTo(EnemyStateMachine.EnemyState.Patrol));

            TickEnemy(controller, 0.9f);

            Assert.That(controller.CurrentState, Is.EqualTo(EnemyStateMachine.EnemyState.Chase));
            Assert.That(controller.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(4f).Within(0.001f));

            player.transform.position = new Vector3(-5f, 0f, 0f);
            TickEnemy(controller, 0.1f);

            Assert.That(controller.CurrentState, Is.EqualTo(EnemyStateMachine.EnemyState.Search));
            Assert.That(controller.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(1f).Within(0.001f));

            TickEnemy(controller, 10.1f);

            Assert.That(controller.CurrentState, Is.EqualTo(EnemyStateMachine.EnemyState.Patrol));

            Object.DestroyImmediate(player.gameObject);
            DestroyController(controller);
        }

        [Test]
        public void EnemyStateMachine_SearchLastsForConfiguredLoseSightDelay()
        {
            EnemyStateMachine stateMachine = new();

            Assert.That(stateMachine.TickState(true, 0.1f, 10f), Is.EqualTo(EnemyStateMachine.EnemyState.Chase));
            Assert.That(stateMachine.TickState(false, 0.1f, 10f), Is.EqualTo(EnemyStateMachine.EnemyState.Search));
            Assert.That(stateMachine.TickState(false, 9.9f, 10f), Is.EqualTo(EnemyStateMachine.EnemyState.Search));
            Assert.That(stateMachine.TickState(false, 0.2f, 10f), Is.EqualTo(EnemyStateMachine.EnemyState.Patrol));
        }

        [Test]
        public void EnemyStateMachine_ChasesAfterContinuousDetectionDelay()
        {
            EnemyStateMachine stateMachine = new();

            Assert.That(
                stateMachine.TickState(true, false, 0.5f, 10f, 1f),
                Is.EqualTo(EnemyStateMachine.EnemyState.Patrol));
            Assert.That(
                stateMachine.TickState(false, false, 0.1f, 10f, 1f),
                Is.EqualTo(EnemyStateMachine.EnemyState.Patrol));
            Assert.That(
                stateMachine.TickState(true, false, 0.9f, 10f, 1f),
                Is.EqualTo(EnemyStateMachine.EnemyState.Patrol));
            Assert.That(
                stateMachine.TickState(true, false, 0.1f, 10f, 1f),
                Is.EqualTo(EnemyStateMachine.EnemyState.Chase));
        }

        [Test]
        public void Phase3Chaser_UsesSharedConstantSpeedProgress()
        {
            float sharedX = Phase3Chaser.CalculateSharedX(
                startX: -8f,
                speed: 4f,
                elapsedSeconds: 2.5f);

            Assert.That(sharedX, Is.EqualTo(2f).Within(0.001f));
        }

        [TestCase(4.2f, 5f, 0.75f, false)]
        [TestCase(4.25f, 5f, 0.75f, true)]
        [TestCase(5f, 5f, 0.75f, true)]
        public void Phase3Chaser_CapturesOnlyInsideConfiguredDistance(
            float chaserX,
            float playerX,
            float captureDistance,
            bool expected)
        {
            Assert.That(
                Phase3Chaser.IsPlayerCaught(chaserX, playerX, captureDistance),
                Is.EqualTo(expected));
        }

        [Test]
        public void EvaluateSight_AppliesRangeFovCrouchAndObstructionRules()
        {
            EnemyPerception2D perception = CreatePerception(CreateTuning(1f, 0f, 10f, 45f, 4f, 10f), 0);
            perception.SetFacingDirection(Vector2.right);

            Assert.That(perception.EvaluateSight(new Vector2(6f, 0f), false), Is.True);
            Assert.That(perception.EvaluateSight(new Vector2(6f, 0f), true), Is.False);
            Assert.That(perception.EvaluateSight(new Vector2(6f, 6f), false), Is.False);

            Object.DestroyImmediate(perception.gameObject);
        }

        [Test]
        public void EvaluateSight_DetectsTouchingPlayerOnlyInFront()
        {
            EnemyPerception2D perception = CreatePerception(CreateTuning(1f, 0f, 10f, 45f, 4f, 10f), 0);
            perception.SetFacingDirection(Vector2.right);
            GameObject player = new("Player");
            BoxCollider2D playerCollider = player.AddComponent<BoxCollider2D>();

            player.transform.position = Vector2.right;
            Physics2D.SyncTransforms();

            Assert.That(perception.EvaluateSight(player.transform.position, false, playerCollider), Is.True);

            player.transform.position = Vector2.left;
            Physics2D.SyncTransforms();

            Assert.That(perception.EvaluateSight(player.transform.position, false, playerCollider), Is.False);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(perception.gameObject);
        }

        [Test]
        public void EvaluateSight_ReturnsFalseWhenObstacleBlocksRaycast()
        {
            EnemyPerception2D perception = CreatePerception(CreateTuning(1f, 0f, 10f, 45f, 4f, 10f), 1);
            perception.SetFacingDirection(Vector2.right);
            GameObject obstacle = new("Obstacle");
            obstacle.transform.position = new Vector3(2f, 0f, 0f);
            BoxCollider2D obstacleCollider = obstacle.AddComponent<BoxCollider2D>();
            obstacleCollider.size = Vector2.one;
            Physics2D.SyncTransforms();

            Assert.That(perception.EvaluateSight(new Vector2(5f, 0f), false), Is.False);

            Object.DestroyImmediate(obstacle);
            Object.DestroyImmediate(perception.gameObject);
        }

        [Test]
        public void CreateVisionArea_DoesNotCreateRuntimeResourcesInEditMode()
        {
            EnemyPerception2D perception = CreatePerception(CreateTuning(1f, 0f), 0);
            MethodInfo methodInfo = typeof(EnemyPerception2D).GetMethod(
                "CreateVisionArea",
                INSTANCE_PRIVATE);

            Assert.DoesNotThrow(() => methodInfo.Invoke(perception, null));
            Assert.That(perception.transform.Find("VisionArea"), Is.Null);

            Object.DestroyImmediate(perception.gameObject);
        }

        [Test]
        public void CanBeTakenDownBy_RequiresRangeAndRearAngle()
        {
            EnemyController controller = CreateController(Vector3.zero, CreateTuning(1f, 0f));
            controller.GetComponent<EnemyPerception2D>().SetFacingDirection(Vector2.right);
            Physics2D.SyncTransforms();

            Assert.That(controller.CanBeTakenDownBy(new Vector2(-1.5f, 0f)), Is.True);
            Assert.That(controller.CanBeTakenDownBy(new Vector2(1f, 0f)), Is.False);
            Assert.That(controller.CanBeTakenDownBy(new Vector2(0f, 1f)), Is.False);
            Assert.That(controller.CanBeTakenDownBy(new Vector2(-2.1f, 0f)), Is.False);

            DestroyController(controller);
        }

        [Test]
        public void CanBeTakenDownBy_ReturnsFalseWhileChasing()
        {
            EnemyController controller = CreateController(Vector3.zero, CreateTuning(1f, 0f));
            controller.GetComponent<EnemyPerception2D>().SetFacingDirection(Vector2.right);
            EnemyStateMachine stateMachine = GetStateMachine(controller);
            stateMachine.TickState(true, 0.1f, 10f);

            Assert.That(controller.CurrentState, Is.EqualTo(EnemyStateMachine.EnemyState.Chase));
            Assert.That(controller.CanBeTakenDownBy(new Vector2(-1f, 0f)), Is.False);

            DestroyController(controller);
        }

        [Test]
        public void ReactivateAfterTakedown_RestoresEnemyComponentsAndState()
        {
            EnemyController controller = CreateController(Vector3.zero, CreateTuning(1f, 0f));
            EnemyPerception2D perception = controller.GetComponent<EnemyPerception2D>();
            Collider2D enemyCollider = controller.GetComponent<Collider2D>();
            Rigidbody2D body = controller.GetComponent<Rigidbody2D>();

            SetPrivateField(controller, "_isBeingTakenDown", true);
            SetPrivateField(controller, "_requiresTakedownReset", true);
            enemyCollider.enabled = false;
            body.simulated = false;
            perception.enabled = false;

            controller.gameObject.SetActive(false);
            controller.gameObject.SetActive(true);

            Assert.That(controller.IsBeingTakenDown, Is.False);
            Assert.That(controller.CurrentState, Is.EqualTo(EnemyStateMachine.EnemyState.Patrol));
            Assert.That(enemyCollider.enabled, Is.True);
            Assert.That(body.simulated, Is.True);
            Assert.That(perception.enabled, Is.True);
            Vector2 rearPosition = (Vector2)controller.transform.position - perception.FacingDirection;
            Assert.That(controller.CanBeTakenDownBy(rearPosition), Is.True);

            DestroyController(controller);
        }

        private static EnemyController CreateController(
            Vector3 position,
            EnemyTuningSO tuning,
            params Transform[] waypoints)
        {
            return CreateController(position, tuning, 0, 5f, waypoints);
        }

        private static EnemyController CreateController(
            Vector3 position,
            EnemyTuningSO tuning,
            int movementModeIndex,
            params Transform[] waypoints)
        {
            return CreateController(position, tuning, movementModeIndex, 5f, waypoints);
        }

        private static EnemyController CreateController(
            Vector3 position,
            EnemyTuningSO tuning,
            int movementModeIndex,
            float stuckSkipSeconds,
            params Transform[] waypoints)
        {
            GameObject gameObject = new("Enemy");
            gameObject.transform.position = position;
            EnemyController controller = gameObject.AddComponent<EnemyController>();

            SerializedObject serializedObject = new(controller);
            serializedObject.FindProperty("_movementMode").enumValueIndex = movementModeIndex;
            serializedObject.FindProperty("_tuning").objectReferenceValue = tuning;
            serializedObject.FindProperty("_stuckSkipSeconds").floatValue = stuckSkipSeconds;
            SerializedProperty waypointProperty = serializedObject.FindProperty("_patrolWaypoints");
            waypointProperty.arraySize = waypoints.Length;
            for (int i = 0; i < waypoints.Length; i++)
            {
                waypoints[i].SetParent(gameObject.transform, true);
                waypointProperty.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            InvokeOnValidate(controller);
            InvokeConfigureRigidbody(controller);

            return controller;
        }

        private static EnemyTuningSO CreateTuning(float moveSpeed, float patrolWaitTime)
        {
            return CreateTuning(moveSpeed, patrolWaitTime, 10f, 45f, 5f, 10f);
        }

        private static EnemyTuningSO CreateTuning(
            float moveSpeed,
            float patrolWaitTime,
            float sightDistance,
            float fovDegrees,
            float chaseSpeed,
            float loseSightSeconds,
            float chaseStartDelaySeconds = 1f)
        {
            EnemyTuningSO tuning = ScriptableObject.CreateInstance<EnemyTuningSO>();
            SerializedObject serializedObject = new(tuning);
            serializedObject.FindProperty("_moveSpeed").floatValue = moveSpeed;
            serializedObject.FindProperty("_patrolWaitTime").floatValue = patrolWaitTime;
            serializedObject.FindProperty("_sightDistance").floatValue = sightDistance;
            serializedObject.FindProperty("_fovDegrees").floatValue = fovDegrees;
            serializedObject.FindProperty("_chaseStartDelaySeconds").floatValue = chaseStartDelaySeconds;
            serializedObject.FindProperty("_chaseSpeed").floatValue = chaseSpeed;
            serializedObject.FindProperty("_loseSightSeconds").floatValue = loseSightSeconds;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return tuning;
        }

        private static PlayerMotor2D CreatePlayer(Vector3 position)
        {
            GameObject gameObject = new("Player");
            gameObject.transform.position = position;
            return gameObject.AddComponent<PlayerMotor2D>();
        }

        private static EnemyPerception2D CreatePerception(EnemyTuningSO tuning, int obstructionLayerMask)
        {
            GameObject gameObject = new("EnemyPerception");
            gameObject.AddComponent<BoxCollider2D>();
            EnemyPerception2D perception = gameObject.AddComponent<EnemyPerception2D>();
            SerializedObject serializedObject = new(perception);
            serializedObject.FindProperty("_tuning").objectReferenceValue = tuning;
            serializedObject.FindProperty("_obstructionLayers").intValue = obstructionLayerMask;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return perception;
        }

        private static PlayerMotor2D GetTargetPlayer(EnemyController enemy)
        {
            FieldInfo fieldInfo = typeof(EnemyController).GetField("_targetPlayer", INSTANCE_PRIVATE);
            return (PlayerMotor2D)fieldInfo.GetValue(enemy);
        }

        private static EnemyStateMachine GetStateMachine(EnemyController enemy)
        {
            FieldInfo fieldInfo = typeof(EnemyController).GetField("_stateMachine", INSTANCE_PRIVATE);
            return (EnemyStateMachine)fieldInfo.GetValue(enemy);
        }

        private static void SetPrivateField<T>(EnemyController enemy, string fieldName, T value)
        {
            FieldInfo fieldInfo = typeof(EnemyController).GetField(fieldName, INSTANCE_PRIVATE);
            fieldInfo.SetValue(enemy, value);
        }

        private static Transform CreateWaypoint(string name, Vector3 position)
        {
            GameObject gameObject = new(name);
            gameObject.transform.position = position;
            return gameObject.transform;
        }

        private static void InvokeOnValidate(EnemyController controller)
        {
            MethodInfo methodInfo = typeof(EnemyController).GetMethod("OnValidate", INSTANCE_PRIVATE);
            methodInfo.Invoke(controller, null);
        }

        private static void InvokeConfigureRigidbody(EnemyController controller)
        {
            MethodInfo methodInfo = typeof(EnemyController).GetMethod("ConfigureRigidbody", INSTANCE_PRIVATE);
            methodInfo.Invoke(controller, null);
        }

        private static void TickPatrol(EnemyController controller, float deltaTime)
        {
            MethodInfo methodInfo = typeof(EnemyController).GetMethod("TickPatrol", INSTANCE_PRIVATE);
            methodInfo.Invoke(controller, new object[] { deltaTime });
        }

        private static void TickEnemy(EnemyController controller, float deltaTime)
        {
            MethodInfo methodInfo = typeof(EnemyController).GetMethod("TickEnemy", INSTANCE_PRIVATE);
            methodInfo.Invoke(controller, new object[] { deltaTime });
        }

        private static void DestroyController(EnemyController controller)
        {
            foreach (Transform waypoint in controller.GetComponentsInChildren<Transform>())
            {
                if (waypoint != controller.transform)
                {
                    Object.DestroyImmediate(waypoint.gameObject);
                }
            }

            Object.DestroyImmediate(controller.gameObject);
        }
    }
}
