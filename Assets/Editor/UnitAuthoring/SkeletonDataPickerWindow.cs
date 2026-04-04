#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Battle.Editor
{
    internal sealed class SkeletonDataPickerWindow : EditorWindow
    {
        private const string FallbackFolderPath = "Assets";

        private readonly List<SkeletonDataAsset> skeletonAssets = new List<SkeletonDataAsset>();
        private readonly List<string> skeletonAssetPaths = new List<string>();
        private readonly Dictionary<SkeletonDataAsset, Texture2D> previewCache = new Dictionary<SkeletonDataAsset, Texture2D>();
        private readonly HashSet<Texture2D> generatedTextures = new HashSet<Texture2D>();

        private Action<SkeletonDataAsset> onPicked;
        private string folderPath = FallbackFolderPath;
        private string status = "Not loaded";
        private string searchFilter = string.Empty;
        private Vector2 listScroll;
        private int selectedIndex = -1;
        private Texture2D selectedPreviewTexture;

        public static void ShowPicker(
            string initialFolderPath,
            SkeletonDataAsset currentSelection,
            Action<SkeletonDataAsset> onPicked
        )
        {
            var window = CreateInstance<SkeletonDataPickerWindow>();
            window.titleContent = new GUIContent("Skeleton Picker");
            window.minSize = new Vector2(900f, 520f);
            window.folderPath = ResolveFolderPath(initialFolderPath);
            window.onPicked = onPicked;
            window.ShowUtility();
            window.ReloadSkeletonList();
            window.TrySelectAsset(currentSelection);
            window.Focus();
        }

        private void OnDisable()
        {
            foreach (Texture2D generatedTexture in generatedTextures)
            {
                if (generatedTexture != null)
                {
                    DestroyImmediate(generatedTexture);
                }
            }

            generatedTextures.Clear();
            previewCache.Clear();
            selectedPreviewTexture = null;
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            DrawListPane();
            EditorGUILayout.Space(8f);
            DrawPreviewPane();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);
            DrawBottomBar();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField($"Folder: {folderPath}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                ReloadSkeletonList();
            }

            if (GUILayout.Button("Ping Folder", EditorStyles.toolbarButton, GUILayout.Width(88f)))
            {
                UnityEngine.Object folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
                if (folderAsset != null)
                {
                    EditorGUIUtility.PingObject(folderAsset);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search", GUILayout.Width(48f));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("Clear", GUILayout.Width(56f)))
            {
                searchFilter = string.Empty;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(status, EditorStyles.miniLabel);
        }

        private void DrawListPane()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.58f));
            using (var scroll = new EditorGUILayout.ScrollViewScope(listScroll, GUILayout.ExpandHeight(true)))
            {
                listScroll = scroll.scrollPosition;

                List<int> filtered = BuildFilteredIndices();
                if (filtered.Count == 0)
                {
                    EditorGUILayout.LabelField("(No SkeletonDataAsset found for current filter)", EditorStyles.centeredGreyMiniLabel);
                    EditorGUILayout.EndVertical();
                    return;
                }

                const float rowHeight = 40f;
                for (int n = 0; n < filtered.Count; n++)
                {
                    int index = filtered[n];
                    SkeletonDataAsset asset = skeletonAssets[index];
                    string path = skeletonAssetPaths[index];
                    bool isSelected = index == selectedIndex;

                    Rect rowRect = GUILayoutUtility.GetRect(1f, rowHeight, GUILayout.ExpandWidth(true));
                    if (isSelected)
                    {
                        EditorGUI.DrawRect(rowRect, new Color(0.29f, 0.55f, 0.82f, 0.28f));
                    }
                    else if ((n & 1) == 0)
                    {
                        EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.03f));
                    }

                    if (!isSelected && rowRect.Contains(UnityEngine.Event.current.mousePosition))
                    {
                        EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.05f));
                    }

                    Texture icon = AssetDatabase.GetCachedIcon(path);
                    Rect iconRect = new Rect(rowRect.x + 6f, rowRect.y + 11f, 16f, 16f);
                    if (icon != null)
                    {
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                    }

                    float textLeft = iconRect.xMax + 8f;
                    Rect nameRect = new Rect(textLeft, rowRect.y + 4f, rowRect.width - (textLeft - rowRect.x) - 6f, 16f);
                    Rect pathRect = new Rect(textLeft, rowRect.y + 20f, rowRect.width - (textLeft - rowRect.x) - 6f, 14f);
                    GUI.Label(nameRect, asset.name, isSelected ? EditorStyles.boldLabel : EditorStyles.label);
                    GUI.Label(pathRect, new GUIContent(GetCompactPath(path), path), EditorStyles.miniLabel);

                    UnityEngine.Event evt = UnityEngine.Event.current;
                    if (evt.type == EventType.MouseDown && evt.button == 0 && rowRect.Contains(evt.mousePosition))
                    {
                        SetSelectedIndex(index);
                        if (evt.clickCount >= 2)
                        {
                            ConfirmSelectionAndClose();
                        }

                        evt.Use();
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewPane()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            SkeletonDataAsset selected = GetSelectedAsset();
            string selectedLabel = selected != null ? selected.name : "None";
            EditorGUILayout.LabelField($"Selected: {selectedLabel}", EditorStyles.boldLabel);
            EditorGUILayout.Space(6f);

            Rect previewRect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(previewRect, new Color(0.10f, 0.11f, 0.13f, 1f));
            if (selectedPreviewTexture != null)
            {
                GUI.DrawTexture(previewRect, selectedPreviewTexture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                GUI.Label(previewRect, "No preview", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBottomBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", GUILayout.Width(90f), GUILayout.Height(24f)))
            {
                Close();
            }

            using (new EditorGUI.DisabledScope(GetSelectedAsset() == null))
            {
                if (GUILayout.Button("Select", GUILayout.Width(110f), GUILayout.Height(24f)))
                {
                    ConfirmSelectionAndClose();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ReloadSkeletonList()
        {
            skeletonAssets.Clear();
            skeletonAssetPaths.Clear();
            selectedIndex = -1;
            selectedPreviewTexture = null;

            folderPath = ResolveFolderPath(folderPath);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                status = $"Invalid folder: {folderPath}";
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:SkeletonDataAsset", new[] { folderPath });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                SkeletonDataAsset asset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path);
                if (asset == null)
                {
                    continue;
                }

                skeletonAssets.Add(asset);
                skeletonAssetPaths.Add(path);
            }

            if (skeletonAssets.Count > 1)
            {
                var ordered = skeletonAssets
                    .Select((asset, index) => new { asset, path = skeletonAssetPaths[index] })
                    .OrderBy(x => x.asset.name, StringComparer.Ordinal)
                    .ToList();
                skeletonAssets.Clear();
                skeletonAssetPaths.Clear();
                for (int i = 0; i < ordered.Count; i++)
                {
                    skeletonAssets.Add(ordered[i].asset);
                    skeletonAssetPaths.Add(ordered[i].path);
                }
            }

            UnitAuthoringPrefabCacheState.instance.SaveSkeletonDataCache(skeletonAssetPaths);
            status = $"Ready ({skeletonAssets.Count})";
            if (skeletonAssets.Count > 0)
            {
                SetSelectedIndex(0);
            }
        }

        private void TrySelectAsset(SkeletonDataAsset target)
        {
            if (target == null)
            {
                return;
            }

            for (int i = 0; i < skeletonAssets.Count; i++)
            {
                if (skeletonAssets[i] == target)
                {
                    SetSelectedIndex(i);
                    return;
                }
            }
        }

        private void SetSelectedIndex(int index)
        {
            if (index < 0 || index >= skeletonAssets.Count)
            {
                selectedIndex = -1;
                selectedPreviewTexture = null;
                return;
            }

            if (selectedIndex == index && selectedPreviewTexture != null)
            {
                return;
            }

            selectedIndex = index;
            selectedPreviewTexture = TryGetPreviewTexture(skeletonAssets[index]);
            Repaint();
        }

        private SkeletonDataAsset GetSelectedAsset()
        {
            return selectedIndex >= 0 && selectedIndex < skeletonAssets.Count
                ? skeletonAssets[selectedIndex]
                : null;
        }

        private void ConfirmSelectionAndClose()
        {
            SkeletonDataAsset selected = GetSelectedAsset();
            onPicked?.Invoke(selected);
            Close();
        }

        private List<int> BuildFilteredIndices()
        {
            var result = new List<int>(skeletonAssets.Count);
            string filter = (searchFilter ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(filter))
            {
                for (int i = 0; i < skeletonAssets.Count; i++)
                {
                    result.Add(i);
                }

                return result;
            }

            for (int i = 0; i < skeletonAssets.Count; i++)
            {
                SkeletonDataAsset asset = skeletonAssets[i];
                string path = skeletonAssetPaths[i];
                if (
                    asset.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    result.Add(i);
                }
            }

            return result;
        }

        private Texture2D TryGetPreviewTexture(SkeletonDataAsset asset)
        {
            if (asset == null)
            {
                return null;
            }

            if (previewCache.TryGetValue(asset, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            Texture2D previewTexture = AssetPreview.GetAssetPreview(asset);
            Texture2D miniThumbnail = AssetPreview.GetMiniThumbnail(asset) as Texture2D;
            if (previewTexture != null && (miniThumbnail == null || previewTexture != miniThumbnail))
            {
                previewCache[asset] = previewTexture;
                return previewTexture;
            }

            previewTexture = RenderSkeletonPreviewTexture(asset, 320);
            if (previewTexture != null)
            {
                previewCache[asset] = previewTexture;
                return previewTexture;
            }

            if (miniThumbnail != null)
            {
                previewCache[asset] = miniThumbnail;
                return miniThumbnail;
            }

            return null;
        }

        private Texture2D RenderSkeletonPreviewTexture(SkeletonDataAsset asset, int size)
        {
            PreviewRenderUtility previewUtility = null;
            GameObject root = null;
            try
            {
                root = new GameObject("SkeletonDataPreviewRoot");
                root.hideFlags = HideFlags.HideAndDontSave;
                SkeletonAnimation skeletonAnimation = root.AddComponent<SkeletonAnimation>();
                skeletonAnimation.skeletonDataAsset = asset;
                skeletonAnimation.Initialize(false);

                if (skeletonAnimation.AnimationState != null && skeletonAnimation.Skeleton != null)
                {
                    Spine.Animation previewAnimation = skeletonAnimation.Skeleton.Data.FindAnimation("idle");
                    if (previewAnimation == null && skeletonAnimation.Skeleton.Data.Animations.Count > 0)
                    {
                        previewAnimation = skeletonAnimation.Skeleton.Data.Animations.Items[0];
                    }

                    if (previewAnimation != null)
                    {
                        skeletonAnimation.AnimationState.SetAnimation(0, previewAnimation, true);
                    }

                    skeletonAnimation.AnimationState.Update(0f);
                    skeletonAnimation.AnimationState.Apply(skeletonAnimation.Skeleton);
                    skeletonAnimation.Skeleton.UpdateWorldTransform();
                }

                skeletonAnimation.LateUpdate();

                if (!TryGetRendererBounds(root, out Bounds rendererBounds))
                {
                    return null;
                }

                previewUtility = new PreviewRenderUtility(true);
                previewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
                previewUtility.camera.backgroundColor = new Color(0.10f, 0.11f, 0.13f, 1f);
                previewUtility.camera.orthographic = true;
                previewUtility.lights[0].intensity = 1.0f;
                previewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
                previewUtility.lights[1].intensity = 0.72f;
                previewUtility.lights[1].transform.rotation = Quaternion.Euler(340f, 218f, 177f);
                previewUtility.ambientColor = new Color(0.45f, 0.45f, 0.45f, 1f);

                ConfigurePreviewCamera(previewUtility.camera, rendererBounds);
                previewUtility.AddSingleGO(root);
                previewUtility.BeginStaticPreview(new Rect(0f, 0f, size, size));
                previewUtility.camera.Render();
                Texture2D texture = previewUtility.EndStaticPreview();
                if (texture != null)
                {
                    generatedTextures.Add(texture);
                }

                return texture;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (root != null)
                {
                    DestroyImmediate(root);
                }

                if (previewUtility != null)
                {
                    previewUtility.Cleanup();
                }
            }
        }

        private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bool hasBounds = false;
            bounds = new Bounds(root.transform.position, Vector3.zero);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return hasBounds;
        }

        private static void ConfigurePreviewCamera(Camera camera, Bounds bounds)
        {
            if (camera == null)
            {
                return;
            }

            float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, 0.01f);
            camera.orthographicSize = Mathf.Max(0.5f, maxExtent * 1.3f);
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 10f);
            camera.transform.rotation = Quaternion.identity;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 200f;
        }

        private string GetCompactPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return "(No path)";
            }

            string normalized = fullPath.Replace('\\', '/');
            string folderPrefix = folderPath.Replace('\\', '/').TrimEnd('/') + "/";
            if (normalized.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return normalized.Substring(folderPrefix.Length);
            }

            const int maxLength = 64;
            if (normalized.Length <= maxLength)
            {
                return normalized;
            }

            return "..." + normalized.Substring(normalized.Length - (maxLength - 3));
        }

        private static string ResolveFolderPath(string candidatePath)
        {
            if (!string.IsNullOrEmpty(candidatePath) && AssetDatabase.IsValidFolder(candidatePath))
            {
                return candidatePath;
            }

            return FallbackFolderPath;
        }
    }
}

#endif
