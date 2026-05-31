using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;

using UnityEngine;

using Caretaker.Gameplay;
using Caretaker.Shared;
using Caretaker.World;

namespace Caretaker.Editor
{
    /// <summary>
    /// Creates initial ScriptableObject seed assets for designers.
    /// </summary>
    public static class SoSeedAssetGenerator
    {
        private const string DATA_ROOT = "Assets/_Project/Data";
        private const string CAUSALITY_FOLDER = DATA_ROOT + "/Causality";
        private const string INVENTORY_FOLDER = DATA_ROOT + "/Inventory";
        private const string ROOMS_FOLDER = DATA_ROOT + "/Rooms";
        private const string AI_FOLDER = DATA_ROOT + "/AI";

        private static readonly CausalRuleSeed[] CAUSAL_RULE_SEEDS =
        {
            new(
                "CR_P1_POWER_LEVER",
                "CR_P1_POWER_LEVER",
                string.Empty,
                Array.Empty<ConditionSeed>(),
                new[] { new EffectSeed("RCV_P1_BREAKER_PANEL", "powerState", "Powered") },
                CausalRuleSO.InteractionWeight.Minor),
            new(
                "CR_P1_POWER_PANEL",
                "CR_P1_POWER_PANEL",
                string.Empty,
                new[] { new ConditionSeed("breakerCombination", "Correct") },
                new[]
                {
                    new EffectSeed("RCV_P1_FLOOR_POWER", "powerState", "On"),
                    new EffectSeed("RCV_P1_SECURITY_SYSTEM", "securityState", "Online")
                },
                CausalRuleSO.InteractionWeight.Major),
            new(
                "CR_P1_VENT_OPEN",
                "CR_P1_VENT_OPEN",
                "ITEM_TOOL_DRIVER",
                Array.Empty<ConditionSeed>(),
                new[] { new EffectSeed("RCV_P1_VENT_PATH", "pathState", "Open") },
                CausalRuleSO.InteractionWeight.Minor),
            new(
                "CR_P1_SEC_HACK",
                "CR_P1_SEC_HACK",
                string.Empty,
                new[] { new ConditionSeed("securityCode", "Accepted") },
                new[] { new EffectSeed("RCV_P1_SECURITY_NETWORK", "recordAccess", "Unlocked") },
                CausalRuleSO.InteractionWeight.Major),
            new(
                "CR_P2_BLUEPRINT_ID",
                "CR_P2_BLUEPRINT_ID",
                string.Empty,
                new[] { new ConditionSeed("blueprintSelection", "Correct") },
                new[] { new EffectSeed("RCV_P2_BLUEPRINT_STATE", "blueprintState", "Identified") },
                CausalRuleSO.InteractionWeight.Major),
            new(
                "CR_P2_VIRUS_PLANT",
                "CR_P2_VIRUS_PLANT",
                string.Empty,
                new[] { new ConditionSeed("virusPuzzle", "Solved") },
                new[] { new EffectSeed("RCV_P2_CYLINDER_LOCK", "lockState", "Unlocked") },
                CausalRuleSO.InteractionWeight.Major),
            new(
                "CR_P3_BRIDGE_DROP",
                "CR_P3_BRIDGE_DROP",
                string.Empty,
                Array.Empty<ConditionSeed>(),
                new[] { new EffectSeed("RCV_P3_BRIDGE_PLATFORM", "platformState", "Deployed") },
                CausalRuleSO.InteractionWeight.Minor),
            new(
                "CR_P3_DOOR_UNLOCK",
                "CR_P3_DOOR_UNLOCK",
                string.Empty,
                Array.Empty<ConditionSeed>(),
                new[] { new EffectSeed("RCV_P3_EMERGENCY_DOOR", "lockState", "Unlocked") },
                CausalRuleSO.InteractionWeight.Minor),
            new(
                "CR_P3_PLATFORM_LOWER",
                "CR_P3_PLATFORM_LOWER",
                string.Empty,
                Array.Empty<ConditionSeed>(),
                new[] { new EffectSeed("RCV_P3_HIGH_PLATFORM", "heightState", "Lowered") },
                CausalRuleSO.InteractionWeight.Minor)
        };

