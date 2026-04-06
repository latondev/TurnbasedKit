#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using GameSystems.Battle;
using GameSystems.Battle.Demo;
using Spine.Unity;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameSystems.Battle.Editor
{
    public class BattleUnitPrefabBuilderWindow : EditorWindow
    {
        private const string DefaultOutputFolder = "Assets/AssetGame/ArtWork/Prefab/BattleUnits";
        private const string SharedFloatingTextPath = "Assets/AssetGame/ArtWork/Prefab/Common/FloatingText.prefab";

        [SerializeField] private List<GameObject> sourcePrefabs = new List<GameObject>();
        [SerializeField] private string outputFolder = DefaultOutputFolder;
        [SerializeField] private bool addUnitView = true;
        [SerializeField] private bool addBattleBehaviours = true;
        [SerializeField] private bool addStatusView = true;
        [SerializeField] private bool addFloatingTextPrefab = true;
        [SerializeField] private bool autoWireAnimationRefs = true;
        [SerializeField] private bool removeMissingScripts = true;
        [SerializeField] private bool createModelWrapperIfMissing = true;
        [SerializeField] private bool createBattleReadyCopy = true;
        [SerializeField] private Vector2 scrollPosition;

        [MenuItem("Tools/Battle/Prefab Builder")]
        public static void ShowWindow()
        {
            var window = GetWindow<BattleUnitPrefabBuilderWindow>("Battle Prefab Builder");
            window.minSize = new Vector2(520f, 420f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Battle Unit Prefab Builder", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Drag prefab assets here. The tool keeps AnimationHandle on the SkeletonAnimation host, and wires the unified step-system runtime components onto the prefab root (UnitView + ActionSequenceRunner + Behit). FloatingText is generated once as a shared prefab.",
                MessageType.Info);

            DrawSettings();
            DrawPrefabDropArea();
            DrawPrefabList();
            DrawActions();
        }

        private void DrawSettings()
        {
            EditorGUILayout.Space(6f);
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
            addUnitView = EditorGUILayout.ToggleLeft("Add UnitView to prefab root", addUnitView);
            addBattleBehaviours = EditorGUILayout.ToggleLeft("Add ActionSequenceRunner + Behit (step system)", addBattleBehaviours);
            addStatusView = EditorGUILayout.ToggleLeft("Add StatusView", addStatusView);
            addFloatingTextPrefab = EditorGUILayout.ToggleLeft("Use shared FloatingText prefab + wire it", addFloatingTextPrefab);
            autoWireAnimationRefs = EditorGUILayout.ToggleLeft("Auto-wire animation refs", autoWireAnimationRefs);
            removeMissingScripts = EditorGUILayout.ToggleLeft("Remove missing scripts before build", removeMissingScripts);
            createModelWrapperIfMissing = EditorGUILayout.ToggleLeft("Create model wrapper if missing", createModelWrapperIfMissing);
            createBattleReadyCopy = EditorGUILayout.ToggleLeft("Write to new prefab copy", createBattleReadyCopy);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Selected Prefabs"))
            {
                AddSelectedPrefabs();
            }

            if (GUILayout.Button("Clear List"))
            {
                sourcePrefabs.Clear();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPrefabDropArea()
        {
            EditorGUILayout.Space(8f);
            var dropRect = GUILayoutUtility.GetRect(0f, 48f, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "Drag prefab assets here");

            HandleDragAndDrop(dropRect);
        }

        private void DrawPrefabList()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField($"Sources ({sourcePrefabs.Count})", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(120f));
            for (int i = 0; i < sourcePrefabs.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                sourcePrefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                    $"Prefab {i + 1}",
                    sourcePrefabs[i],
                    typeof(GameObject),
                    false);

                if (GUILayout.Button("X", GUILayout.Width(24f)))
                {
                    sourcePrefabs.RemoveAt(i);
                    i--;
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(8f);
            GUI.enabled = sourcePrefabs.Count > 0;
            if (GUILayout.Button("Build Battle-Ready Prefabs", GUILayout.Height(34f)))
            {
                BuildPrefabs();
            }
            GUI.enabled = true;
        }

        private void BuildPrefabs()
        {
            if (!EnsureOutputFolder())
            {
                return;
            }

            var successCount = 0;
            var failCount = 0;

            foreach (var prefab in sourcePrefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                if (!ProcessPrefab(prefab))
                {
                    failCount++;
                }
                else
                {
                    successCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BattleUnitPrefabBuilder] Done. Success: {successCount}, Failed: {failCount}");
        }

        private bool ProcessPrefab(GameObject prefabAsset)
        {
            var sourcePath = AssetDatabase.GetAssetPath(prefabAsset);
            if (string.IsNullOrEmpty(sourcePath))
            {
                Debug.LogWarning($"[BattleUnitPrefabBuilder] Skipped non-asset object: {prefabAsset.name}");
                return false;
            }

            var prefabName = Path.GetFileNameWithoutExtension(sourcePath);
            var outputPath = createBattleReadyCopy
                ? GetOutputPath(prefabName)
                : sourcePath;

            var prefabRoot = PrefabUtility.LoadPrefabContents(sourcePath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[BattleUnitPrefabBuilder] Failed to load prefab: {sourcePath}");
                return false;
            }

            try
            {
                if (!TryGetMainSkeletonHost(prefabRoot, out var skeletonHost, out var skeletonAnimation))
                {
                    Debug.LogWarning($"[BattleUnitPrefabBuilder] No SkeletonAnimation found in {sourcePath}");
                    return false;
                }

                if (removeMissingScripts)
                {
                    RemoveMissingScriptsRecursive(prefabRoot);
                }

                var viewRoot = EnsureViewRoot(prefabRoot, skeletonHost);
                EnsureAnimationHandle(skeletonHost, skeletonAnimation);
                EnsureRootComponent<UnitSocketResolver>(viewRoot, skeletonHost);

                if (addBattleBehaviours)
                {
                    EnsureRootComponent<ActionSequenceRunner>(viewRoot, skeletonHost);
                    EnsureRootComponent<BehitBehavior>(viewRoot, skeletonHost);
                }

                if (addStatusView)
                {
                    EnsureRootComponent<StatusView>(viewRoot, skeletonHost);
                }

                FloatingText floatingTextPrefab = null;
                if (addFloatingTextPrefab)
                {
                    floatingTextPrefab = EnsureFloatingTextPrefabAsset();
                }

                if (addUnitView)
                {
                    EnsureComponent<UnitView>(viewRoot);
                }

                if (autoWireAnimationRefs)
                {
                    AutoWireAnimationReferences(viewRoot, skeletonHost.GetComponent<AnimationHandle>(), floatingTextPrefab);
                }

                PrefabUtility.SaveAsPrefabAsset(viewRoot, outputPath);
                Debug.Log($"[BattleUnitPrefabBuilder] Built: {outputPath}");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private bool TryGetMainSkeletonHost(GameObject prefabRoot, out GameObject skeletonHost, out SkeletonAnimation skeletonAnimation)
        {
            skeletonHost = null;
            skeletonAnimation = null;

            var skeletons = prefabRoot.GetComponentsInChildren<SkeletonAnimation>(true);
            if (skeletons == null || skeletons.Length == 0)
            {
                return false;
            }

            SkeletonAnimation best = null;

            for (int i = 0; i < skeletons.Length; i++)
            {
                var candidate = skeletons[i];
                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.gameObject.name, "model", System.StringComparison.OrdinalIgnoreCase))
                {
                    best = candidate;
                    break;
                }
            }

            if (best == null)
            {
                for (int i = 0; i < skeletons.Length; i++)
                {
                    var candidate = skeletons[i];
                    if (candidate == null)
                    {
                        continue;
                    }

                    if (IsDescendantNamed(candidate.transform, "model"))
                    {
                        best = candidate;
                        break;
                    }
                }
            }

            if (best == null)
            {
                for (int i = 0; i < skeletons.Length; i++)
                {
                    var candidate = skeletons[i];
                    if (candidate != null)
                    {
                        best = candidate;
                        break;
                    }
                }
            }

            if (best == null)
            {
                return false;
            }

            skeletonHost = best.gameObject;
            skeletonAnimation = best;
            return true;
        }

        private GameObject EnsureViewRoot(GameObject prefabRoot, GameObject skeletonHost)
        {
            if (prefabRoot == null || skeletonHost == null)
            {
                return prefabRoot;
            }

            var existingViewRoot = prefabRoot.transform.parent != null
                ? prefabRoot.transform.parent.gameObject
                : prefabRoot;

            if (skeletonHost != prefabRoot)
            {
                return existingViewRoot;
            }

            if (!createModelWrapperIfMissing)
            {
                return prefabRoot;
            }

            if (prefabRoot.transform.parent != null)
            {
                return prefabRoot.transform.parent.gameObject;
            }

            var originalName = prefabRoot.name;
            var viewRoot = new GameObject(originalName);
            prefabRoot.transform.SetParent(viewRoot.transform, false);
            prefabRoot.name = "model";

            Debug.Log($"[BattleUnitPrefabBuilder] Created view root wrapper for {originalName}");
            return viewRoot;
        }

        private void EnsureAnimationHandle(GameObject skeletonHost, SkeletonAnimation skeletonAnimation)
        {
            if (skeletonHost == null || skeletonAnimation == null)
            {
                return;
            }

            var handle = skeletonHost.GetComponent<AnimationHandle>();
            if (handle == null)
            {
                handle = skeletonHost.AddComponent<AnimationHandle>();
            }

            handle.skeletonAnimation = skeletonAnimation;
            EditorUtility.SetDirty(handle);
        }

        private void RemoveMissingScriptsRecursive(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);

            foreach (Transform child in root.transform)
            {
                if (child == null)
                {
                    continue;
                }

                RemoveMissingScriptsRecursive(child.gameObject);
            }
        }

        private void AutoWireAnimationReferences(GameObject prefabRoot, AnimationHandle animationHandle, FloatingText floatingTextPrefab)
        {
            if (prefabRoot == null || animationHandle == null)
            {
                return;
            }

            var components = prefabRoot.GetComponentsInChildren<Component>(true);
                foreach (var component in components)
                {
                    if (component == null)
                    {
                        continue;
                    }

                    TryAssignSerializedReference(component, "animationHandle", animationHandle);
                    TryAssignSerializedReference(component, "animationController", animationHandle);
                    TryAssignSerializedReference(component, "socketResolver", prefabRoot.GetComponent<UnitSocketResolver>());
                    TryAssignSerializedReference(component, "unitSocketResolver", prefabRoot.GetComponent<UnitSocketResolver>());
                    TryAssignSerializedReference(component, "statusView", prefabRoot.GetComponent<StatusView>());

                    if (floatingTextPrefab != null)
                    {
                        TryAssignSerializedReference(component, "floatingTextPrefab", floatingTextPrefab);
                }
            }
        }

        private FloatingText EnsureFloatingTextPrefabAsset()
        {
            var sharedFolder = Path.GetDirectoryName(SharedFloatingTextPath)?.Replace('\\', '/');
            EnsureFolder(sharedFolder);

            var floatingTextPath = SharedFloatingTextPath;
            var prefab = LoadFloatingTextPrefab(floatingTextPath);
            if (prefab != null && PrefabUsesTMP(prefab))
            {
                return prefab;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(floatingTextPath) != null)
            {
                AssetDatabase.DeleteAsset(floatingTextPath);
            }

            var tempRoot = new GameObject(
                "FloatingText",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(FloatingText));

            try
            {
                var canvas = tempRoot.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;

                var rootRect = tempRoot.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(200f, 60f);

                var textGo = new GameObject("Text", typeof(RectTransform));
                textGo.transform.SetParent(tempRoot.transform, false);

                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                var text = textGo.AddComponent<TextMeshProUGUI>();
                text.text = "0";
                text.fontSize = 28;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                text.raycastTarget = false;
                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Overflow;
                text.font = ResolveTmpFontAsset();

                var floatingText = tempRoot.GetComponent<FloatingText>();
                var serializedObject = new SerializedObject(floatingText);
                var textProperty = serializedObject.FindProperty("text");
                if (textProperty != null)
                {
                    textProperty.objectReferenceValue = text;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(tempRoot, floatingTextPath);
                Debug.Log($"[BattleUnitPrefabBuilder] Created shared FloatingText prefab: {floatingTextPath}");
            }
            finally
            {
                DestroyImmediate(tempRoot);
            }

            return LoadFloatingTextPrefab(floatingTextPath);
        }

        private bool PrefabUsesTMP(FloatingText prefab)
        {
            if (prefab == null)
            {
                return false;
            }

            var serializedObject = new SerializedObject(prefab);
            var textProperty = serializedObject.FindProperty("text");
            if (textProperty == null)
            {
                return false;
            }

            if (textProperty.objectReferenceValue is TMP_Text)
            {
                return true;
            }

            return prefab.GetComponentInChildren<TMP_Text>(true) != null;
        }

        private FloatingText LoadFloatingTextPrefab(string assetPath)
        {
            var prefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefabGo == null)
            {
                return null;
            }

            return prefabGo.GetComponent<FloatingText>();
        }

        private TMP_FontAsset ResolveTmpFontAsset()
        {
            var preferredPaths = new[]
            {
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset",
                "Assets/AssetGame/ArtWork/Font/BattleNum.asset",
                "Assets/AssetGame/ArtWork/Font/RoleNum.asset",
            };

            for (int i = 0; i < preferredPaths.Length; i++)
            {
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(preferredPaths[i]);
                if (fontAsset != null)
                {
                    return fontAsset;
                }
            }

            var fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (fontGuids != null && fontGuids.Length > 0)
            {
                var fontPath = AssetDatabase.GUIDToAssetPath(fontGuids[0]);
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
                if (fontAsset != null)
                {
                    return fontAsset;
                }
            }

            Debug.LogWarning("[BattleUnitPrefabBuilder] No TMP font asset found. FloatingText will use TMP default fallback.");
            return null;
        }

        private void TryAssignSerializedReference(Component component, string propertyName, Object reference)
        {
            if (component == null || string.IsNullOrEmpty(propertyName) || reference == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(component);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            {
                return;
            }

            if (property.objectReferenceValue == reference)
            {
                return;
            }

            property.objectReferenceValue = reference;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
        }

        private T EnsureComponent<T>(GameObject target) where T : Component
        {
            if (target == null)
            {
                return null;
            }

            var component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.AddComponent<T>();
            }

            EditorUtility.SetDirty(target);
            return component;
        }

        private T EnsureRootComponent<T>(GameObject prefabRoot, GameObject sourceContainer) where T : Component
        {
            if (prefabRoot == null)
            {
                return null;
            }

            var existingRoot = prefabRoot.GetComponent<T>();
            if (existingRoot != null)
            {
                return existingRoot;
            }

            T existingSource = null;
            if (sourceContainer != null)
            {
                existingSource = sourceContainer.GetComponent<T>();
            }

            if (existingSource == null)
            {
                existingSource = prefabRoot.GetComponentInChildren<T>(true);
            }

            var rootComponent = prefabRoot.AddComponent<T>();
            if (existingSource != null && !ReferenceEquals(existingSource, rootComponent))
            {
                EditorUtility.CopySerialized(existingSource, rootComponent);
                if (existingSource.gameObject != prefabRoot)
                {
                    Object.DestroyImmediate(existingSource);
                }
            }

            EditorUtility.SetDirty(prefabRoot);
            EditorUtility.SetDirty(rootComponent);
            return rootComponent;
        }

        private void AddSelectedPrefabs()
        {
            var selection = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            foreach (var prefab in selection)
            {
                if (prefab == null)
                {
                    continue;
                }

                if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab)
                {
                    continue;
                }

                if (!sourcePrefabs.Contains(prefab))
                {
                    sourcePrefabs.Add(prefab);
                }
            }
        }

        private void HandleDragAndDrop(Rect dropRect)
        {
            var current = Event.current;
            if (!dropRect.Contains(current.mousePosition))
            {
                return;
            }

            if (current.type != EventType.DragUpdated && current.type != EventType.DragPerform)
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (current.type != EventType.DragPerform)
            {
                current.Use();
                return;
            }

            DragAndDrop.AcceptDrag();
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject go && PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
                {
                    if (!sourcePrefabs.Contains(go))
                    {
                        sourcePrefabs.Add(go);
                    }
                }
            }

            current.Use();
        }

        private bool EnsureOutputFolder()
        {
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                outputFolder = DefaultOutputFolder;
            }

            if (AssetDatabase.IsValidFolder(outputFolder))
            {
                return true;
            }

            var parentFolder = Path.GetDirectoryName(outputFolder)?.Replace('\\', '/');
            var folderName = Path.GetFileName(outputFolder);

            if (string.IsNullOrEmpty(parentFolder) || string.IsNullOrEmpty(folderName))
            {
                Debug.LogError($"[BattleUnitPrefabBuilder] Invalid output folder: {outputFolder}");
                return false;
            }

            if (!AssetDatabase.IsValidFolder(parentFolder))
            {
                Directory.CreateDirectory(parentFolder);
                AssetDatabase.Refresh();
            }

            if (!AssetDatabase.IsValidFolder(outputFolder))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }

            return AssetDatabase.IsValidFolder(outputFolder);
        }

        private void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parentFolder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var folderName = Path.GetFileName(assetPath);

            if (string.IsNullOrEmpty(parentFolder) || string.IsNullOrEmpty(folderName))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parentFolder))
            {
                EnsureFolder(parentFolder);
            }

            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        private string GetOutputPath(string prefabName)
        {
            var safeName = prefabName.EndsWith("_BattleUnit")
                ? prefabName
                : $"{prefabName}_BattleUnit";

            return $"{outputFolder}/{safeName}.prefab";
        }

        private bool IsDescendantNamed(Transform transform, string targetName)
        {
            if (transform == null || string.IsNullOrEmpty(targetName))
            {
                return false;
            }

            var current = transform;
            while (current != null)
            {
                if (string.Equals(current.name, targetName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}

#endif
