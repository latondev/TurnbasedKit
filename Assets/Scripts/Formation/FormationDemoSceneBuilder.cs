using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using GameSystems.Formation;
using System.Collections.Generic;
using System.IO;

namespace GameSystems.Editor
{
    public class FormationDemoSceneBuilder
    {
        private const string DEMO_FOLDER = "Assets/Core/GameSystems/Formation/DemoAssets";

        [MenuItem("Tools/Formation System/🚀 Build Full 3D Demo Scene")]
        public static void BuildDemoScene()
        {
            // 1. Create Folders
            if (!AssetDatabase.IsValidFolder("Assets/Core")) AssetDatabase.CreateFolder("Assets", "Core");
            if (!AssetDatabase.IsValidFolder("Assets/Core/GameSystems")) AssetDatabase.CreateFolder("Assets/Core", "GameSystems");
            if (!AssetDatabase.IsValidFolder("Assets/Core/GameSystems/Formation")) AssetDatabase.CreateFolder("Assets/Core/GameSystems", "Formation");
            if (!AssetDatabase.IsValidFolder(DEMO_FOLDER)) AssetDatabase.CreateFolder("Assets/Core/GameSystems/Formation", "DemoAssets");

            // 2. Create Materials
            Material matEmpty = CreateMaterial("Mat_SlotEmpty", new Color(0.2f, 0.2f, 0.2f, 0.5f));
            Material matOccupied = CreateMaterial("Mat_SlotOccupied", new Color(0.1f, 0.6f, 0.2f, 0.8f));
            Material matHover = CreateMaterial("Mat_SlotHover", new Color(0.9f, 0.8f, 0.2f, 0.8f));
            
            Material matHero = CreateMaterial("Mat_Hero", Color.cyan);
            Material matWarrior = CreateMaterial("Mat_Warrior", Color.red);
            Material matArcher = CreateMaterial("Mat_Archer", Color.green);
            Material matMage = CreateMaterial("Mat_Mage", Color.magenta);

            // 3. Create Prefabs
            GameObject slotPrefab = CreateSlotPrefab();
            GameObject heroPrefab = CreateUnitPrefab("Hero", PrimitiveType.Capsule, matHero, 1.2f);
            GameObject warriorPrefab = CreateUnitPrefab("Warrior", PrimitiveType.Cube, matWarrior, 1f);
            GameObject archerPrefab = CreateUnitPrefab("Archer", PrimitiveType.Cylinder, matArcher, 0.8f);
            GameObject magePrefab = CreateUnitPrefab("Mage", PrimitiveType.Sphere, matMage, 1f);

            // 4. Create New Scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // 5. Setup Environment
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5, 1, 5);
            Material groundMat = new Material(Shader.Find("Standard"));
            groundMat.color = new Color(0.15f, 0.15f, 0.2f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // Setup Camera
            Camera.main.transform.position = new Vector3(0, 12, -8);
            Camera.main.transform.rotation = Quaternion.Euler(55, 0, 0);
            Camera.main.backgroundColor = new Color(0.1f, 0.1f, 0.15f);

            // 6. Setup Canvas UI
            GameObject canvasObj = new GameObject("DemoCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Setup Event System
            if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Create some instructions text
            GameObject textObj = new GameObject("InstructionsText");
            textObj.transform.SetParent(canvasObj.transform, false);
            Text txt = textObj.AddComponent<Text>();
            txt.text = "FORMATION SYSTEM 3D DEMO\n\n- Click <color=yellow>Left</color> on empty slot to Spawn\n- <color=cyan>Drag & Drop</color> units to Swap\n- Click <color=red>Right</color> on unit to Remove";
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 20;
            txt.color = Color.white;
            txt.supportRichText = true;
            RectTransform txtRt = textObj.GetComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0, 1);
            txtRt.anchorMax = new Vector2(0, 1);
            txtRt.pivot = new Vector2(0, 1);
            txtRt.anchoredPosition = new Vector2(20, -20);
            txtRt.sizeDelta = new Vector2(400, 150);

            // 7. Setup Manager
            GameObject managerParam = new GameObject("FormationManager");
            var formationCtrl = managerParam.AddComponent<FormationController>();
            var visualDemo = managerParam.AddComponent<FormationVisualDemo>();

            // Assign visual variables
            var so = new SerializedObject(visualDemo);
            so.FindProperty("gridCenter").objectReferenceValue = managerParam.transform;
            so.FindProperty("slotSpacing").floatValue = 2.5f;
            so.FindProperty("slotMaterialEmpty").objectReferenceValue = matEmpty;
            so.FindProperty("slotMaterialOccupied").objectReferenceValue = matOccupied;
            so.FindProperty("slotMaterialHover").objectReferenceValue = matHover;
            so.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
            
            // Assign unit visuals via SerializedProperty
            var unitListProp = so.FindProperty("unitVisuals");
            unitListProp.ClearArray();
            
            AddUnitVisualToProp(unitListProp, "Hero", heroPrefab);
            AddUnitVisualToProp(unitListProp, "Warrior", warriorPrefab);
            AddUnitVisualToProp(unitListProp, "Archer", archerPrefab);
            AddUnitVisualToProp(unitListProp, "Mage", magePrefab);
            
            so.ApplyModifiedProperties();

            // 8. Save Scene
            string scenePath = $"{DEMO_FOLDER}/FormationDemo3D.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);

            // Now open the UI components on FormationVisualDemo to remove OnGUI because we have real UI
            // Or just instruct user. We will stick with OnGUI for buttons (Next/Prev) 
            // but the instructions are now visually beautiful on Canvas.

            Debug.Log("<color=green>✅ FULL 3D DEMO SCENE GENERATED SUCCESSFULLY!</color>");
            Debug.Log($"Open it at: {scenePath}");
        }