        private static readonly ItemSeed[] ITEM_SEEDS =
        {
            new("ITEM_TOOL_DRIVER", "드라이버", "Tool", new[] { "VentScrew" }, false),
            new("ITEM_CABLE", "전력 케이블", "Power", new[] { "BreakerPanel", "AuxPowerDevice" }, true),
            new("ITEM_BATTERY", "예비 배터리", "Power", new[] { "AuxPowerDevice" }, true),
            new("ITEM_KEY_CARD", "카드키", "Access", new[] { "CardReader" }, false),
            new("ITEM_P3_TOOL", "(TBD) Phase 3 도구", "TBD", new[] { "Phase3Obstacle" }, false)
        };

        private static readonly RoomBaseSeed[] ROOM_BASE_SEEDS =
        {
            new("A_1F_LOBBY", "A동 1F 연구소 로비", new[] { "A_B1_SUBSTATION_PATH", "A_1F_SECURITY_HALL" }),
            new("A_B1_SUBSTATION_PATH", "A동 B1 변전실로 가는 길", new[] { "A_1F_LOBBY", "A_B1_SUBSTATION_HALL" }),
            new("A_B1_SUBSTATION_HALL", "A동 B1 변전실 앞 복도", new[] { "A_B1_SUBSTATION_PATH", "A_B1_SUBSTATION", "A_1F_SECURITY_HALL" }),
            new("A_B1_SUBSTATION", "A동 B1 변전실", new[] { "A_B1_SUBSTATION_HALL" }),
            new("A_1F_SECURITY_HALL", "A동 1F 보안실 앞 복도", new[] { "A_1F_LOBBY", "A_B1_SUBSTATION_HALL", "A_1F_SECURITY_ROOM", "A_2F_HALL" }),
            new("A_1F_SECURITY_ROOM", "A동 1F 보안실", new[] { "A_1F_SECURITY_HALL" }),
            new("A_2F_DIRECTOR_OFFICE", "A동 2F 연구소장실", new[] { "A_2F_HALL" }),
            new("A_2F_HALL", "A동 2F 2층 복도", new[] { "A_1F_SECURITY_HALL", "A_2F_DIRECTOR_OFFICE", "A_TO_B_SKYBRIDGE" }),
            new("A_TO_B_SKYBRIDGE", "구름다리", new[] { "A_2F_HALL", "B_2F_HALL" }),
            new("B_2F_HALL", "B동 2F 복도", new[] { "A_TO_B_SKYBRIDGE", "B_2F_ARCHIVE", "B_1F_HALL" }),
            new("B_2F_ARCHIVE", "B동 2F 자료실", new[] { "B_2F_HALL" }),
            new("B_1F_HALL", "B동 1F 복도", new[] { "B_2F_HALL", "B_1F_LAB", "B_1F_SAMPLE_STORAGE" }),
            new("B_1F_LAB", "B동 1F 실험실", new[] { "B_1F_HALL" }),
            new("B_1F_SAMPLE_STORAGE", "B동 1F 샘플 저장고", new[] { "B_1F_HALL" }),
            new("C_ESCAPE_SEGMENT_01", "폐쇄된 저장고 C동 구간 1", new[] { "C_ESCAPE_SEGMENT_02" }),
            new("C_ESCAPE_SEGMENT_02", "폐쇄된 저장고 C동 구간 2", new[] { "C_ESCAPE_SEGMENT_01", "C_ESCAPE_SEGMENT_03" }),
            new("C_ESCAPE_SEGMENT_03", "폐쇄된 저장고 C동 구간 3", new[] { "C_ESCAPE_SEGMENT_02" })
        };

