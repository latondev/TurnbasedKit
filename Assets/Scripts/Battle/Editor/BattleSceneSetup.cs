#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using GameSystems.AutoBattle;
using GameSystems.AutoBattle.Visuals;
using UnityEditor.SceneManagement;

namespace GameSystems.AutoBattle.EditorUtils
{
    public class BattleSceneSetup
    {
        [MenuItem("Tools/Auto Battle/Create Demo Scene")]
        public static void CreateDemoScene()
        {
            // Creates a slightly lit scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // 1. Create Core Manager
            GameObject managerObj = new GameObject("BattleManager");
            var controller = managerObj.AddComponent<AutoBattleController>();
            var visualManager = managerObj.AddComponent<BattleVisualManager>();
            visualManager.battleController = controller;

            // 2. Create Canvas
            GameObject canvasObj = new GameObject("BattleCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();

            // 3. Spawner
            var spawner = managerObj.AddComponent<BattleDemoSpawner>();
            spawner.controller = controller;
            spawner.visualManager = visualManager;
            spawner.canvasTransform = canvasObj.transform;

            // 4. Set Camera & Stage
            Camera.main.transform.position = new Vector3(0, 10, -15);
            Camera.main.transform.rotation = Quaternion.Euler(30, 0, 0);

            GameObject stage = GameObject.CreatePrimitive(PrimitiveType.Plane);
            stage.name = "Stage";
            stage.transform.position = Vector3.zero;
            stage.transform.localScale = new Vector3(3, 1, 2);
            
            var stageRend = stage.GetComponent<Renderer>();
            stageRend.sharedMaterial.color = new Color(0.2f, 0.4f, 0.2f); // Dark green stage

            // 5. Lighting
            Light dirLight = Object.FindObjectOfType<Light>();
            if (dirLight == null)
            {
                var lightObj = new GameObject("Directional Light");
                dirLight = lightObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }
            dirLight.transform.rotation = Quaternion.Euler(50, -30, 0);
            dirLight.intensity = 1.0f;

            Selection.activeGameObject = managerObj;
            Debug.Log("✅ Default Battle Demo scene generated! Press Play to start!");
        }
    }
}
#endif
