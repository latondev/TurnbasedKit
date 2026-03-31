using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameSystems.Battle.Editor
{
    public static class BattleDemoSceneSetup
    {
        private static readonly string PREFAB_FOLDER = "Assets/AssetGame/ArtWork/Prefab/Role";
        private static readonly string CONFIG_PATH = "Assets/Scripts/Battle/Examples/BattlePrefabConfig.asset";

        // Prefab names cho player team
        private static readonly string[] PLAYER_PREFABS = {
            "yang_jian", "fei_yu", "jing_wei", "lei_zhen_zi", "chu_chu"
        };

        // Prefab names cho enemy team
        private static readonly string[] ENEMY_PREFABS = {
            "tian_bing", "xing_tian", "dao_ba_tu", "mo_jian_shi", "tao_tie"
        };

        [MenuItem("Tools/Setup BattleDemo Scene")]
        public static void SetupScene()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/BattleDemo.unity", OpenSceneMode.Single);

            // Clean existing root objects (keep camera/light)
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root.name != "Main Camera" && root.name != "Directional Light")
                    Object.DestroyImmediate(root);
            }

            // 1. BattleManager
            var battleManagerGO = new GameObject("BattleManager");
            battleManagerGO.AddComponent<GameSystems.AutoBattle.AutoBattleController>();
            var sceneSetup = battleManagerGO.AddComponent<GameSystems.Battle.Demo.BattleSceneSetup>();

            // 2. CharacterManager
            var charManagerGO = new GameObject("CharacterManager");
            charManagerGO.AddComponent<GameSystems.Battle.CharacterManager>();

            // 3. UIManager + BattleUIView
            var uiManagerGO = new GameObject("UIManager");
            uiManagerGO.AddComponent<GameSystems.Battle.Demo.BattleUIView>();

            // 4. VisualManager
            var visualManagerGO = new GameObject("VisualManager");
            var visualManager = visualManagerGO.AddComponent<GameSystems.Battle.Demo.BattleVisualManager>();

            // 5. EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
            }

            // 6. Ensure Camera
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                var camGO = new GameObject("Main Camera");
                cam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
                camGO.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // 7. Setup BattlePrefabConfig asset với prefabs
            SetupPrefabConfig(visualManager);

            // Mark dirty & save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("✅ BattleDemo Scene setup complete with Visual Manager!");
        }

        [MenuItem("Tools/Assign Battle Prefabs to Config")]
        public static void AssignPrefabsToConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameSystems.Battle.Demo.BattlePrefabConfig>(CONFIG_PATH);
            if (config == null)
            {
                Debug.LogError($"Config not found at {CONFIG_PATH}");
                return;
            }

            AssignPrefabsToConfigAsset(config);
            Debug.Log("✅ Prefabs assigned to BattlePrefabConfig!");
        }

        private static void SetupPrefabConfig(GameSystems.Battle.Demo.BattleVisualManager visualManager)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameSystems.Battle.Demo.BattlePrefabConfig>(CONFIG_PATH);
            if (config == null)
            {
                Debug.LogWarning($"BattlePrefabConfig not found at {CONFIG_PATH}. Skipping prefab assignment.");
                return;
            }

            AssignPrefabsToConfigAsset(config);

            // Assign config to VisualManager
            var so = new SerializedObject(visualManager);
            var configProp = so.FindProperty("_config");
            if (configProp != null)
            {
                configProp.objectReferenceValue = config;
                so.ApplyModifiedProperties();
            }
        }

        private static void AssignPrefabsToConfigAsset(GameSystems.Battle.Demo.BattlePrefabConfig config)
        {
            var so = new SerializedObject(config);

            // Assign player prefabs
            var playerProp = so.FindProperty("_playerPrefabs");
            playerProp.arraySize = PLAYER_PREFABS.Length;
            for (int i = 0; i < PLAYER_PREFABS.Length; i++)
            {
                string path = $"{PREFAB_FOLDER}/{PLAYER_PREFABS[i]}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    playerProp.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
                    Debug.Log($"  Player[{i}]: {PLAYER_PREFABS[i]} ✓");
                }
                else
                {
                    Debug.LogWarning($"  Player[{i}]: {path} not found!");
                }
            }

            // Assign enemy prefabs
            var enemyProp = so.FindProperty("_enemyPrefabs");
            enemyProp.arraySize = ENEMY_PREFABS.Length;
            for (int i = 0; i < ENEMY_PREFABS.Length; i++)
            {
                string path = $"{PREFAB_FOLDER}/{ENEMY_PREFABS[i]}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    enemyProp.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
                    Debug.Log($"  Enemy[{i}]: {ENEMY_PREFABS[i]} ✓");
                }
                else
                {
                    Debug.LogWarning($"  Enemy[{i}]: {path} not found!");
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }
}