        private static readonly EnemySeed[] ENEMY_SEEDS =
        {
            new("PAST_GUARD", 2f, 1f, 10f, 45f, 5f, 10f, 30f),
            new("FUTURE_ROBOT", 2.2f, 1f, 10f, 45f, 5f, 10f, 30f),
            new("CCTV", 0f, 0f, 10f, 45f, 0f, 10f, 30f),
            new("PHASE3_CHASER", 4f, 0f, 12f, 60f, 6f, 0f, 0f)
        };

        /// <summary>
        /// Creates missing seed assets without overwriting existing designer edits.
        /// </summary>
        [MenuItem("Caretaker SO/Seed/Create Missing Seed Data Assets")]
        public static void CreateMissingSeedDataAssets()
        {
            EnsureSeedFolders();

            int createdCount = 0;
            int skippedCount = 0;

            CreateCausalRuleAssets(ref createdCount, ref skippedCount);
            CreateItemAssets(ref createdCount, ref skippedCount);
            CreateRoomAssets(ref createdCount, ref skippedCount);
            CreateEnemyAssets(ref createdCount, ref skippedCount);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"SO seed generation complete. Created: {createdCount}, skipped existing: {skippedCount}");
        }

        private static void EnsureSeedFolders()
        {
            EnsureFolder(DATA_ROOT, "Causality");
            EnsureFolder(DATA_ROOT, "Inventory");
            EnsureFolder(DATA_ROOT, "Rooms");
            EnsureFolder(DATA_ROOT, "AI");
        }

        private static void CreateCausalRuleAssets(ref int createdCount, ref int skippedCount)
        {
            foreach (CausalRuleSeed seed in CAUSAL_RULE_SEEDS)
            {
                string path = $"{CAUSALITY_FOLDER}/SO_{seed.RuleId}.asset";
                CreateAssetIfMissing<CausalRuleSO>(path, asset =>
                {
                    SerializedObject serializedObject = new(asset);
                    SetString(serializedObject, "_ruleId", seed.RuleId);
                    SetString(serializedObject, "_triggerId", seed.TriggerId);
                    SetEnum(serializedObject, "_requiredRole", TimelineRole.Past);
                    SetString(serializedObject, "_requiredItemId", seed.RequiredItemId);
                    SetConditions(serializedObject, seed.Conditions);
                    SetEffects(serializedObject, seed.Effects);
                    SetEnum(serializedObject, "_interactionWeight", seed.Weight);
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }, ref createdCount, ref skippedCount);
            }
        }

        private static void CreateItemAssets(ref int createdCount, ref int skippedCount)
        {
            foreach (ItemSeed seed in ITEM_SEEDS)
            {
                string path = $"{INVENTORY_FOLDER}/SO_{seed.ItemId}.asset";
                CreateAssetIfMissing<ItemDefinitionSO>(path, asset =>
                {
                    SerializedObject serializedObject = new(asset);
                    SetString(serializedObject, "_itemId", seed.ItemId);
                    SetString(serializedObject, "_displayName", seed.DisplayName);
                    SetString(serializedObject, "_category", seed.Category);
                    SetStringArray(serializedObject, "_usableTargetTags", seed.UsableTargetTags);
                    SetBool(serializedObject, "_consumable", seed.Consumable);
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }, ref createdCount, ref skippedCount);
            }
        }

        private static void CreateRoomAssets(ref int createdCount, ref int skippedCount)
        {
            CreateTimelineRoomAssets(TimelineRole.Past, "PAST", "FUTURE", ref createdCount, ref skippedCount);
            CreateTimelineRoomAssets(TimelineRole.Future, "FUTURE", "PAST", ref createdCount, ref skippedCount);
        }