        private static void AddUnitVisualToProp(SerializedProperty arrayProp, string unitId, GameObject prefab)
        {
            arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize);
            SerializedProperty elementProp = arrayProp.GetArrayElementAtIndex(arrayProp.arraySize - 1);
            elementProp.FindPropertyRelative("unitId").stringValue = unitId;
            elementProp.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = $"{DEMO_FOLDER}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                
                // Make transparent if alpha < 1
                if (color.a < 1f)
                {
                    mat.SetFloat("_Mode", 3); // Transparent
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.DisableKeyword("_ALPHABLEND_ON");
                    mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                }
                
                mat.color = color;
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static GameObject CreateSlotPrefab()
        {
            string path = $"{DEMO_FOLDER}/Prefabs";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(DEMO_FOLDER, "Prefabs");
            
            string prefabPath = $"{path}/SlotVisual.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = "SlotVisual";
            obj.transform.rotation = Quaternion.Euler(90, 0, 0); // Lay flat
            obj.transform.localScale = new Vector3(2f, 2f, 1f);
            
            // Ensure BoxCollider has some thickness for raycasting
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<MeshCollider>());
            BoxCollider box = obj.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, 0.2f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            UnityEngine.Object.DestroyImmediate(obj);
            return prefab;
        }

        private static GameObject CreateUnitPrefab(string name, PrimitiveType type, Material mat, float scale)
        {
            string path = $"{DEMO_FOLDER}/Prefabs/{name}Visual.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.localScale = Vector3.one * scale;
            
            // Move pivot to bottom loosely
            obj.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
            
            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            // Add simple eyes
            GameObject eye1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye1.transform.SetParent(obj.transform);
            eye1.transform.localPosition = new Vector3(0.2f, 0.5f, 0.4f);
            eye1.transform.localScale = Vector3.one * 0.2f;
            eye1.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = Color.black };

            GameObject eye2 = UnityEngine.Object.Instantiate(eye1, obj.transform);
            eye2.transform.localPosition = new Vector3(-0.2f, 0.5f, 0.4f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            UnityEngine.Object.DestroyImmediate(obj);
            return prefab;
        }
    }
}