        private static void CreateTimelineRoomAssets(
            TimelineRole timeline,
            string timelinePrefix,
            string pairedTimelinePrefix,
            ref int createdCount,
            ref int skippedCount)
        {
            foreach (RoomBaseSeed seed in ROOM_BASE_SEEDS)
            {
                string roomId = $"{timelinePrefix}_{seed.RoomKey}";
                string path = $"{ROOMS_FOLDER}/SO_{roomId}.asset";
                string[] adjacentRoomIds = seed.AdjacentRoomKeys.Select(key => $"{timelinePrefix}_{key}").ToArray();
                string[] spawnPointIds = { $"SPAWN_{timelinePrefix}_{seed.RoomKey}" };

                CreateAssetIfMissing<RoomGraphSO>(path, asset =>
                {
                    SerializedObject serializedObject = new(asset);
                    SetString(serializedObject, "_roomId", roomId);
                    SetString(serializedObject, "_displayName", seed.DisplayName);
                    SetEnum(serializedObject, "_timeline", timeline);
                    SetStringArray(serializedObject, "_adjacentRoomIds", adjacentRoomIds);
                    SetString(serializedObject, "_pairedTimelineRoomId", $"{pairedTimelinePrefix}_{seed.RoomKey}");
                    SetStringArray(serializedObject, "_spawnPointIds", spawnPointIds);
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }, ref createdCount, ref skippedCount);
            }
        }

        private static void CreateEnemyAssets(ref int createdCount, ref int skippedCount)
        {
            foreach (EnemySeed seed in ENEMY_SEEDS)
            {
                string path = $"{AI_FOLDER}/SO_Enemy_{seed.EnemyType}.asset";
                CreateAssetIfMissing<EnemyTuningSO>(path, asset =>
                {
                    SerializedObject serializedObject = new(asset);
                    SetString(serializedObject, "_enemyType", seed.EnemyType);
                    SetFloat(serializedObject, "_moveSpeed", seed.MoveSpeed);
                    SetFloat(serializedObject, "_patrolWaitTime", seed.PatrolWaitTime);
                    SetFloat(serializedObject, "_sightDistance", seed.SightDistance);
                    SetFloat(serializedObject, "_fovDegrees", seed.FovDegrees);
                    SetFloat(serializedObject, "_chaseSpeed", seed.ChaseSpeed);
                    SetFloat(serializedObject, "_loseSightSeconds", seed.LoseSightSeconds);
                    SetFloat(serializedObject, "_alertDuration", seed.AlertDuration);
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }, ref createdCount, ref skippedCount);
            }
        }

        private static void CreateAssetIfMissing<T>(
            string path,
            Action<T> configure,
            ref int createdCount,
            ref int skippedCount)
            where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                skippedCount++;
                return;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, path);
            createdCount++;
        }

        private static void EnsureFolder(string parentPath, string folderName)
        {
            string folderPath = $"{parentPath}/{folderName}";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        private static void SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            serializedObject.FindProperty(propertyName).stringValue = value;
        }

        private static void SetStringArray(SerializedObject serializedObject, string propertyName, IReadOnlyList<string> values)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.arraySize = values.Count;

            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }

        private static void SetConditions(SerializedObject serializedObject, IReadOnlyList<ConditionSeed> conditions)
        {
            SerializedProperty property = serializedObject.FindProperty("_conditions");
            property.arraySize = conditions.Count;

            for (int i = 0; i < conditions.Count; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_conditionKey").stringValue = conditions[i].ConditionKey;
                element.FindPropertyRelative("_expectedValue").stringValue = conditions[i].ExpectedValue;
            }
        }

        private static void SetEffects(SerializedObject serializedObject, IReadOnlyList<EffectSeed> effects)
        {
            SerializedProperty property = serializedObject.FindProperty("_receiverEffects");
            property.arraySize = effects.Count;

            for (int i = 0; i < effects.Count; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_receiverId").stringValue = effects[i].ReceiverId;
                element.FindPropertyRelative("_stateKey").stringValue = effects[i].StateKey;
                element.FindPropertyRelative("_stateValue").stringValue = effects[i].StateValue;
            }
        }

        private static void SetEnum<TEnum>(SerializedObject serializedObject, string propertyName, TEnum value)
            where TEnum : Enum
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            int enumIndex = Array.IndexOf(property.enumNames, value.ToString());
            property.enumValueIndex = enumIndex >= 0 ? enumIndex : Convert.ToInt32(value);
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            serializedObject.FindProperty(propertyName).floatValue = value;
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            serializedObject.FindProperty(propertyName).boolValue = value;
        }

        private readonly struct CausalRuleSeed
        {
            public CausalRuleSeed(
                string ruleId,
                string triggerId,
                string requiredItemId,
                IReadOnlyList<ConditionSeed> conditions,
                IReadOnlyList<EffectSeed> effects,
                CausalRuleSO.InteractionWeight weight)
            {
                RuleId = ruleId;
                TriggerId = triggerId;
                RequiredItemId = requiredItemId;
                Conditions = conditions;
                Effects = effects;
                Weight = weight;
            }

            public string RuleId { get; }
            public string TriggerId { get; }
            public string RequiredItemId { get; }
            public IReadOnlyList<ConditionSeed> Conditions { get; }
            public IReadOnlyList<EffectSeed> Effects { get; }
            public CausalRuleSO.InteractionWeight Weight { get; }
        }

        private readonly struct ConditionSeed
        {
            public ConditionSeed(string conditionKey, string expectedValue)
            {
                ConditionKey = conditionKey;
                ExpectedValue = expectedValue;
            }

            public string ConditionKey { get; }
            public string ExpectedValue { get; }
        }

        private readonly struct EffectSeed
        {
            public EffectSeed(string receiverId, string stateKey, string stateValue)
            {
                ReceiverId = receiverId;
                StateKey = stateKey;
                StateValue = stateValue;
            }

            public string ReceiverId { get; }
            public string StateKey { get; }
            public string StateValue { get; }
        }

        private readonly struct ItemSeed
        {
            public ItemSeed(string itemId, string displayName, string category, IReadOnlyList<string> usableTargetTags, bool consumable)
            {
                ItemId = itemId;
                DisplayName = displayName;
                Category = category;
                UsableTargetTags = usableTargetTags;
                Consumable = consumable;
            }

            public string ItemId { get; }
            public string DisplayName { get; }
            public string Category { get; }
            public IReadOnlyList<string> UsableTargetTags { get; }
            public bool Consumable { get; }
        }

        private readonly struct RoomBaseSeed
        {
            public RoomBaseSeed(string roomKey, string displayName, IReadOnlyList<string> adjacentRoomKeys)
            {
                RoomKey = roomKey;
                DisplayName = displayName;
                AdjacentRoomKeys = adjacentRoomKeys;
            }

            public string RoomKey { get; }
            public string DisplayName { get; }
            public IReadOnlyList<string> AdjacentRoomKeys { get; }
        }

        private readonly struct EnemySeed
        {
            public EnemySeed(
                string enemyType,
                float moveSpeed,
                float patrolWaitTime,
                float sightDistance,
                float fovDegrees,
                float chaseSpeed,
                float loseSightSeconds,
                float alertDuration)
            {
                EnemyType = enemyType;
                MoveSpeed = moveSpeed;
                PatrolWaitTime = patrolWaitTime;
                SightDistance = sightDistance;
                FovDegrees = fovDegrees;
                ChaseSpeed = chaseSpeed;
                LoseSightSeconds = loseSightSeconds;
                AlertDuration = alertDuration;
            }

            public string EnemyType { get; }
            public float MoveSpeed { get; }
            public float PatrolWaitTime { get; }
            public float SightDistance { get; }
            public float FovDegrees { get; }
            public float ChaseSpeed { get; }
            public float LoseSightSeconds { get; }
            public float AlertDuration { get; }
        }
    }
}
