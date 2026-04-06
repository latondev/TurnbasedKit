#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameSystems.Battle;
using GameSystems.Battle.Demo;
using GameSystems.Skills;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Battle.Editor
{
    public class UnitAuthoringWindow : EditorWindow
    {
        // "The Precise Architect" colors
        private static readonly Color BaseBackground = new Color(0.0745f, 0.0745f, 0.0745f, 1f); // #131313
        private static readonly Color SurfaceContainerLowest = new Color(0.0549f, 0.0549f, 0.0549f, 1f); // #0e0e0e
        private static readonly Color SurfaceContainerLow = new Color(0.1059f, 0.1059f, 0.1098f, 1f); // #1b1b1c
        private static readonly Color SurfaceContainerHigh = new Color(0.1647f, 0.1647f, 0.1647f, 1f); // #2a2a2a
        private static readonly Color SurfaceContainerHighest = new Color(0.2078f, 0.2078f, 0.2078f, 1f); // #353535
        private static readonly Color Primary = new Color(1f, 0.7137f, 0.5647f, 1f); // #ffb690
        private static readonly Color PrimaryContainer = new Color(0.9412f, 0.4431f, 0.1059f, 1f); // #f0711b
        private static readonly Color SecondaryContainer = new Color(0.2745f, 0.2863f, 0.2980f, 1f); // #46494c
        private static readonly Color Tertiary = new Color(0.4784f, 0.8157f, 1f, 1f); // #7ad0ff
        private static readonly Color TextOnSurface = new Color(0.898f, 0.8863f, 0.8824f, 1f); // #e5e2e1
        private static readonly Color TextOnSurfaceVariant = new Color(0.8745f, 0.7529f, 0.6941f, 1f); // #dfc0b1

        private static readonly Color PanelColor = SurfaceContainerLowest;
        private static readonly Color PanelAltColor = SurfaceContainerLowest;
        private static readonly Color AccentColor = Primary;
        private static readonly Color AccentSoftColor = new Color(Primary.r, Primary.g, Primary.b, 0.16f);
        
        private static readonly Color GoodColor = new Color(0.28f, 0.72f, 0.38f);
        private static readonly Color WarnColor = Tertiary; // Use Tertiary for logic/warnings (#7ad0ff)
        private static readonly Color BadColor = new Color(1f, 0.7059f, 0.6706f, 1f); // #ffb4ab (error)

        private static readonly GUIContent PreviewPlayContent = new GUIContent("▶", "Preview animation");

        private static Texture2D texSurfaceLowest;
        private static Texture2D texSurfaceLow;
        private static Texture2D texSurfaceHigh;
        private static Texture2D texSurfaceHighest;
        private static Texture2D texPrimaryGradient;
        private static Texture2D texSecondaryContainer;

        private static Texture2D iconAssetSetup;
        private static Texture2D iconCharacterData;
        private static Texture2D iconSkeletonData;
        private static Texture2D iconPrefabAuthoring;
        private static Texture2D iconSkillSequences;
        private static Texture2D iconSkillPreview;
        private static Texture2D[] tabIcons;

        [SerializeField] private CharacterDataSO characterData;
        [SerializeField] private GameObject prefabAsset;
        [SerializeField] private Vector2 scrollPosition;
        [SerializeField] private Vector2 skeletonDetailsScrollPosition;
        [SerializeField] private int currentTab = 0;
        [SerializeField] private SkillViewSequence selectedLibrarySequence;
        [SerializeField] private float previewPlaybackSpeed = 1f;
        [SerializeField] private bool previewSequenceLoop = false;
        [SerializeField] private GameObject previewTargetPrefab;
        [SerializeField] private bool previewTargetPrefabInitialized = false;
        private readonly string[] tabNames = { "Asset Setup", "Character Data", "Skeleton Data", "Prefab Authoring", "Skill sequences", "Skill Step Preview" };
        [SerializeField] private string skillSearchFilter = string.Empty;
        [SerializeField] private string animationSearchFilter = string.Empty;
        [SerializeField] private string eventSearchFilter = string.Empty;

        private GameObject workingPrefabRoot;
        private string workingPrefabPath;
        private SkeletonAnimation skeletonAnimation;
        private SkeletonDataAsset skeletonDataAsset;
        private ActionSequenceRunner actionSequenceRunner;
        private BehitBehavior behitBehavior;
        private AnimationHandle animationHandle;
        private UnitView unitView;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle sectionHeaderStyle;
        private GUIStyle sectionBodyStyle;
        private GUIStyle cardStyle;
        private GUIStyle chipStyle;
        private GUIStyle chipStyleSmall;
        private GUIStyle primaryButtonStyle;
        private GUIStyle secondaryButtonStyle;
        private GUIStyle dangerButtonStyle;
        private GUIStyle previewIconButtonStyle;
        private GUIStyle panelLabelStyle;
        private GUIStyle searchFieldStyle;
        private GUIStyle tabNormalStyle;
        private GUIStyle tabSelectedStyle;

        private struct AnimationInfo
        {
            public string Name;
            public float Duration;
            public string EventNames;
        }

        private readonly List<string> animationNames = new List<string>();
        private readonly List<AnimationInfo> animationInfos = new List<AnimationInfo>();
        private readonly Dictionary<string, string> animationUsageCache = new Dictionary<string, string>();
        private readonly List<string> eventNames = new List<string>();
        private const string DefaultUnitViewPrefabSearchFolder = "Assets/AssetGame/ArtWork/Prefab/BattleUnits";
        private const string DefaultCharacterDataSearchFolder = "Assets/SO";
        private const string DefaultSkeletonDataSearchFolder = "Assets";
        private const string UnitCreationTemplatePrefabPath = "Assets/AssetGame/ArtWork/Prefab/BattleUnits/1000_jing_wei.prefab";
        private const double UnitViewPrefabScanFrameBudgetSeconds = 0.004d;
        private const int UnitViewPrefabScanBatchSize = 20;
        private readonly List<GameObject> unitViewPrefabs = new List<GameObject>();
        private readonly Dictionary<int, CharacterDataSO> characterDataById = new Dictionary<int, CharacterDataSO>();
        private string[] unitViewPrefabLabels = Array.Empty<string>();
        private readonly Dictionary<GameObject, Texture2D> unitViewPreviewCache = new Dictionary<GameObject, Texture2D>();
        private readonly HashSet<Texture2D> unitViewGeneratedPreviewTextures = new HashSet<Texture2D>();
        private bool unitViewPrefabCacheDirty = true;
        private bool characterDataIndexDirty = true;
        private bool skeletonDataListDirty = true;
        [SerializeField] private string characterDataSearchFolderPath = DefaultCharacterDataSearchFolder;
        [SerializeField] private string prefabSearchFolderPath = DefaultUnitViewPrefabSearchFolder;
        [SerializeField] private string skeletonDataSearchFolderPath = DefaultSkeletonDataSearchFolder;
        private bool unitViewPrefabListLoaded;
        private bool isUnitViewPrefabScanRunning;
        private bool unitViewPrefabScanFullProject;
        private string[] unitViewPrefabScanGuids = Array.Empty<string>();
        private int unitViewPrefabScanIndex;
        private int unitViewPrefabScanTotal;
        private readonly List<string> scannedUnitViewPrefabPaths = new List<string>();
        private readonly HashSet<string> scannedUnitViewPrefabPathSet =
            new HashSet<string>(StringComparer.Ordinal);
        private string unitViewPrefabScanStatus = "Not loaded";
        private string unitViewAutoBindStatus = string.Empty;
        [SerializeField] private SkeletonDataAsset createSkeletonDataAsset;
        private readonly List<SkeletonDataAsset> skeletonDataFolderAssets = new List<SkeletonDataAsset>();
        private readonly List<string> skeletonDataFolderPaths = new List<string>();
        private bool skeletonDataListLoaded;
        private string skeletonDataListStatus = "Not loaded";
        [SerializeField] private Vector2 skeletonDataListScroll;
        [SerializeField] private int selectedSkeletonDataListIndex = -1;
        [SerializeField] private string skeletonDataListFilter = string.Empty;
        [SerializeField] private string createUnitAssetStatus = string.Empty;
        [SerializeField] private bool createUnitAssetStatusIsError;
        private Vector2 unitViewListScroll;
        [SerializeField] private UnitViewBrowserMode unitViewBrowserMode = UnitViewBrowserMode.List;
        [SerializeField] private int selectedUnitViewListIndex = -1;
        private SkillSequencePreviewController previewController;
        private SkillSequencePreviewController skeletonPreviewController;
        private GameObject previewBoundPrefab;
        private GameObject skeletonPreviewBoundPrefab;
        private SkillViewSequence previewBoundSequence;
        private SkillViewSequence previewOverrideSequence;
        private string previewOverrideLabel;
        [SerializeField] private int selectedPreviewActionIndex;
        [SerializeField] private bool actionsListExpanded = true;
        [SerializeField] private Vector2 sequenceTimelineScrollPosition;
        [SerializeField] private float sequenceTimelinePixelsPerSecond = 140f;
        [SerializeField] private SequenceTemplateArchetype selectedSequenceTemplate = SequenceTemplateArchetype.MeleeSingleHit;
        [SerializeField] private string sequenceTemplateAnimationName = "skill";
        [SerializeField] private bool sequenceTemplateOverwriteSequenceId;
        [SerializeField] private bool markerEditorShowWorldPosition = true;
        [SerializeField] private int selectedPreviewTimelineStepIndex = -1;
        [SerializeField] private bool showPreviewMarkerOverlay = true;
        [SerializeField] private bool previewMarkerEditMode;
        [SerializeField] private bool showPreviewStepDetails;
        [SerializeField] private string draggedPreviewMarkerName;
        [SerializeField] private float draggedPreviewMarkerWorldZ;
        private bool skeletonMetadataLoaded;

        private enum UnitViewBrowserMode
        {
            List,
            Detail
        }

        private static readonly string[] UnitViewBrowserModeLabels = { "List", "Detail" };

        private enum SequenceTemplateArchetype
        {
            MeleeSingleHit,
            MeleeMultiHit,
            RangedCast,
            AreaBurst,
            JumpBehindStrike,
            SummonPulse
        }

        private readonly struct MarkerDefinition
        {
            public MarkerDefinition(string name, Vector3 defaultLocalPosition)
            {
                Name = name;
                DefaultLocalPosition = defaultLocalPosition;
            }

            public string Name { get; }
            public Vector3 DefaultLocalPosition { get; }
        }

        private static readonly MarkerDefinition[] RequiredMarkers =
        {
            new MarkerDefinition("UIPos", new Vector3(0.1f, 1.45f, 0f)),
            new MarkerDefinition("FlyStart", new Vector3(0.26f, 0.78f, 0f)),
            new MarkerDefinition("PetPos", new Vector3(-0.42f, 0.74f, 0f)),
            new MarkerDefinition("BuffTop", new Vector3(0.09f, 1.37f, 0f)),
            new MarkerDefinition("BuffMiddle", new Vector3(0.09f, 0.67f, 0f)),
            new MarkerDefinition("BuffBottom", Vector3.zero),
        };

        [MenuItem("Tools/Battle/Unit Authoring")]
        public static void ShowWindow()
        {
            var window = GetWindow<UnitAuthoringWindow>("Unit Authoring");
            window.minSize = new Vector2(980f, 760f);
        }

        private void OnEnable()
        {
            EditorApplication.update -= HandleEditorUpdate;
            EditorApplication.update += HandleEditorUpdate;
            EditorApplication.projectChanged -= HandleProjectChanged;
            EditorApplication.projectChanged += HandleProjectChanged;

            if (previewController == null)
            {
                previewController = new SkillSequencePreviewController();
            }

            previewController.SetRepaintCallback(Repaint);
            previewController.Speed = previewPlaybackSpeed;

            if (skeletonPreviewController == null)
            {
                skeletonPreviewController = new SkillSequencePreviewController();
                skeletonPreviewController.ActorStartPosition = new Vector3(0f, -1.0f, 0f);
                skeletonPreviewController.PreviewCameraPosition = new Vector3(0f, 0.5f, -10f);
                skeletonPreviewController.PreviewCameraSize = 2.5f;
            }

            skeletonPreviewController.SetRepaintCallback(Repaint);
            skeletonPreviewController.Speed = previewPlaybackSpeed;

            EnsureDefaultPreviewTargetPrefab();
            RestoreUnitViewPrefabCache();
            RefreshSequenceLibrary();
        }

        private void OnDisable()
        {
            if (texSurfaceLowest != null) { DestroyImmediate(texSurfaceLowest); texSurfaceLowest = null; }
            if (texSurfaceLow != null) { DestroyImmediate(texSurfaceLow); texSurfaceLow = null; }
            if (texSurfaceHigh != null) { DestroyImmediate(texSurfaceHigh); texSurfaceHigh = null; }
            if (texSurfaceHighest != null) { DestroyImmediate(texSurfaceHighest); texSurfaceHighest = null; }
            if (texPrimaryGradient != null) { DestroyImmediate(texPrimaryGradient); texPrimaryGradient = null; }
            if (texSecondaryContainer != null) { DestroyImmediate(texSecondaryContainer); texSecondaryContainer = null; }

            EditorApplication.update -= HandleEditorUpdate;
            EditorApplication.projectChanged -= HandleProjectChanged;
            SkillViewStepDrawer.SetAnimationOptions(null);
            SkillViewAnimationEventDrawer.SetEventOptions(null);
            ClearPreviewAnimationOverride(false);
            if (skeletonPreviewController != null)
            {
                skeletonPreviewController.Dispose();
                skeletonPreviewController = null;
            }
            if (previewController != null)
            {
                previewController.Dispose();
                previewController = null;
            }

            draggedPreviewMarkerName = null;
            StopUnitViewPrefabScan();
            ClearUnitViewPreviewCache();

            UnloadPrefabWorkingCopy();
        }

        private void HandleEditorUpdate()
        {
            ProcessUnitViewPrefabScan();

            if (previewController != null)
            {
                bool shouldTickSkillPreview = currentTab == 5;
                if (!shouldTickSkillPreview)
                {
                    if (previewController.IsPlaying)
                    {
                        previewController.Pause();
                    }
                }

                if (shouldTickSkillPreview && (previewController.IsPlaying || previewController.HasPendingRestart))
                {
                    previewController.Tick();
                }
            }

            if (skeletonPreviewController != null)
            {
                bool shouldTickSkeletonPreview = currentTab == 2 && previewOverrideSequence != null;
                if (!shouldTickSkeletonPreview)
                {
                    if (skeletonPreviewController.IsPlaying)
                    {
                        skeletonPreviewController.Pause();
                    }
                }

                if (shouldTickSkeletonPreview && skeletonPreviewController.IsPlaying)
                {
                    skeletonPreviewController.Tick();
                }
            }
        }

        private void OnGUI()
        {
            BuildStyles();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), BaseBackground);
            SkillViewStepDrawer.SetAnimationOptions(animationNames);
            SkillViewAnimationEventDrawer.SetEventOptions(eventNames);
            DrawHeader();

            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();

            DrawSidebar();

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical();
            bool useMainScroll = currentTab != 2;
            if (useMainScroll)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            }

            switch (currentTab)
            {
                case 0: DrawAssetBindingTab(); break;
                case 1: DrawCharacterSection(); break;
                case 2: DrawSkeletonSection(); break;
                case 3: DrawPrefabSection(); break;
                case 4: DrawSkillSequencesSection(); break;
                case 5: DrawSkillSequencePreviewTab(); break;
            }

            if (useMainScroll)
            {
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            if (currentTab != 2)
            {
                EditorGUILayout.Space(8f);
                DrawBottomActions();
            }
        }

        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(cardStyle, GUILayout.Width(180f), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("Menu", sectionHeaderStyle);
            EditorGUILayout.Space(8f);

            for (int i = 0; i < tabNames.Length; i++)
            {
                bool isSelected = currentTab == i;
                
                Rect rect = EditorGUILayout.GetControlRect(false, 32f);
                bool isHover = rect.Contains(UnityEngine.Event.current.mousePosition);
                if (isSelected)
                {
                    EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.12f));
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), AccentColor);
                }
                else if (isHover)
                {
                    EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.05f));
                }

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    currentTab = i;
                    GUI.FocusControl(null);
                }

                GUIStyle labelStyle = isSelected ? tabSelectedStyle : tabNormalStyle;
                
                float textOffset = isSelected ? 14f : 12f;
                if (tabIcons != null && i < tabIcons.Length && tabIcons[i] != null)
                {
                    Rect iconRect = new Rect(rect.x + textOffset, rect.y + 8f, 16f, 16f);
                    Color oldColor = GUI.color;
                    GUI.color = isSelected ? Primary : TextOnSurfaceVariant;
                    GUI.DrawTexture(iconRect, tabIcons[i], ScaleMode.ScaleToFit);
                    GUI.color = oldColor;
                    textOffset += 24f; // Shift text right
                }

                Rect textRect = new Rect(rect.x + textOffset, rect.y, rect.width - textOffset, rect.height);
                GUI.Label(textRect, tabNames[i], labelStyle);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("Unit Authoring", titleStyle);
            EditorGUILayout.LabelField(
                "Bind one CharacterDataSO and one prefab to the same id, edit the SO inline, and configure Spine animation/event names from the prefab's SkeletonAnimation.",
                subtitleStyle);

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            DrawStatusChip("SO", characterData != null ? $"#{characterData.id}" : "None", characterData != null ? GoodColor : BadColor);
            DrawStatusChip("Prefab", prefabAsset != null ? prefabAsset.name : "None", prefabAsset != null ? GoodColor : BadColor);
            DrawStatusChip("Skeleton", skeletonDataAsset != null ? skeletonDataAsset.name : "None", skeletonDataAsset != null ? GoodColor : WarnColor);
            DrawStatusChip("Animations", animationNames.Count.ToString(), animationNames.Count > 0 ? GoodColor : WarnColor);
            DrawStatusChip("Events", eventNames.Count.ToString(), eventNames.Count > 0 ? GoodColor : WarnColor);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8f);
        }

        private void DrawAssetBindingTab()
        {
            // ── TOP ROW: Asset Fields + Actions (left) | Preview (center) | UnitView List (right) ──
            EditorGUILayout.BeginHorizontal();

            // ── LEFT COLUMN: Binding Form ──
            EditorGUILayout.BeginVertical(cardStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Asset Binding", sectionHeaderStyle);
            EditorGUILayout.Space(8f);

            EditorGUI.BeginChangeCheck();
            var nextCharacterData = (CharacterDataSO)EditorGUILayout.ObjectField("Character Data SO", characterData, typeof(CharacterDataSO), false);
            if (EditorGUI.EndChangeCheck()) characterData = nextCharacterData;

            EditorGUI.BeginChangeCheck();
            var nextPrefabAsset = (GameObject)EditorGUILayout.ObjectField("Prefab Asset", prefabAsset, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck()) SetPrefabAsset(nextPrefabAsset);

            EditorGUI.BeginChangeCheck();
            DefaultAsset skeletonFolderAsset = GetFolderAsset(skeletonDataSearchFolderPath);
            var nextSkeletonFolderAsset = (DefaultAsset)EditorGUILayout.ObjectField(
                "Skeleton Folder",
                skeletonFolderAsset,
                typeof(DefaultAsset),
                false
            );
            if (EditorGUI.EndChangeCheck())
            {
                SetSkeletonDataSearchFolder(nextSkeletonFolderAsset);
            }

            EditorGUI.BeginChangeCheck();
            var nextCreateSkeletonData = (SkeletonDataAsset)EditorGUILayout.ObjectField(
                "Skeleton Data Asset",
                createSkeletonDataAsset,
                typeof(SkeletonDataAsset),
                false
            );
            if (EditorGUI.EndChangeCheck())
            {
                createSkeletonDataAsset = nextCreateSkeletonData;
                SyncSkeletonDataSelection();
                createUnitAssetStatus = string.Empty;
                createUnitAssetStatusIsError = false;
            }

            DrawSkeletonDataPickerControls();

            EditorGUILayout.Space(8f);
            Rect separatorRect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(separatorRect, new Color(1f, 1f, 1f, 0.06f));
            EditorGUILayout.Space(8f);

            bool canCreateUnitAssets = TryValidateCreateInputs(
                createSkeletonDataAsset,
                out string createValidationMessage,
                out _,
                out _,
                out _
            );
            using (new EditorGUI.DisabledScope(!canCreateUnitAssets))
            {
                if (GUILayout.Button("Create Prefab + SO", primaryButtonStyle, GUILayout.Height(28f)))
                {
                    TryCreateUnitAssetsFromSkeleton(createSkeletonDataAsset);
                }
            }

            if (!canCreateUnitAssets && !string.IsNullOrEmpty(createValidationMessage))
            {
                EditorGUILayout.LabelField(createValidationMessage, EditorStyles.miniLabel);
            }

            if (!string.IsNullOrEmpty(createUnitAssetStatus))
            {
                EditorGUILayout.HelpBox(
                    createUnitAssetStatus,
                    createUnitAssetStatusIsError ? MessageType.Warning : MessageType.Info
                );
            }

            EditorGUILayout.Space(8f);
            Rect separatorRect2 = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(separatorRect2, new Color(1f, 1f, 1f, 0.06f));
            EditorGUILayout.Space(6f);

            string idText = characterData != null ? characterData.id.ToString() : "—";
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("ID", EditorStyles.miniLabel, GUILayout.Width(20f));
            GUILayout.Label(idText, EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6f);

            // ── CENTER COLUMN: Unit Preview ──
            EditorGUILayout.BeginVertical(cardStyle, GUILayout.Width(300f), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("Unit Preview", sectionHeaderStyle);
            EditorGUILayout.Space(4f);

            if (prefabAsset != null && skeletonAnimation != null)
            {
                EnsurePreviewController();
                skeletonPreviewController.Pause();

                float availW = 280f;
                float availH = Mathf.Max(200f, position.height - 380f);
                float previewSize = Mathf.Min(availW, availH);

                Rect containerRect = EditorGUILayout.GetControlRect(false, previewSize + 8f);
                float cx = containerRect.x + (containerRect.width - previewSize) * 0.5f;
                float cy = containerRect.y + (containerRect.height - previewSize) * 0.5f;
                Rect centeredRect = new Rect(cx, cy, previewSize, previewSize);

                EditorGUI.DrawRect(centeredRect, new Color(0.10f, 0.11f, 0.13f, 1f));
                skeletonPreviewController.DrawPreview(centeredRect);
            }
            else
            {
                float emptyH = Mathf.Max(120f, position.height - 420f);
                Rect emptyRect = EditorGUILayout.GetControlRect(false, emptyH);
                EditorGUI.DrawRect(emptyRect, new Color(0.10f, 0.11f, 0.13f, 1f));
                string emptyLabel = prefabAsset == null
                    ? "No Prefab Selected"
                    : workingPrefabRoot == null
                        ? "Preview not loaded"
                        : "No Spine Skeleton";
                GUI.Label(emptyRect, emptyLabel, EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6f);

            // ── RIGHT COLUMN: UnitView Browser ──
            EditorGUILayout.BeginVertical(cardStyle, GUILayout.Width(280f), GUILayout.ExpandHeight(true));
            DrawUnitViewPrefabList();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);
        }

        private void DrawSkeletonSection()
        {
            EnsureWorkingPrefabLoaded(true);

            EditorGUILayout.BeginVertical(cardStyle);
            try
            {
                EditorGUILayout.LabelField("Skeleton Data", sectionHeaderStyle);
                EditorGUILayout.Space(4f);
                if (skeletonAnimation == null)
                {
                    EditorGUILayout.HelpBox("No SkeletonAnimation found in the selected prefab.", MessageType.Warning);
                    return;
                }

                EnsurePreviewController();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                string skeletonDataAssetName = skeletonDataAsset != null ? skeletonDataAsset.name : "None";
                EditorGUILayout.LabelField($"Skeleton Host: {skeletonAnimation.name}", sectionHeaderStyle);
                DrawKeyValueRow("Skeleton Data", skeletonDataAssetName);
                DrawKeyValueRow("Animation Count", animationNames.Count.ToString());
                DrawKeyValueRow("Event Count", eventNames.Count.ToString());
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(8f);

                float previewWidth = Mathf.Clamp(position.width * 0.33f, 300f, 420f);
                EditorGUILayout.BeginVertical(GUILayout.Width(previewWidth));
                DrawSkeletonAnimationPreviewPanel();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(6f);

                EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
                skeletonDetailsScrollPosition = EditorGUILayout.BeginScrollView(skeletonDetailsScrollPosition, GUILayout.ExpandHeight(true));

                EditorGUILayout.LabelField("Animations Detail", sectionHeaderStyle);
                EditorGUILayout.Space(2f);

                BuildAnimationUsageCache();

                Rect listRect = EditorGUILayout.BeginVertical(sectionBodyStyle);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Animation Name", EditorStyles.miniBoldLabel, GUILayout.Width(160f));
                EditorGUILayout.LabelField("Duration", EditorStyles.miniBoldLabel, GUILayout.Width(60f));
                EditorGUILayout.LabelField("Events", EditorStyles.miniBoldLabel, GUILayout.Width(120f));
                EditorGUILayout.LabelField("Usage Info", EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField("Play", EditorStyles.miniBoldLabel, GUILayout.Width(38f));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2f);

                EditorGUI.DrawRect(new Rect(listRect.x, listRect.y + 24f, listRect.width, 1f), new Color(1f, 1f, 1f, 0.1f));

                for (int i = 0; i < animationInfos.Count; i++)
                {
                    var anim = animationInfos[i];
                    string usageList = animationUsageCache.TryGetValue(anim.Name, out var useStr) ? useStr : "-";

                    Rect rowRect = EditorGUILayout.GetControlRect(false, 20f);
                    if (i % 2 == 0)
                    {
                        EditorGUI.DrawRect(rowRect, new Color(0, 0, 0, 0.15f));
                    }

                    Rect nameRect = new Rect(rowRect.x + 4f, rowRect.y + 2f, 156f, 18f);
                    Rect durRect = new Rect(rowRect.x + 164f, rowRect.y + 2f, 60f, 18f);
                    Rect eventRect = new Rect(rowRect.x + 228f, rowRect.y + 2f, 116f, 18f);
                    Rect previewRect = new Rect(rowRect.xMax - 28f, rowRect.y + 1f, 24f, 18f);
                    Rect useRect = new Rect(rowRect.x + 348f, rowRect.y + 2f, Mathf.Max(32f, previewRect.x - (rowRect.x + 348f) - 8f), 18f);

                    GUI.Label(nameRect, anim.Name, EditorStyles.miniLabel);
                    GUI.Label(durRect, $"{anim.Duration:F2}s", EditorStyles.miniLabel);

                    GUIStyle eventStyle = new GUIStyle(EditorStyles.miniLabel);
                    if (anim.EventNames == "-") eventStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                    else eventStyle.normal.textColor = new Color(0.9f, 0.7f, 0.2f);
                    GUI.Label(eventRect, anim.EventNames, eventStyle);

                    GUIStyle useStyle = new GUIStyle(EditorStyles.miniLabel);
                    if (usageList == "-") useStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                    else useStyle.normal.textColor = new Color(0.7f, 0.9f, 0.7f);

                    GUI.Label(useRect, usageList, useStyle);

                    bool previousEnabled = GUI.enabled;
                    GUI.enabled = skeletonAnimation != null && !string.IsNullOrWhiteSpace(anim.Name);
                    if (GUI.Button(previewRect, PreviewPlayContent, previewIconButtonStyle))
                    {
                        PreviewSkeletonAnimation(anim.Name, anim.Duration);
                    }
                    GUI.enabled = previousEnabled;
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(8f);
                DrawPreviewList("Event Preview", eventNames, 20);

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4f);
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void BuildAnimationUsageCache()
        {
            animationUsageCache.Clear();
            if (animationInfos.Count == 0) return;

            var behitSo = behitBehavior != null ? new SerializedObject(behitBehavior) : null;

            SerializedObject charSo = null;
            if (characterData != null) charSo = new SerializedObject(characterData);

            List<SerializedObject> seqBasicSOs = GetSeqSOs(charSo, "skillBasic");
            List<SerializedObject> seqUltiSOs = GetSeqSOs(charSo, "skillUltimate");
            List<SerializedObject> seqPassSOs = GetSeqSOs(charSo, "skillPassive");
            List<SerializedObject> seqAwakSOs = GetSeqSOs(charSo, "skillAwaken");
            List<SerializedObject> actionSeqSOs = GetActionSeqSOs(charSo);

            foreach (var anim in animationInfos)
            {
                var usages = new List<string>();

                if (behitSo != null)
                {
                    CheckProp(usages, anim.Name, behitSo, "behitAnimation", "BehitBehavior(Behit)");
                    CheckProp(usages, anim.Name, behitSo, "dieAnimation", "BehitBehavior(Die)");
                    CheckProp(usages, anim.Name, behitSo, "idleAnimation", "BehitBehavior(Idle)");
                }

                CheckSeqAnimList(usages, anim.Name, seqBasicSOs, "Basic Skill");
                CheckSeqAnimList(usages, anim.Name, seqUltiSOs, "Ultimate Skill");
                CheckSeqAnimList(usages, anim.Name, seqPassSOs, "Passive Skill");
                CheckSeqAnimList(usages, anim.Name, seqAwakSOs, "Awaken Skill");
                CheckSeqAnimList(usages, anim.Name, actionSeqSOs, "Action Data");

                animationUsageCache[anim.Name] = usages.Count == 0 ? "-" : string.Join(", ", usages);
            }
        }

        private List<SerializedObject> GetSeqSOs(SerializedObject charSo, string propertyName)
        {
            var list = new List<SerializedObject>();
            if (charSo == null) return list;
            var prop = charSo.FindProperty(propertyName);
            if (prop == null) return list;

            var stepSelectionsProp = prop.FindPropertyRelative("stepSelections");
            if (stepSelectionsProp != null)
            {
                AddSelectedSequences(list, stepSelectionsProp);
                if (list.Count > 0)
                {
                    return list;
                }
            }

            var legacySeqsProp = prop.FindPropertyRelative("legacyStepSequences");
            if (legacySeqsProp == null)
            {
                legacySeqsProp = prop.FindPropertyRelative("stepSkills");
            }

            if (legacySeqsProp != null)
            {
                for (int i = 0; i < legacySeqsProp.arraySize; i++)
                {
                    if (legacySeqsProp.GetArrayElementAtIndex(i).objectReferenceValue is SkillViewSequence seq)
                    {
                        AddUniqueSequence(list, seq);
                    }
                }
            }

            return list;
        }

        private List<SerializedObject> GetActionSeqSOs(SerializedObject charSo)
        {
            var list = new List<SerializedObject>();
            if (charSo == null)
            {
                return list;
            }

            var actionsProp = charSo.FindProperty("actions");
            if (actionsProp == null || !actionsProp.isArray)
            {
                return list;
            }

            for (int i = 0; i < actionsProp.arraySize; i++)
            {
                var actionProp = actionsProp.GetArrayElementAtIndex(i);
                if (actionProp == null)
                {
                    continue;
                }

                var stepSelectionsProp = actionProp.FindPropertyRelative("stepSelections");
                if (stepSelectionsProp != null)
                {
                    AddSelectedSequences(list, stepSelectionsProp);
                }

                var viewSequenceProp = actionProp.FindPropertyRelative("viewSequence");
                if (
                    viewSequenceProp != null
                    && viewSequenceProp.objectReferenceValue is SkillViewSequence sequence
                )
                {
                    AddUniqueSequence(list, sequence);
                }
            }

            return list;
        }

        private void AddSelectedSequences(List<SerializedObject> list, SerializedProperty stepSelectionsProp)
        {
            for (int i = 0; i < stepSelectionsProp.arraySize; i++)
            {
                var element = stepSelectionsProp.GetArrayElementAtIndex(i);
                var sequenceProp = element.FindPropertyRelative("sequence");
                if (sequenceProp != null && sequenceProp.objectReferenceValue is SkillViewSequence seq)
                {
                    AddUniqueSequence(list, seq);
                }
            }
        }

        private void AddUniqueSequence(List<SerializedObject> list, SkillViewSequence sequence)
        {
            if (sequence == null)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].targetObject == sequence)
                {
                    return;
                }
            }

            list.Add(new SerializedObject(sequence));
        }

        private void CheckProp(List<string> usages, string animName, SerializedObject so, string propName, string label)
        {
            var p = so.FindProperty(propName);
            if (p != null && p.stringValue == animName) usages.Add(label);
        }

        private void CheckSeqAnimList(List<string> usages, string animName, List<SerializedObject> seqSOs, string label)
        {
            if (seqSOs == null || seqSOs.Count == 0) return;
            foreach (var seqSo in seqSOs)
            {
                var animProp = seqSo.FindProperty("animationName");
                if (animProp != null && animProp.stringValue == animName && !usages.Contains(label)) usages.Add($"{label}");

                var fallProp = seqSo.FindProperty("fallbackAnimationName");
                if (fallProp != null && fallProp.stringValue == animName && !usages.Contains($"{label}(Fallback)")) usages.Add($"{label}(Fallback)");
                
                var idleProp = seqSo.FindProperty("idleAnimationName");
                if (idleProp != null && idleProp.stringValue == animName && !usages.Contains($"{label}(Idle)")) usages.Add($"{label}(Idle)");
            }
        }

        private void DrawCharacterSection()
        {
            EnsureWorkingPrefabLoaded(true);

            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("Character Data", sectionHeaderStyle);
            EditorGUILayout.LabelField("Edit stats, identity, and combat actions directly from the linked SO.", subtitleStyle);
            EditorGUILayout.Space(4f);
            if (characterData == null)
            {
                EditorGUILayout.HelpBox("Assign a CharacterDataSO to edit unit data.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            if (characterData.Actions == null || characterData.Actions.Count == 0)
            {
                characterData.EnsureActionsData();
            }
            var characterSo = new SerializedObject(characterData);
            characterSo.Update();

            EditorGUILayout.BeginVertical(sectionBodyStyle);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            DrawCharacterProperty(characterSo, "id", "Id");
            DrawCharacterProperty(characterSo, "nameHero", "Name");
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginVertical();
            DrawCharacterProperty(characterSo, "level", "Level");
            DrawCharacterProperty(characterSo, "type", "Type");
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginVertical();
            DrawCharacterProperty(characterSo, "isUnlock", "Unlocked");
            DrawCharacterProperty(characterSo, "rarity", "Rarity");
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(sectionBodyStyle);
            DrawCharacterProperty(characterSo, "stats", "Stats");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Combat Actions", sectionHeaderStyle);
            EditorGUILayout.BeginVertical(sectionBodyStyle);
            DrawCombatActionsProperty(characterSo, "actions", "Actions");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Legacy Skill Slots (Compatibility)", sectionHeaderStyle);
            EditorGUILayout.BeginVertical(sectionBodyStyle);
            DrawSkillProperty(characterSo, "skillBasic", "Basic Skill");
            DrawSkillProperty(characterSo, "skillUltimate", "Ultimate Skill");
            DrawSkillProperty(characterSo, "skillPassive", "Passive Skill");
            DrawSkillProperty(characterSo, "skillAwaken", "Awaken Skill");
            EditorGUILayout.EndVertical();

            if (characterSo.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(characterData);
                SyncPreviewSkillSelectionFromCharacterData();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Initialize Default Stats", secondaryButtonStyle))
            {
                Undo.RecordObject(characterData, "Initialize Default Stats");
                characterData.InitializeDefaultStats();
                EditorUtility.SetDirty(characterData);
            }

            if (GUILayout.Button("Ping SO", secondaryButtonStyle))
            {
                EditorGUIUtility.PingObject(characterData);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private void DrawPrefabSection()
        {
            EnsureWorkingPrefabLoaded(true);

            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("Prefab Authoring", sectionHeaderStyle);
            EditorGUILayout.LabelField("Edit the prefab's working copy, then save it back to the asset.", subtitleStyle);
            EditorGUILayout.Space(4f);
            if (workingPrefabRoot == null)
            {
                EditorGUILayout.HelpBox("Assign a prefab asset to edit its working copy.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField($"Working Prefab: {workingPrefabRoot.name}", sectionHeaderStyle);
            DrawKeyValueRow("Path", workingPrefabPath);
            DrawKeyValueRow("Root View", unitView != null ? unitView.name : "Missing");

            DrawComponentStringSection();
            DrawComponentOrderSection();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        // Global Sequence Editor State
        private Vector2 sequenceListScrollPosition;
        private List<SkillViewSequence> allLibrarySequences = new List<SkillViewSequence>();

        private void RefreshSequenceLibrary()
        {
            allLibrarySequences.Clear();
            string[] guids = AssetDatabase.FindAssets("t:SkillViewSequence");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var seq = AssetDatabase.LoadAssetAtPath<SkillViewSequence>(path);
                if (seq != null) allLibrarySequences.Add(seq);
            }
            allLibrarySequences.Sort((a,b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            if (allLibrarySequences.Count > 0)
            {
                if (selectedLibrarySequence == null || !allLibrarySequences.Contains(selectedLibrarySequence))
                {
                    selectedLibrarySequence = allLibrarySequences[0];
                }
            }
        }

        private void CreateNewLibrarySequence()
        {
            string folderPath = "Assets/Data/SkillSequences";
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/SkillSequences")) AssetDatabase.CreateFolder("Assets/Data", "SkillSequences");

            string path = EditorUtility.SaveFilePanelInProject(
                "Create Skill View Sequence",
                "NewSkillSequence",
                "asset",
                "Create new global sequence",
                folderPath);

            if (!string.IsNullOrEmpty(path))
            {
                var sequence = CreateInstance<SkillViewSequence>();
                sequence.SetSequenceId("new_skill_sequence");
                AssetDatabase.CreateAsset(sequence, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                RefreshSequenceLibrary();
                selectedLibrarySequence = sequence;
            }
        }

        private void DrawSkillSequencesSection()
        {
            EnsureWorkingPrefabLoaded(true);

            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("Global Sequence Library", sectionHeaderStyle);
            EditorGUILayout.LabelField("Manage all SkillViewSequence assets across the project here.", subtitleStyle);
            EditorGUILayout.Space(8f);

            if (allLibrarySequences.Count == 0 && !Application.isPlaying)
            {
                RefreshSequenceLibrary();
            }

            EditorGUILayout.BeginHorizontal();
            
            // Left panel: List
            EditorGUILayout.BeginVertical(sectionBodyStyle, GUILayout.Width(250f));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sequences", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(60f)))
            {
                RefreshSequenceLibrary();
            }
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(24f)))
            {
                CreateNewLibrarySequence();
            }
            EditorGUILayout.EndHorizontal();

            skillSearchFilter = DrawSearchField("", skillSearchFilter);
            
            EditorGUILayout.Space(4f);
            sequenceListScrollPosition = EditorGUILayout.BeginScrollView(sequenceListScrollPosition);
            
            foreach (var seq in allLibrarySequences)
            {
                if (seq == null) continue;
                if (!string.IsNullOrEmpty(skillSearchFilter) && seq.name.IndexOf(skillSearchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                bool isSelected = selectedLibrarySequence == seq;
                Rect rect = EditorGUILayout.GetControlRect(false, 28f);
                if (isSelected)
                {
                    EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.1f));
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), AccentColor);
                }
                
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    selectedLibrarySequence = seq;
                    GUI.FocusControl(null);
                }
                
                Rect iconRect = new Rect(rect.x + 8f, rect.y + 6f, 16f, 16f);
                GUI.DrawTexture(iconRect, EditorGUIUtility.IconContent("ScriptableObject Icon").image);
                
                Rect textRect = new Rect(iconRect.xMax + 8f, rect.y, rect.width - 32f, rect.height);
                string displayName = !string.IsNullOrEmpty(seq.SequenceId) ? seq.SequenceId : seq.name;
                GUI.Label(textRect, displayName, EditorStyles.label);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Right panel: Editor
            EditorGUILayout.BeginVertical(sectionBodyStyle);
            
            if (selectedLibrarySequence != null)
            {
                EditorGUILayout.BeginHorizontal();
                string displayTitle = !string.IsNullOrEmpty(selectedLibrarySequence.SequenceId) ? selectedLibrarySequence.SequenceId : selectedLibrarySequence.name;
                EditorGUILayout.LabelField(displayTitle, EditorStyles.boldLabel);
                if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(60f)))
                {
                    EditorGUIUtility.PingObject(selectedLibrarySequence);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(4f);
                
                // Animation selection filtering inside the sequence
                animationSearchFilter = DrawSearchField("Animation Filter", animationSearchFilter);
                eventSearchFilter = DrawSearchField("Event Filter", eventSearchFilter);
                
                DrawSequenceInline(selectedLibrarySequence, selectedLibrarySequence.name);
            }
            else
            {
                EditorGUILayout.HelpBox("Select a sequence from the left to edit its steps.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawBottomActions()
        {
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("Actions", sectionHeaderStyle);
            EditorGUILayout.LabelField("Save the working prefab first, then sync asset names and metadata.", subtitleStyle);
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = workingPrefabRoot != null;
            if (GUILayout.Button("Save Prefab", primaryButtonStyle, GUILayout.Height(30f)))
            {
                SavePrefabWorkingCopy();
            }

            GUI.enabled = characterData != null || workingPrefabRoot != null;
            if (GUILayout.Button("Save All", primaryButtonStyle, GUILayout.Height(30f)))
            {
                SaveAll();
            }

            GUI.enabled = true;
            if (GUILayout.Button("Close Prefab", dangerButtonStyle, GUILayout.Height(30f)))
            {
                UnloadPrefabWorkingCopy();
                prefabAsset = null;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawCharacterProperty(SerializedObject serializedObject, string propertyName, string label)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label), true);
            }
        }

        private void DrawCombatActionsProperty(
            SerializedObject serializedObject,
            string propertyName,
            string label
        )
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.isExpanded = actionsListExpanded;
            EditorGUILayout.PropertyField(property, new GUIContent(label), true);
            actionsListExpanded = property.isExpanded;
        }

        private void DrawSkillProperty(SerializedObject serializedObject, string propertyName, string label)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label), true);
            }
        }



        private void DrawSequenceInline(SkillViewSequence sequence, string title)
        {
            if (sequence == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical(sectionBodyStyle);
            EditorGUILayout.LabelField($"Sequence: {title}", sectionHeaderStyle);

            var sequenceSo = new SerializedObject(sequence);
            sequenceSo.Update();
            bool templateApplied = false;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Animation", sectionHeaderStyle);
            DrawPopupString(sequenceSo.FindProperty("sequenceId"), "Sequence Id", null);
            DrawPopupString(sequenceSo.FindProperty("animationName"), "Animation", animationNames, animationSearchFilter);
            DrawPopupString(sequenceSo.FindProperty("fallbackAnimationName"), "Fallback", animationNames, animationSearchFilter);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8f);

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Events", sectionHeaderStyle);
            DrawPopupString(sequenceSo.FindProperty("hitEventName"), "Hit Event", eventNames, eventSearchFilter);
            DrawPopupString(sequenceSo.FindProperty("falldownEventName"), "Falldown Event", eventNames, eventSearchFilter);
            DrawPopupString(sequenceSo.FindProperty("idleAnimationName"), "Idle Animation", animationNames, animationSearchFilter);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            templateApplied = DrawSequenceTemplateSection(sequence) || templateApplied;
            if (templateApplied)
            {
                sequenceSo.Update();
            }

            var stepsProp = sequenceSo.FindProperty("steps");
            if (stepsProp != null)
            {
                EditorGUILayout.PropertyField(stepsProp, true);
            }

            bool modified = sequenceSo.ApplyModifiedProperties();
            if (modified || templateApplied)
            {
                EditorUtility.SetDirty(sequence);
                if (previewController != null && previewController.Sequence == sequence)
                {
                    previewController.Restart();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private bool DrawSequenceTemplateSection(SkillViewSequence sequence)
        {
            if (sequence == null)
            {
                return false;
            }

            bool applied = false;

            EditorGUILayout.BeginVertical(sectionBodyStyle);
            EditorGUILayout.LabelField("Archetype Template", sectionHeaderStyle);

            selectedSequenceTemplate = (SequenceTemplateArchetype)EditorGUILayout.EnumPopup(
                "Template",
                selectedSequenceTemplate);

            sequenceTemplateAnimationName = DrawTemplateAnimationField(
                "Animation",
                sequenceTemplateAnimationName);

            sequenceTemplateOverwriteSequenceId = EditorGUILayout.ToggleLeft(
                "Overwrite sequence id",
                sequenceTemplateOverwriteSequenceId);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Template", primaryButtonStyle, GUILayout.Height(24f)))
            {
                ApplyTemplateToSequence(
                    sequence,
                    selectedSequenceTemplate,
                    sequenceTemplateAnimationName,
                    sequenceTemplateOverwriteSequenceId);
                applied = true;
            }

            if (GUILayout.Button("Use Sequence Animation", secondaryButtonStyle, GUILayout.Height(24f)))
            {
                sequenceTemplateAnimationName = sequence.AnimationName;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            return applied;
        }

        private string DrawTemplateAnimationField(string label, string currentValue)
        {
            if (animationNames == null || animationNames.Count == 0)
            {
                return EditorGUILayout.TextField(label, string.IsNullOrWhiteSpace(currentValue) ? "skill" : currentValue);
            }

            List<string> options = new List<string>();
            BuildFilteredOptions(options, animationNames, animationSearchFilter);
            if (options.Count == 0)
            {
                BuildFilteredOptions(options, animationNames, string.Empty);
            }

            if (options.Count == 0)
            {
                return EditorGUILayout.TextField(label, string.IsNullOrWhiteSpace(currentValue) ? "skill" : currentValue);
            }

            string safeValue = string.IsNullOrWhiteSpace(currentValue) ? options[0] : currentValue;
            if (!options.Contains(safeValue))
            {
                safeValue = options[0];
            }

            int currentIndex = options.IndexOf(safeValue);
            int nextIndex = EditorGUILayout.Popup(label, currentIndex, options.ToArray());
            if (nextIndex < 0 || nextIndex >= options.Count)
            {
                return safeValue;
            }

            return options[nextIndex];
        }

        private void ApplyTemplateToSequence(
            SkillViewSequence sequence,
            SequenceTemplateArchetype template,
            string animationName,
            bool overwriteSequenceId)
        {
            if (sequence == null)
            {
                return;
            }

            string safeAnimationName = string.IsNullOrWhiteSpace(animationName)
                ? "skill"
                : animationName.Trim();

            Undo.RecordObject(sequence, $"Apply {template} Template");
            switch (template)
            {
                case SequenceTemplateArchetype.MeleeSingleHit:
                    sequence.ApplyBasicStrikePreset(safeAnimationName);
                    break;
                case SequenceTemplateArchetype.MeleeMultiHit:
                    sequence.SetRuntimeSteps(BuildMeleeMultiHitTemplateSteps(safeAnimationName));
                    sequence.SetMetadata(safeAnimationName, safeAnimationName, "hit", "falldown", "idle");
                    break;
                case SequenceTemplateArchetype.RangedCast:
                    sequence.ApplyStationaryCastPreset(safeAnimationName);
                    break;
                case SequenceTemplateArchetype.AreaBurst:
                    sequence.ApplyAreaBurstPreset(safeAnimationName);
                    break;
                case SequenceTemplateArchetype.JumpBehindStrike:
                    sequence.ApplyJumpBehindStrikePreset(safeAnimationName);
                    break;
                case SequenceTemplateArchetype.SummonPulse:
                    sequence.SetRuntimeSteps(BuildSummonPulseTemplateSteps(safeAnimationName));
                    sequence.SetMetadata(safeAnimationName, safeAnimationName, "hit", "falldown", "idle");
                    break;
                default:
                    sequence.ApplyBasicStrikePreset(safeAnimationName);
                    break;
            }

            if (overwriteSequenceId || string.IsNullOrWhiteSpace(sequence.SequenceId))
            {
                sequence.SetSequenceId(BuildTemplateSequenceId(template));
            }

            EditorUtility.SetDirty(sequence);
        }

        private static string BuildTemplateSequenceId(SequenceTemplateArchetype template)
        {
            return template switch
            {
                SequenceTemplateArchetype.MeleeSingleHit => "melee_single_hit",
                SequenceTemplateArchetype.MeleeMultiHit => "melee_multi_hit",
                SequenceTemplateArchetype.RangedCast => "ranged_cast",
                SequenceTemplateArchetype.AreaBurst => "area_burst",
                SequenceTemplateArchetype.JumpBehindStrike => "jump_behind_strike",
                SequenceTemplateArchetype.SummonPulse => "summon_pulse",
                _ => "custom_sequence",
            };
        }

        private static IReadOnlyList<SkillViewStep> BuildMeleeMultiHitTemplateSteps(string animationName)
        {
            return new[]
            {
                new SkillViewStep(
                    SkillViewStepType.MoveToTarget,
                    SkillViewTargetType.PrimaryTarget,
                    animationName,
                    animationName,
                    false,
                    0.24f,
                    0f,
                    1f,
                    SkillViewMoveMode.Direct),
                new SkillViewStep(
                    SkillViewStepType.PlayAnimation,
                    SkillViewTargetType.PrimaryTarget,
                    animationName,
                    animationName,
                    false,
                    0.12f,
                    animationEvents: new[]
                    {
                        new SkillViewAnimationEvent(
                            SkillViewAnimationEventType.TriggerHit,
                            SkillViewEventTiming.OnEnd,
                            string.Empty,
                            SkillViewTargetType.PrimaryTarget,
                            UnitSocketPoint.None,
                            null,
                            null,
                            null,
                            true,
                            1,
                            true),
                    }),
                new SkillViewStep(SkillViewStepType.Wait, SkillViewTargetType.Actor, animationName, animationName, false, 0.06f),
                new SkillViewStep(
                    SkillViewStepType.PlayAnimation,
                    SkillViewTargetType.PrimaryTarget,
                    animationName,
                    animationName,
                    false,
                    0.12f,
                    animationEvents: new[]
                    {
                        new SkillViewAnimationEvent(
                            SkillViewAnimationEventType.TriggerHit,
                            SkillViewEventTiming.OnEnd,
                            string.Empty,
                            SkillViewTargetType.PrimaryTarget,
                            UnitSocketPoint.None,
                            null,
                            null,
                            null,
                            true,
                            1,
                            true),
                    }),
                new SkillViewStep(SkillViewStepType.Wait, SkillViewTargetType.Actor, animationName, animationName, false, 0.05f),
                new SkillViewStep(
                    SkillViewStepType.PlayAnimation,
                    SkillViewTargetType.PrimaryTarget,
                    animationName,
                    animationName,
                    false,
                    0.12f,
                    animationEvents: new[]
                    {
                        new SkillViewAnimationEvent(
                            SkillViewAnimationEventType.TriggerHit,
                            SkillViewEventTiming.OnEnd,
                            string.Empty,
                            SkillViewTargetType.PrimaryTarget,
                            UnitSocketPoint.None,
                            null,
                            null,
                            null,
                            true,
                            1,
                            true),
                    }),
                new SkillViewStep(SkillViewStepType.MoveBack, SkillViewTargetType.Actor, animationName, animationName, false, 0.24f),
                new SkillViewStep(SkillViewStepType.SetIdleAnimation, SkillViewTargetType.Actor, "idle", "idle", true, 0.1f)
            };
        }

        private static IReadOnlyList<SkillViewStep> BuildSummonPulseTemplateSteps(string animationName)
        {
            return new[]
            {
                new SkillViewStep(
                    SkillViewStepType.PlayAnimation,
                    SkillViewTargetType.Actor,
                    animationName,
                    animationName,
                    false,
                    0.15f,
                    animationEvents: new[]
                    {
                        new SkillViewAnimationEvent(
                            SkillViewAnimationEventType.SpawnVfx,
                            SkillViewEventTiming.OnEnd,
                            string.Empty,
                            SkillViewTargetType.Actor,
                            UnitSocketPoint.BuffTop,
                            null,
                            null,
                            null,
                            true,
                            1,
                            true),
                    }),
                new SkillViewStep(SkillViewStepType.Wait, SkillViewTargetType.Actor, animationName, animationName, false, 0.08f),
                new SkillViewStep(
                    SkillViewStepType.PlayAnimation,
                    SkillViewTargetType.AllTargets,
                    animationName,
                    animationName,
                    false,
                    0f,
                    0f,
                    1f,
                    SkillViewMoveMode.Direct,
                    false,
                    false,
                    1,
                    0,
                    false,
                    animationEvents: new[]
                    {
                        new SkillViewAnimationEvent(
                            SkillViewAnimationEventType.TriggerHit,
                            SkillViewEventTiming.OnStart,
                            string.Empty,
                            SkillViewTargetType.AllTargets,
                            UnitSocketPoint.None,
                            null,
                            null,
                            null,
                            true,
                            1,
                            true),
                    }),
                new SkillViewStep(SkillViewStepType.Wait, SkillViewTargetType.Actor, animationName, animationName, false, 0.08f),
                new SkillViewStep(
                    SkillViewStepType.PlayAnimation,
                    SkillViewTargetType.AllTargets,
                    animationName,
                    animationName,
                    false,
                    0f,
                    0f,
                    1f,
                    SkillViewMoveMode.Direct,
                    false,
                    false,
                    1,
                    0,
                    false,
                    animationEvents: new[]
                    {
                        new SkillViewAnimationEvent(
                            SkillViewAnimationEventType.TriggerHit,
                            SkillViewEventTiming.OnStart,
                            string.Empty,
                            SkillViewTargetType.AllTargets,
                            UnitSocketPoint.None,
                            null,
                            null,
                            null,
                            true,
                            1,
                            true),
                    }),
                new SkillViewStep(SkillViewStepType.SetIdleAnimation, SkillViewTargetType.Actor, "idle", "idle", true, 0.1f)
            };
        }

        private void DrawSequenceTimeline(SkillViewSequence sequence, int currentStepIndex)
        {
            if (sequence == null || sequence.Steps == null || sequence.Steps.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(sectionBodyStyle);
            EditorGUILayout.LabelField("Step Timeline", sectionHeaderStyle);
            sequenceTimelinePixelsPerSecond = EditorGUILayout.Slider(
                "Pixels / sec",
                sequenceTimelinePixelsPerSecond,
                60f,
                320f);

            float estimatedDuration = EstimateSequenceDuration(sequence.Steps);
            DrawKeyValueRow("Estimated Length", $"{estimatedDuration:0.00}s");

            float labelWidth = 168f;
            float rowHeight = 22f;
            float axisHeight = 24f;
            float timelineWidth = Mathf.Max(320f, estimatedDuration * sequenceTimelinePixelsPerSecond + 36f);
            float canvasWidth = labelWidth + timelineWidth + 12f;
            float canvasHeight = axisHeight + (sequence.Steps.Count * rowHeight) + 8f;
            float viewHeight = Mathf.Min(250f, canvasHeight + 8f);

            using (var scope = new EditorGUILayout.ScrollViewScope(sequenceTimelineScrollPosition, GUILayout.Height(viewHeight)))
            {
                sequenceTimelineScrollPosition = scope.scrollPosition;

                Rect canvasRect = GUILayoutUtility.GetRect(canvasWidth, canvasHeight, GUILayout.ExpandWidth(false));
                EditorGUI.DrawRect(canvasRect, new Color(0f, 0f, 0f, 0.12f));

                DrawTimelineAxis(canvasRect, labelWidth, timelineWidth, estimatedDuration);

                float cursor = 0f;
                for (int i = 0; i < sequence.Steps.Count; i++)
                {
                    SkillViewStep step = sequence.Steps[i];
                    float delay = Mathf.Max(0f, step != null ? step.Delay : 0f);
                    float duration = EstimateStepDuration(step);
                    float start = cursor;
                    float actionStart = start + delay;
                    float end = actionStart + duration;
                    float rowY = canvasRect.y + axisHeight + (i * rowHeight);

                    if (i % 2 == 0)
                    {
                        EditorGUI.DrawRect(new Rect(canvasRect.x, rowY, canvasRect.width, rowHeight), new Color(1f, 1f, 1f, 0.03f));
                    }

                    if (selectedPreviewTimelineStepIndex == i)
                    {
                        EditorGUI.DrawRect(new Rect(canvasRect.x, rowY, canvasRect.width, 1f), new Color(Primary.r, Primary.g, Primary.b, 0.8f));
                        EditorGUI.DrawRect(new Rect(canvasRect.x, rowY + rowHeight - 1f, canvasRect.width, 1f), new Color(Primary.r, Primary.g, Primary.b, 0.8f));
                    }

                    if (currentStepIndex == i)
                    {
                        EditorGUI.DrawRect(new Rect(canvasRect.x, rowY + 1f, 3f, rowHeight - 2f), new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.95f));
                    }

                    string label = step == null
                        ? $"{i + 1}. (null)"
                        : $"{i + 1}. {step.StepType}";
                    GUI.Label(new Rect(canvasRect.x + 6f, rowY + 3f, labelWidth - 10f, rowHeight - 6f), label, EditorStyles.miniLabel);

                    float delayX = canvasRect.x + labelWidth + (start * sequenceTimelinePixelsPerSecond);
                    float delayWidth = delay * sequenceTimelinePixelsPerSecond;
                    if (delayWidth > 0.5f)
                    {
                        EditorGUI.DrawRect(
                            new Rect(delayX, rowY + 5f, delayWidth, rowHeight - 10f),
                            new Color(1f, 1f, 1f, 0.08f));
                    }

                    float actionX = canvasRect.x + labelWidth + (actionStart * sequenceTimelinePixelsPerSecond);
                    float actionWidth = Mathf.Max(3f, duration * sequenceTimelinePixelsPerSecond);
                    Color stepColor = GetTimelineColor(step != null ? step.StepType : SkillViewStepType.Wait);
                    EditorGUI.DrawRect(new Rect(actionX, rowY + 4f, actionWidth, rowHeight - 8f), stepColor);

                    if (selectedPreviewTimelineStepIndex == i)
                    {
                        EditorGUI.DrawRect(new Rect(actionX, rowY + 4f, actionWidth, 1f), new Color(1f, 1f, 1f, 0.5f));
                        EditorGUI.DrawRect(new Rect(actionX, rowY + rowHeight - 5f, actionWidth, 1f), new Color(1f, 1f, 1f, 0.5f));
                    }

                    if (
                        step != null
                        && step.StepType == SkillViewStepType.PlayAnimation
                        && step.AnimationEvents != null
                        && step.AnimationEvents.Any(
                            animationEvent =>
                                animationEvent != null
                                && animationEvent.Enabled
                                && animationEvent.EventType == SkillViewAnimationEventType.TriggerHit))
                    {
                        EditorGUI.DrawRect(
                            new Rect(actionX - 1f, rowY + 3f, 2f, rowHeight - 6f),
                            new Color(1f, 0.25f, 0.25f, 0.9f));
                    }

                    string timingText = $"{actionStart:0.00}s -> {end:0.00}s";
                    GUI.Label(
                        new Rect(actionX + 4f, rowY + 3f, Mathf.Max(20f, actionWidth - 6f), rowHeight - 6f),
                        timingText,
                        EditorStyles.miniLabel);

                    cursor = end;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTimelineAxis(Rect canvasRect, float labelWidth, float timelineWidth, float estimatedDuration)
        {
            float axisY = canvasRect.y + 14f;
            float axisStart = canvasRect.x + labelWidth;
            float axisEnd = axisStart + timelineWidth;
            EditorGUI.DrawRect(new Rect(axisStart, axisY, timelineWidth, 1f), new Color(1f, 1f, 1f, 0.22f));

            float majorStep = estimatedDuration > 8f ? 1f : 0.5f;
            if (estimatedDuration > 20f)
            {
                majorStep = 2f;
            }

            int maxIndex = Mathf.CeilToInt(Mathf.Max(estimatedDuration, 0.01f) / majorStep);
            for (int i = 0; i <= maxIndex; i++)
            {
                float time = i * majorStep;
                float x = axisStart + (time * sequenceTimelinePixelsPerSecond);
                if (x > axisEnd)
                {
                    break;
                }

                EditorGUI.DrawRect(new Rect(x, axisY - 4f, 1f, 8f), new Color(1f, 1f, 1f, 0.26f));
                GUI.Label(new Rect(x + 2f, axisY - 14f, 46f, 14f), $"{time:0.#}s", EditorStyles.miniLabel);
            }
        }

        private static float EstimateSequenceDuration(IReadOnlyList<SkillViewStep> steps)
        {
            if (steps == null || steps.Count == 0)
            {
                return 0f;
            }

            float cursor = 0f;
            for (int i = 0; i < steps.Count; i++)
            {
                SkillViewStep step = steps[i];
                if (step == null)
                {
                    continue;
                }

                float delay = Mathf.Max(0f, step.Delay);
                float duration = EstimateStepDuration(step);
                cursor += delay + duration;
            }

            return Mathf.Max(0.05f, cursor);
        }

        private static float EstimateStepDuration(SkillViewStep step)
        {
            if (step == null)
            {
                return 0f;
            }

            float duration = Mathf.Max(0f, step.Duration);
            if (duration > 0.0001f)
            {
                return duration;
            }

            return step.StepType switch
            {
                SkillViewStepType.SetFlipX => 0.03f,
                SkillViewStepType.SetSortingOrder => 0.03f,
                SkillViewStepType.ResetSortingOrder => 0.03f,
                _ => 0.04f,
            };
        }

        private static Color GetTimelineColor(SkillViewStepType stepType)
        {
            return stepType switch
            {
                SkillViewStepType.MoveToTarget => new Color(0.45f, 0.72f, 1f, 0.95f),
                SkillViewStepType.MoveBack => new Color(0.29f, 0.65f, 1f, 0.95f),
                SkillViewStepType.PlayAnimation => new Color(0.96f, 0.68f, 0.38f, 0.96f),
                SkillViewStepType.Wait => new Color(0.76f, 0.76f, 0.76f, 0.82f),
                SkillViewStepType.SetFlipX => new Color(0.38f, 1f, 0.72f, 0.95f),
                SkillViewStepType.SetSortingOrder => new Color(0.42f, 0.88f, 1f, 0.95f),
                SkillViewStepType.ResetSortingOrder => new Color(0.42f, 0.88f, 1f, 0.8f),
                SkillViewStepType.SetIdleAnimation => new Color(0.45f, 0.95f, 0.55f, 0.95f),
                _ => new Color(1f, 1f, 1f, 0.9f),
            };
        }

        private void DrawSelectedPreviewStepDetails(SkillViewSequence sequence)
        {
            if (sequence == null || sequence.Steps == null || sequence.Steps.Count == 0)
            {
                return;
            }

            int clampedIndex = Mathf.Clamp(selectedPreviewTimelineStepIndex, 0, sequence.Steps.Count - 1);
            if (selectedPreviewTimelineStepIndex < 0 || selectedPreviewTimelineStepIndex >= sequence.Steps.Count)
            {
                return;
            }

            SkillViewStep step = sequence.Steps[clampedIndex];
            if (step == null)
            {
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical(sectionBodyStyle);
            EditorGUILayout.LabelField($"Selected Step {clampedIndex + 1}", sectionHeaderStyle);
            DrawKeyValueRow("Type", step.StepType.ToString());
            DrawKeyValueRow("Target", step.TargetType.ToString());
            DrawKeyValueRow("Animation", string.IsNullOrWhiteSpace(step.AnimationName) ? "-" : step.AnimationName);
            DrawKeyValueRow("Fallback", string.IsNullOrWhiteSpace(step.FallbackAnimationName) ? "-" : step.FallbackAnimationName);
            DrawKeyValueRow("Duration", $"{step.Duration:0.00}s");
            DrawKeyValueRow("Delay", $"{step.Delay:0.00}s");
            DrawKeyValueRow("Move Mode", step.MoveMode.ToString());
            if (step.StepType == SkillViewStepType.PlayAnimation)
            {
                DrawKeyValueRow("Wait For End", step.WaitForAnimationEnd ? "Yes" : "No");
                DrawKeyValueRow("Animation Events", step.AnimationEvents != null ? step.AnimationEvents.Count.ToString() : "0");
                DrawAnimationEventRows(step.AnimationEvents);
            }
            else if (step.StepType == SkillViewStepType.MoveToTarget || step.StepType == SkillViewStepType.MoveBack)
            {
                DrawKeyValueRow("Move Distance", $"{step.MoveDistance:0.00}");
                DrawKeyValueRow("Offset", step.Offset.ToString("F2"));
            }
            else if (step.StepType == SkillViewStepType.SetSortingOrder)
            {
                DrawKeyValueRow("Sorting Order", step.SortingOrder.ToString());
            }
            else if (step.StepType == SkillViewStepType.SetFlipX)
            {
                DrawKeyValueRow("Flip X", step.FlipX ? "True" : "False");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAnimationEventRows(IReadOnlyList<SkillViewAnimationEvent> eventsList)
        {
            if (eventsList == null || eventsList.Count == 0)
            {
                return;
            }

            for (int i = 0; i < eventsList.Count; i++)
            {
                SkillViewAnimationEvent animationEvent = eventsList[i];
                if (animationEvent == null)
                {
                    continue;
                }

                string eventLabel = $"{animationEvent.EventType} / {animationEvent.Timing}";
                if (!string.IsNullOrWhiteSpace(animationEvent.AnimationEventName))
                {
                    eventLabel += $" [{animationEvent.AnimationEventName}]";
                }

                string targetLabel = animationEvent.TargetType.ToString();
                if (animationEvent.EventType == SkillViewAnimationEventType.SpawnVfx)
                {
                    string socketLabel = animationEvent.SpawnSocket.ToString();
                    string suffix = animationEvent.Enabled ? string.Empty : " [Disabled]";
                    DrawKeyValueRow($"Event {i + 1}", $"{eventLabel} -> {targetLabel} / {socketLabel}{suffix}");
                }
                else
                {
                    string hitLabel = animationEvent.IsHitEffectEvent ? "Hit Effect" : "Logic Hit";
                    string suffix = animationEvent.Enabled ? string.Empty : " [Disabled]";
                    DrawKeyValueRow(
                        $"Event {i + 1}",
                        $"{eventLabel} -> {targetLabel} / {hitLabel} x{animationEvent.HitCount}{suffix}"
                    );
                }
            }
        }

        private void DrawPreviewMarkerOverlay(Rect previewRect)
        {
            if (!showPreviewMarkerOverlay || previewController == null || !previewController.HasPreviewObject)
            {
                return;
            }

            UnityEngine.Event evt = UnityEngine.Event.current;
            Vector3 draggedMarkerPosition;
            Vector2 previewOrigin = new Vector2(previewRect.x, previewRect.y);
            bool allowMarkerEditing = previewMarkerEditMode;
            if (!allowMarkerEditing)
            {
                draggedPreviewMarkerName = null;
            }
            if (!string.IsNullOrWhiteSpace(draggedPreviewMarkerName)
                && !previewController.TryGetPreviewMarkerWorldPosition(draggedPreviewMarkerName, out draggedMarkerPosition)
                && evt != null
                && evt.type != EventType.MouseDrag)
            {
                draggedPreviewMarkerName = null;
            }

            EditorGUI.DrawRect(
                new Rect(previewRect.x + 8f, previewRect.y + 8f, 110f, 18f),
                new Color(0f, 0f, 0f, 0.35f));
            GUI.Label(
                new Rect(previewRect.x + 12f, previewRect.y + 10f, 100f, 14f),
                "Marker Overlay",
                EditorStyles.miniBoldLabel);

            Rect legendRect = new Rect(previewRect.x + 8f, previewRect.y + 28f, 118f, 18f + (RequiredMarkers.Length * 18f));
            EditorGUI.DrawRect(legendRect, new Color(0f, 0f, 0f, 0.28f));
            GUI.Label(
                new Rect(legendRect.x + 8f, legendRect.y + 4f, legendRect.width - 16f, 14f),
                "Legend",
                EditorStyles.miniBoldLabel);

            for (int i = 0; i < RequiredMarkers.Length; i++)
            {
                MarkerDefinition definition = RequiredMarkers[i];
                if (!previewController.TryGetPreviewMarkerWorldPosition(definition.Name, out Vector3 worldPosition))
                {
                    continue;
                }

                if (!previewController.TryProjectWorldToPreviewPoint(previewRect, worldPosition, out Vector2 screenPoint))
                {
                    continue;
                }

                Color markerColor = GetMarkerOverlayColor(i);
                Rect dotRect = new Rect(screenPoint.x - 3f, screenPoint.y - 3f, 6f, 6f);
                bool isDraggingThisMarker = string.Equals(draggedPreviewMarkerName, definition.Name, StringComparison.Ordinal);
                EditorGUI.DrawRect(dotRect, markerColor);
                EditorGUI.DrawRect(new Rect(dotRect.x - 1f, dotRect.y - 1f, dotRect.width + 2f, 1f), Color.black);
                EditorGUI.DrawRect(new Rect(dotRect.x - 1f, dotRect.yMax, dotRect.width + 2f, 1f), Color.black);
                EditorGUI.DrawRect(new Rect(dotRect.x - 1f, dotRect.y - 1f, 1f, dotRect.height + 2f), Color.black);
                EditorGUI.DrawRect(new Rect(dotRect.xMax, dotRect.y - 1f, 1f, dotRect.height + 2f), Color.black);

                Rect legendRowRect = new Rect(legendRect.x + 8f, legendRect.y + 20f + (i * 18f), legendRect.width - 16f, 16f);
                Rect legendDotRect = new Rect(legendRowRect.x, legendRowRect.y + 4f, 8f, 8f);
                EditorGUI.DrawRect(legendDotRect, markerColor);
                GUI.Label(
                    new Rect(legendRowRect.x + 14f, legendRowRect.y, legendRowRect.width - 14f, 16f),
                    definition.Name,
                    EditorStyles.miniLabel);

                Rect hitRect = new Rect(
                    Mathf.Min(dotRect.xMin, legendDotRect.xMin),
                    Mathf.Min(dotRect.yMin, legendDotRect.yMin),
                    Mathf.Max(dotRect.xMax, legendDotRect.xMax) - Mathf.Min(dotRect.xMin, legendDotRect.xMin),
                    Mathf.Max(dotRect.yMax, legendDotRect.yMax) - Mathf.Min(dotRect.yMin, legendDotRect.yMin));

                if (evt == null)
                {
                    continue;
                }

                if (allowMarkerEditing && evt.type == EventType.MouseDown && evt.button == 0 && hitRect.Contains(evt.mousePosition))
                {
                    draggedPreviewMarkerName = definition.Name;
                    draggedPreviewMarkerWorldZ = worldPosition.z;
                    GUI.FocusControl(null);
                    evt.Use();
                    Repaint();
                    return;
                }

                if (allowMarkerEditing && isDraggingThisMarker && evt.type == EventType.MouseDrag && evt.button == 0)
                {
                    if (previewController.TryScreenPointToWorldPoint(previewRect, evt.mousePosition - previewOrigin, draggedPreviewMarkerWorldZ, out Vector3 nextWorldPosition))
                    {
                        UpdatePreviewMarkerPosition(definition.Name, nextWorldPosition);
                        evt.Use();
                        Repaint();
                    }
                }

                if (isDraggingThisMarker && evt.type == EventType.MouseUp && evt.button == 0)
                {
                    draggedPreviewMarkerName = null;
                    evt.Use();
                    Repaint();
                }
            }
        }

        private void UpdatePreviewMarkerPosition(string markerName, Vector3 worldPosition)
        {
            if (string.IsNullOrWhiteSpace(markerName))
            {
                return;
            }

            if (previewController != null)
            {
                previewController.TrySetPreviewMarkerWorldPosition(markerName, worldPosition);
            }

            if (workingPrefabRoot == null)
            {
                return;
            }

            Transform marker = FindMarkerTransform(workingPrefabRoot.transform, markerName);
            if (marker == null)
            {
                return;
            }

            Undo.RecordObject(marker, $"Move {markerName}");
            marker.position = worldPosition;
            EditorUtility.SetDirty(marker);
            EditorUtility.SetDirty(workingPrefabRoot);
        }

        private static Transform FindMarkerTransform(Transform root, string markerName)
        {
            if (root == null || string.IsNullOrWhiteSpace(markerName))
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (string.Equals(child.name, markerName, StringComparison.Ordinal))
                {
                    return child;
                }

                Transform nested = FindMarkerTransform(child, markerName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static Color GetMarkerOverlayColor(int index)
        {
            switch (index % 6)
            {
                case 0: return new Color(1f, 0.55f, 0.45f, 1f);
                case 1: return new Color(0.45f, 0.9f, 1f, 1f);
                case 2: return new Color(0.65f, 1f, 0.5f, 1f);
                case 3: return new Color(1f, 0.82f, 0.35f, 1f);
                case 4: return new Color(0.72f, 0.58f, 1f, 1f);
                default: return new Color(1f, 0.72f, 0.8f, 1f);
            }
        }

        private void DrawComponentStringSection()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Component Mapping", sectionHeaderStyle);

            DrawUnitViewAuthoringSection();
            DrawActionRunnerSection();

            DrawComponentStrings("Behit Behavior", behitBehavior, new[]
            {
                "behitAnimation", "dieAnimation", "idleAnimation"
            });
        }

        private void DrawUnitViewAuthoringSection()
        {
            EditorGUILayout.BeginVertical(sectionBodyStyle);
            EditorGUILayout.LabelField("Unit View", sectionHeaderStyle);

            if (unitView == null)
            {
                EditorGUILayout.HelpBox("UnitView component not found in prefab.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            var unitViewSo = new SerializedObject(unitView);
            unitViewSo.Update();

            SerializedProperty authoringIdProp = unitViewSo.FindProperty("authoringUnitId");
            if (authoringIdProp != null)
            {
                int nextId = EditorGUILayout.IntField("Authoring Unit Id", authoringIdProp.intValue);
                if (nextId != authoringIdProp.intValue)
                {
                    authoringIdProp.intValue = nextId;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Field 'authoringUnitId' not found on UnitView.", MessageType.Warning);
            }

            if (unitViewSo.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(unitView);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActionRunnerSection()
        {
            if (actionSequenceRunner == null)
            {
                EditorGUILayout.HelpBox("ActionSequenceRunner not found.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(sectionBodyStyle);
            EditorGUILayout.LabelField("Action Sequence Runner", sectionHeaderStyle);

            var serializedObject = new SerializedObject(actionSequenceRunner);
            serializedObject.Update();

            DrawProperty(serializedObject.FindProperty("animationHandle"), "Animation Handle");
            DrawProperty(serializedObject.FindProperty("speed"), "Speed");
            DrawProperty(serializedObject.FindProperty("moveDuration"), "Move Duration");

            if (serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(actionSequenceRunner);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawComponentOrderSection()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Skeleton Actions", sectionHeaderStyle);

            if (animationHandle == null)
            {
                EditorGUILayout.HelpBox("AnimationHandle not found in prefab.", MessageType.Warning);
                return;
            }

            var handleSo = new SerializedObject(animationHandle);
            handleSo.Update();

            var sortingLayerName = handleSo.FindProperty("sortingLayerName");
            if (sortingLayerName != null)
            {
                string[] layerNames = GetSortingLayerNames();
                int currentIndex = Array.IndexOf(layerNames, sortingLayerName.stringValue);
                if (currentIndex < 0)
                {
                    currentIndex = 0;
                }

                int nextIndex = EditorGUILayout.Popup("Sorting Layer", currentIndex, layerNames);
                string nextSortingLayerName = layerNames.Length > 0 ? layerNames[nextIndex] : sortingLayerName.stringValue;
                if (nextSortingLayerName != sortingLayerName.stringValue)
                {
                    sortingLayerName.stringValue = nextSortingLayerName;
                }
            }

            var sortingOrder = handleSo.FindProperty("sortingOrder");
            if (sortingOrder != null)
            {
                int nextSortingOrder = EditorGUILayout.IntField("Sorting Order", sortingOrder.intValue);
                if (nextSortingOrder != sortingOrder.intValue)
                {
                    sortingOrder.intValue = nextSortingOrder;
                }
            }

            if (handleSo.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(animationHandle);
            }
        }

        private void DrawMarkerEditorSection()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Anchor Marker Editor", sectionHeaderStyle);

            if (workingPrefabRoot == null)
            {
                EditorGUILayout.HelpBox("No working prefab loaded.", MessageType.Info);
                return;
            }

            Transform[] allTransforms = workingPrefabRoot.GetComponentsInChildren<Transform>(true);
            var markerMap = new Dictionary<string, List<Transform>>(StringComparer.Ordinal);

            for (int i = 0; i < allTransforms.Length; i++)
            {
                Transform transformNode = allTransforms[i];
                if (transformNode == null)
                {
                    continue;
                }

                string markerName = transformNode.name;
                if (!IsRequiredMarkerName(markerName))
                {
                    continue;
                }

                if (!markerMap.TryGetValue(markerName, out List<Transform> list))
                {
                    list = new List<Transform>();
                    markerMap.Add(markerName, list);
                }

                list.Add(transformNode);
            }

            int missingCount = 0;
            int duplicateCount = 0;
            for (int i = 0; i < RequiredMarkers.Length; i++)
            {
                var definition = RequiredMarkers[i];
                if (!markerMap.TryGetValue(definition.Name, out List<Transform> markers) || markers.Count == 0)
                {
                    missingCount++;
                }
                else if (markers.Count > 1)
                {
                    duplicateCount += markers.Count - 1;
                }
            }

            EditorGUILayout.BeginVertical(sectionBodyStyle);
            DrawKeyValueRow("Required Markers", RequiredMarkers.Length.ToString());
            DrawKeyValueRow("Missing", missingCount.ToString());
            DrawKeyValueRow("Duplicates", duplicateCount.ToString());
            if (missingCount > 0)
            {
                EditorGUILayout.HelpBox("Some required markers are missing. Create missing markers to keep battle placement predictable.", MessageType.Warning);
            }

            if (duplicateCount > 0)
            {
                EditorGUILayout.HelpBox("Duplicate marker names detected. Runtime lookups may pick unintended transforms.", MessageType.Warning);
            }

            bool refreshNeeded = false;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Missing", secondaryButtonStyle, GUILayout.Height(24f)))
            {
                CreateMissingMarkers(markerMap);
                refreshNeeded = true;
            }

            if (GUILayout.Button("Reset All", secondaryButtonStyle, GUILayout.Height(24f)))
            {
                ResetAllMarkersToDefault(markerMap);
                refreshNeeded = true;
            }

            if (GUILayout.Button("Zero Z", secondaryButtonStyle, GUILayout.Height(24f)))
            {
                NormalizeMarkersToZeroZ(markerMap);
                refreshNeeded = true;
            }
            EditorGUILayout.EndHorizontal();

            markerEditorShowWorldPosition = EditorGUILayout.ToggleLeft(
                "Show world position",
                markerEditorShowWorldPosition);
            EditorGUILayout.EndVertical();

            if (refreshNeeded)
            {
                RefreshPrefabCache(skeletonMetadataLoaded);
            }

            for (int i = 0; i < RequiredMarkers.Length; i++)
            {
                DrawSingleMarkerEditor(RequiredMarkers[i], markerMap);
            }
        }

        private void DrawSingleMarkerEditor(
            MarkerDefinition definition,
            Dictionary<string, List<Transform>> markerMap)
        {
            markerMap.TryGetValue(definition.Name, out List<Transform> markers);
            Transform marker = markers != null && markers.Count > 0 ? markers[0] : null;
            bool hasDuplicate = markers != null && markers.Count > 1;

            EditorGUILayout.BeginVertical(sectionBodyStyle);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(definition.Name, sectionHeaderStyle);
            if (marker == null)
            {
                GUILayout.Label("Missing", EditorStyles.miniBoldLabel, GUILayout.Width(60f));
            }
            else if (hasDuplicate)
            {
                GUILayout.Label("Duplicate", EditorStyles.miniBoldLabel, GUILayout.Width(60f));
            }
            else
            {
                GUILayout.Label("OK", EditorStyles.miniBoldLabel, GUILayout.Width(60f));
            }
            EditorGUILayout.EndHorizontal();

            if (marker == null)
            {
                EditorGUILayout.HelpBox($"{definition.Name} not found.", MessageType.Warning);
                if (GUILayout.Button($"Create {definition.Name}", secondaryButtonStyle, GUILayout.Height(22f)))
                {
                    CreateMarker(definition);
                    RefreshPrefabCache(skeletonMetadataLoaded);
                }

                EditorGUILayout.EndVertical();
                return;
            }

            if (hasDuplicate)
            {
                EditorGUILayout.HelpBox(
                    $"{definition.Name} appears {markers.Count} times. First one is being edited.",
                    MessageType.Warning);
            }

            Vector3 currentLocalPosition = marker.localPosition;
            Vector3 nextLocalPosition = EditorGUILayout.Vector3Field("Local Position", currentLocalPosition);
            if (nextLocalPosition != currentLocalPosition)
            {
                Undo.RecordObject(marker, $"Edit {definition.Name} Local Position");
                marker.localPosition = nextLocalPosition;
                EditorUtility.SetDirty(marker);
                EditorUtility.SetDirty(workingPrefabRoot);
            }

            if (markerEditorShowWorldPosition)
            {
                DrawKeyValueRow("World Position", marker.position.ToString("F3"));
            }

            DrawKeyValueRow("Hierarchy", GetTransformHierarchyPath(marker, workingPrefabRoot.transform));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping", secondaryButtonStyle, GUILayout.Height(22f)))
            {
                EditorGUIUtility.PingObject(marker.gameObject);
            }

            if (GUILayout.Button("Reset", secondaryButtonStyle, GUILayout.Height(22f)))
            {
                Undo.RecordObject(marker, $"Reset {definition.Name}");
                marker.localPosition = definition.DefaultLocalPosition;
                marker.localRotation = Quaternion.identity;
                marker.localScale = Vector3.one;
                EditorUtility.SetDirty(marker);
                EditorUtility.SetDirty(workingPrefabRoot);
            }

            if (GUILayout.Button("Move To Root", secondaryButtonStyle, GUILayout.Height(22f)))
            {
                Undo.SetTransformParent(marker, workingPrefabRoot.transform, $"Reparent {definition.Name}");
                marker.SetParent(workingPrefabRoot.transform, true);
                EditorUtility.SetDirty(marker);
                EditorUtility.SetDirty(workingPrefabRoot);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static string GetTransformHierarchyPath(Transform node, Transform root)
        {
            if (node == null)
            {
                return "-";
            }

            var segments = new List<string>();
            Transform current = node;
            while (current != null)
            {
                segments.Add(current.name);
                if (current == root)
                {
                    break;
                }

                current = current.parent;
            }

            segments.Reverse();
            return string.Join("/", segments);
        }

        private void CreateMissingMarkers(Dictionary<string, List<Transform>> markerMap)
        {
            if (workingPrefabRoot == null)
            {
                return;
            }

            bool createdAny = false;
            for (int i = 0; i < RequiredMarkers.Length; i++)
            {
                MarkerDefinition definition = RequiredMarkers[i];
                if (markerMap.TryGetValue(definition.Name, out List<Transform> markers) && markers.Count > 0)
                {
                    continue;
                }

                CreateMarker(definition);
                createdAny = true;
            }

            if (createdAny)
            {
                EditorUtility.SetDirty(workingPrefabRoot);
            }
        }

        private void ResetAllMarkersToDefault(Dictionary<string, List<Transform>> markerMap)
        {
            if (workingPrefabRoot == null)
            {
                return;
            }

            bool changed = false;
            for (int i = 0; i < RequiredMarkers.Length; i++)
            {
                MarkerDefinition definition = RequiredMarkers[i];
                if (!markerMap.TryGetValue(definition.Name, out List<Transform> markers) || markers.Count == 0)
                {
                    continue;
                }

                for (int markerIndex = 0; markerIndex < markers.Count; markerIndex++)
                {
                    Transform marker = markers[markerIndex];
                    if (marker == null)
                    {
                        continue;
                    }

                    Undo.RecordObject(marker, $"Reset {definition.Name}");
                    marker.localPosition = definition.DefaultLocalPosition;
                    marker.localRotation = Quaternion.identity;
                    marker.localScale = Vector3.one;
                    EditorUtility.SetDirty(marker);
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(workingPrefabRoot);
            }
        }

        private void NormalizeMarkersToZeroZ(Dictionary<string, List<Transform>> markerMap)
        {
            bool changed = false;
            for (int i = 0; i < RequiredMarkers.Length; i++)
            {
                MarkerDefinition definition = RequiredMarkers[i];
                if (!markerMap.TryGetValue(definition.Name, out List<Transform> markers) || markers.Count == 0)
                {
                    continue;
                }

                for (int markerIndex = 0; markerIndex < markers.Count; markerIndex++)
                {
                    Transform marker = markers[markerIndex];
                    if (marker == null)
                    {
                        continue;
                    }

                    Vector3 localPosition = marker.localPosition;
                    if (Mathf.Abs(localPosition.z) <= 0.0001f)
                    {
                        continue;
                    }

                    Undo.RecordObject(marker, $"Normalize {definition.Name} Z");
                    marker.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
                    EditorUtility.SetDirty(marker);
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(workingPrefabRoot);
            }
        }

        private void CreateMarker(MarkerDefinition definition)
        {
            if (workingPrefabRoot == null)
            {
                return;
            }

            var markerObject = new GameObject(definition.Name);
            Undo.RegisterCreatedObjectUndo(markerObject, $"Create {definition.Name}");

            Transform markerTransform = markerObject.transform;
            markerTransform.SetParent(workingPrefabRoot.transform, false);
            markerTransform.localPosition = definition.DefaultLocalPosition;
            markerTransform.localRotation = Quaternion.identity;
            markerTransform.localScale = Vector3.one;

            EditorUtility.SetDirty(markerObject);
            EditorUtility.SetDirty(workingPrefabRoot);
        }

        private static bool IsRequiredMarkerName(string markerName)
        {
            if (string.IsNullOrEmpty(markerName))
            {
                return false;
            }

            for (int i = 0; i < RequiredMarkers.Length; i++)
            {
                if (string.Equals(RequiredMarkers[i].Name, markerName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawComponentStrings(string header, Component component, IEnumerable<string> stringProperties)
        {
            if (component == null)
            {
                EditorGUILayout.HelpBox($"{header} not found.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(sectionBodyStyle);
            EditorGUILayout.LabelField(header, sectionHeaderStyle);

            var serializedObject = new SerializedObject(component);
            serializedObject.Update();

            foreach (var propertyName in stringProperties)
            {
                bool isEventField = propertyName.IndexOf("event", StringComparison.OrdinalIgnoreCase) >= 0;
                DrawPopupString(serializedObject.FindProperty(propertyName), ObjectNames.NicifyVariableName(propertyName), isEventField ? eventNames : animationNames);
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(component);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawProperty(SerializedProperty property, string label)
        {
            if (property == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(property, new GUIContent(label), true);
        }

        private void DrawPopupString(SerializedProperty property, string label, IReadOnlyList<string> options)
        {
            DrawPopupString(property, label, options, null);
        }

        private void DrawPopupString(SerializedProperty property, string label, IReadOnlyList<string> options, string filter)
        {
            if (property == null)
            {
                return;
            }

            if (options == null || options.Count == 0)
            {
                string nextValue = EditorGUILayout.TextField(label, property.stringValue);
                if (nextValue != property.stringValue)
                {
                    property.stringValue = nextValue;
                }
                return;
            }

            string currentValue = property.stringValue;
            if (!string.IsNullOrWhiteSpace(currentValue) && !options.Contains(currentValue))
            {
                property.stringValue = options[0];
                currentValue = property.stringValue;
            }

            var popupOptions = BuildPopupOptions(options, filter);
            int currentIndex = Array.IndexOf(popupOptions, property.stringValue);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = EditorGUILayout.Popup(label, currentIndex, popupOptions);
            if (nextIndex >= 0 && nextIndex < popupOptions.Length && nextIndex != currentIndex)
            {
                string nextValue = popupOptions[nextIndex] == "<None>" ? string.Empty : popupOptions[nextIndex];
                if (property.stringValue != nextValue)
                {
                    property.stringValue = nextValue;
                }
            }
        }

        private string[] BuildPopupOptions(IReadOnlyList<string> options, string filter = null)
        {
            var list = new List<string>();
            string normalizedFilter = string.IsNullOrWhiteSpace(filter) ? string.Empty : filter.Trim();

            BuildFilteredOptions(list, options, normalizedFilter);
            if (list.Count == 0 && !string.IsNullOrEmpty(normalizedFilter))
            {
                BuildFilteredOptions(list, options, string.Empty);
            }

            list.Add("<None>");
            return list.ToArray();
        }

        private static void BuildFilteredOptions(List<string> list, IReadOnlyList<string> options, string normalizedFilter)
        {
            foreach (var option in options)
            {
                if (string.IsNullOrWhiteSpace(option))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(normalizedFilter)
                    && option.IndexOf(normalizedFilter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (!list.Contains(option))
                {
                    list.Add(option);
                }
            }
        }

        private void DrawPreviewList(string title, IReadOnlyList<string> values, int maxItems)
        {
            EditorGUILayout.BeginVertical(sectionBodyStyle);
            EditorGUILayout.LabelField(title, sectionHeaderStyle);
            if (values == null || values.Count == 0)
            {
                EditorGUILayout.LabelField("(none)", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            for (int i = 0; i < Mathf.Min(values.Count, maxItems); i++)
            {
                EditorGUILayout.LabelField($"- {values[i]}", EditorStyles.miniLabel);
            }

            if (values.Count > maxItems)
            {
                EditorGUILayout.LabelField($"... +{values.Count - maxItems} more", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private string DrawSearchField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(100f));
            value = EditorGUILayout.TextField(value ?? string.Empty, searchFieldStyle, GUILayout.MinHeight(18f));
            if (GUILayout.Button("Clear", secondaryButtonStyle, GUILayout.Width(52f)))
            {
                value = string.Empty;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
            return value;
        }



        private void DrawSkillSequencePreviewTab()
        {
            EnsureWorkingPrefabLoaded(true);

            EditorGUILayout.BeginVertical(sectionBodyStyle);
            try
            {
                EditorGUILayout.LabelField("Skill Step Preview", sectionHeaderStyle);
                EditorGUILayout.LabelField("Live preview of the selected combat action on the current prefab host.", subtitleStyle);
                EditorGUILayout.Space(8f);

                EnsurePreviewController();

                GameObject previewSourcePrefab = workingPrefabRoot != null ? workingPrefabRoot : prefabAsset;
                if (previewBoundPrefab != previewSourcePrefab || !previewController.HasPreviewObject)
                {
                    previewBoundPrefab = previewSourcePrefab;
                    previewController.BindPrefab(previewSourcePrefab);
                }

                DrawPreviewSkillSlotSelector();

                CombatActionData previewActionData = GetSelectedPreviewActionData();
                SkillViewSequence previewSequence = previewActionData != null
                    ? previewActionData.ViewSequence
                    : null;

                if (previewBoundSequence != previewSequence)
                {
                    previewBoundSequence = previewSequence;
                    previewController.SetSequence(previewSequence);
                    if (previewSequence != null && previewSequence.Steps != null && previewSequence.Steps.Count > 0)
                    {
                        selectedPreviewTimelineStepIndex = Mathf.Clamp(previewController.CurrentStepIndex, 0, previewSequence.Steps.Count - 1);
                        if (selectedPreviewTimelineStepIndex < 0)
                        {
                            selectedPreviewTimelineStepIndex = 0;
                        }
                    }
                    else
                    {
                        selectedPreviewTimelineStepIndex = -1;
                    }
                }

                if (Mathf.Abs(previewController.Speed - previewPlaybackSpeed) > 0.0001f)
                {
                    previewController.Speed = previewPlaybackSpeed;
                }
                previewController.LoopPlayback = previewSequenceLoop;
                previewController.SetTargetPrefab(previewTargetPrefab);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Target", GUILayout.Width(70f));
                GameObject nextTargetPrefab = (GameObject)EditorGUILayout.ObjectField(
                    previewTargetPrefab,
                    typeof(GameObject),
                    false);
                if (nextTargetPrefab != previewTargetPrefab)
                {
                    previewTargetPrefab = nextTargetPrefab;
                    previewTargetPrefabInitialized = true;
                    previewController.SetTargetPrefab(previewTargetPrefab);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Action", GUILayout.Width(70f));
                EditorGUILayout.LabelField(
                    previewActionData != null
                        ? BuildPreviewActionLabel(previewActionData)
                        : "None selected",
                    EditorStyles.boldLabel);
                if (GUILayout.Button("Focus", secondaryButtonStyle, GUILayout.Width(60f)))
                {
                    currentTab = 1;
                    GUI.FocusControl(null);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Prefab", GUILayout.Width(70f));
                EditorGUILayout.LabelField(prefabAsset != null ? prefabAsset.name : "No prefab bound", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                bool showPauseButton = previewController.IsPlaying && !previewController.IsIdleLoopActive;
                if (showPauseButton)
                {
                    if (GUILayout.Button("Pause", primaryButtonStyle, GUILayout.Height(28f)))
                    {
                        previewController.Pause();
                    }
                }
                else
                {
                    if (GUILayout.Button("Play", primaryButtonStyle, GUILayout.Height(28f)))
                    {
                        previewController.TogglePlayback();
                    }
                }

                if (GUILayout.Button("Restart", secondaryButtonStyle, GUILayout.Height(28f)))
                {
                    previewController.Restart();
                }

                previewSequenceLoop = EditorGUILayout.ToggleLeft("Loop", previewSequenceLoop, GUILayout.Width(52f));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Speed", GUILayout.Width(70f));
                previewPlaybackSpeed = EditorGUILayout.Slider(previewPlaybackSpeed, 0.25f, 3f);
                EditorGUILayout.LabelField(string.Format("{0:0.00}x", previewPlaybackSpeed), GUILayout.Width(48f));
                showPreviewMarkerOverlay = EditorGUILayout.ToggleLeft("Markers", showPreviewMarkerOverlay, GUILayout.Width(78f));
                previewMarkerEditMode = EditorGUILayout.ToggleLeft("Edit", previewMarkerEditMode, GUILayout.Width(54f));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(8f);

                Rect previewRect = GUILayoutUtility.GetRect(1f, 320f, GUILayout.ExpandWidth(true));
                previewRect.height = 320f;
                EditorGUI.DrawRect(previewRect, PanelAltColor);
                previewController.DrawPreview(previewRect);
                DrawPreviewMarkerOverlay(previewRect);

                EditorGUILayout.Space(8f);

                EditorGUILayout.LabelField("Step Preview", sectionHeaderStyle);

                if (characterData == null)
                {
                    EditorGUILayout.HelpBox("Assign a CharacterDataSO first.", MessageType.Info);
                    return;
                }

                if (previewActionData == null)
                {
                    EditorGUILayout.HelpBox("Select an action from Character Data to preview its steps.", MessageType.Info);
                    return;
                }

                if (previewSequence == null || previewSequence.Steps == null || previewSequence.Steps.Count == 0)
                {
                    EditorGUILayout.HelpBox("The selected action has no preview steps.", MessageType.Warning);
                    return;
                }

                DrawSequenceTimeline(previewSequence, previewController.CurrentStepIndex);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(showPreviewStepDetails ? "Hide Detail" : "Show Detail", secondaryButtonStyle, GUILayout.Width(100f)))
                {
                    showPreviewStepDetails = !showPreviewStepDetails;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginVertical(sectionBodyStyle);
                EditorGUILayout.LabelField(string.Format("Status: {0}", previewController.StatusText), EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField(string.Format("Current step: {0}", previewController.CurrentStepIndex >= 0 ? (previewController.CurrentStepIndex + 1).ToString() : "-"), EditorStyles.miniLabel);

                for (int i = 0; i < previewSequence.Steps.Count; i++)
                {
                    var step = previewSequence.Steps[i];
                    Rect rowRect = GUILayoutUtility.GetRect(1f, 24f, GUILayout.ExpandWidth(true));
                    bool isCurrent = previewController.CurrentStepIndex == i;
                    bool isSelected = selectedPreviewTimelineStepIndex == i;
                    bool isCompleted = previewController.CurrentStepIndex > i;

                    Color rowColor = isSelected
                        ? new Color(Primary.r, Primary.g, Primary.b, 0.14f)
                        : (isCurrent ? AccentSoftColor : (isCompleted ? new Color(1f, 1f, 1f, 0.04f) : PanelColor));
                    EditorGUI.DrawRect(rowRect, rowColor);

                    string stepLabel = step != null
                        ? string.Format(
                            "{0}. {1}  |  anim {2}  |  loop {3}  |  delay {4:0.##}  |  duration {5:0.##}",
                            i + 1,
                            step.StepType,
                            string.IsNullOrWhiteSpace(step.AnimationName) ? "-" : step.AnimationName,
                            step.Loop ? "on" : "off",
                            step.Delay,
                            step.Duration)
                        : string.Format("{0}. (null step)", i + 1);

                    Rect labelRect = new Rect(rowRect.x + 8f, rowRect.y + 3f, rowRect.width - 16f, rowRect.height - 6f);
                    GUI.Label(labelRect, stepLabel, (isCurrent || isSelected) ? sectionHeaderStyle : EditorStyles.miniLabel);

                    if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
                    {
                        selectedPreviewTimelineStepIndex = i;
                        previewController.SeekToStepIndex(i);
                        Repaint();
                    }
                }

                if (showPreviewStepDetails)
                {
                    DrawSelectedPreviewStepDetails(previewSequence);
                }
                EditorGUILayout.EndVertical();
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawPreviewSkillSlotSelector()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Action", GUILayout.Width(70f));

            string[] labels = BuildPreviewActionLabels();
            int nextIndex = EditorGUILayout.Popup(selectedPreviewActionIndex, labels);
            nextIndex = Mathf.Clamp(nextIndex, 0, labels.Length - 1);

            if (nextIndex != selectedPreviewActionIndex)
            {
                selectedPreviewActionIndex = nextIndex;
                previewBoundSequence = null;
                SyncPreviewSkillSelectionFromCharacterData();
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        private string[] BuildPreviewActionLabels()
        {
            if (characterData == null)
            {
                return new[] { "<No Character>" };
            }

            characterData.EnsureActionsData();
            var actions = characterData.Actions;
            if (actions == null || actions.Count == 0)
            {
                return new[] { "<No Actions>" };
            }

            selectedPreviewActionIndex = Mathf.Clamp(selectedPreviewActionIndex, 0, actions.Count - 1);

            var labels = new string[actions.Count];
            for (int i = 0; i < actions.Count; i++)
            {
                labels[i] = BuildPreviewActionLabel(actions[i]);
            }

            return labels;
        }

        private string BuildPreviewActionLabel(CombatActionData actionData)
        {
            if (actionData == null)
            {
                return "<None>";
            }

            string name = !string.IsNullOrWhiteSpace(actionData.DisplayName)
                ? actionData.DisplayName
                : actionData.ActionKind.ToString();
            string actionId = !string.IsNullOrWhiteSpace(actionData.ActionId)
                ? actionData.ActionId
                : "-";
            return $"{name} [{actionData.ActionKind}] #{actionId}";
        }

        private CombatActionData GetSelectedPreviewActionData()
        {
            if (characterData == null)
            {
                return null;
            }

            characterData.EnsureActionsData();
            var actions = characterData.Actions;
            if (actions == null || actions.Count == 0)
            {
                return null;
            }

            selectedPreviewActionIndex = Mathf.Clamp(selectedPreviewActionIndex, 0, actions.Count - 1);
            return actions[selectedPreviewActionIndex];
        }

        private void SyncPreviewSkillSelectionFromCharacterData()
        {
            CombatActionData previewAction = GetSelectedPreviewActionData();
            if (previewAction != null)
            {
                previewAction.InvalidateViewSequenceCache();
            }
            SkillViewSequence previewSequence = previewAction != null ? previewAction.ViewSequence : null;
            previewBoundSequence = previewSequence;

            if (previewController == null)
            {
                return;
            }

            previewController.SetSequence(previewSequence);
        }

        private void EnsurePreviewController()
        {
            if (previewController == null)
            {
                previewController = new SkillSequencePreviewController();
            }

            previewController.SetRepaintCallback(Repaint);
            previewController.Speed = previewPlaybackSpeed;
            previewController.ShowEventPopups = true;
        }

        private void EnsureDefaultPreviewTargetPrefab()
        {
            if (previewTargetPrefabInitialized)
            {
                return;
            }

            if (previewTargetPrefab == null)
            {
                previewTargetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/AssetGame/ArtWork/Prefab/Role/tian_jiang.prefab"
                );
            }

            previewTargetPrefabInitialized = true;
        }

        private void EnsureSkeletonPreviewController()
        {
            if (skeletonPreviewController == null)
            {
                skeletonPreviewController = new SkillSequencePreviewController();
            }

            skeletonPreviewController.SetRepaintCallback(Repaint);
            skeletonPreviewController.Speed = previewPlaybackSpeed;
            skeletonPreviewController.ShowEventPopups = false;
        }

        private void PreviewSkeletonAnimation(string animationName, float duration)
        {
            if (string.IsNullOrWhiteSpace(animationName))
            {
                return;
            }

            if (prefabAsset == null)
            {
                EditorUtility.DisplayDialog("Preview Animation", "Assign a prefab first so the preview host can be created.", "OK");
                return;
            }

            EnsureSkeletonPreviewController();

            if (skeletonPreviewBoundPrefab != prefabAsset || !skeletonPreviewController.HasPreviewObject)
            {
                skeletonPreviewBoundPrefab = prefabAsset;
                skeletonPreviewController.BindPrefab(prefabAsset);
            }

            ClearPreviewAnimationOverride(false);

            float previewDuration = Mathf.Max(duration, 0.35f);
            var runtimeSequence = SkillViewSequence.CreateRuntimeSequence(
                $"preview_{animationName}",
                new SkillViewStep(
                    SkillViewStepType.PlayAnimation,
                    SkillViewTargetType.Actor,
                    animationName,
                    animationName,
                    false,
                    previewDuration,
                    0f,
                    1f,
                    SkillViewMoveMode.Direct,
                    true,
                    false,
                    1));

            runtimeSequence.hideFlags = HideFlags.HideAndDontSave;
            runtimeSequence.SetMetadata(animationName, animationName, "hit", "falldown", "idle");

            previewOverrideSequence = runtimeSequence;
            previewOverrideLabel = $"Animation preview: {animationName}";
            skeletonPreviewController.SetSequence(previewOverrideSequence);
            skeletonPreviewController.Restart();

            currentTab = 2;
            Repaint();
        }

        private void ClearPreviewAnimationOverride(bool restoreSelectedSequence = true)
        {
            if (previewOverrideSequence != null)
            {
                if (skeletonPreviewController != null && skeletonPreviewController.Sequence == previewOverrideSequence)
                {
                    skeletonPreviewController.SetSequence(null);
                }

                UnityEngine.Object.DestroyImmediate(previewOverrideSequence);
                previewOverrideSequence = null;
                previewOverrideLabel = null;
            }
        }

        private void DrawAssetPreviewPanel(string title, UnityEngine.Object asset, string line1, string line2, float previewHeight = 132f)
        {
            EditorGUILayout.BeginVertical(sectionBodyStyle);
            EditorGUILayout.LabelField(title, sectionHeaderStyle);

            Texture2D previewTexture = asset != null ? AssetPreview.GetAssetPreview(asset) : null;
            if (previewTexture == null && asset != null)
            {
                previewTexture = AssetPreview.GetMiniThumbnail(asset) as Texture2D;
            }

            Rect previewRect = GUILayoutUtility.GetRect(1f, previewHeight, GUILayout.ExpandWidth(true));
            previewRect.height = previewHeight;
            EditorGUI.DrawRect(previewRect, PanelAltColor);

            if (previewTexture != null)
            {
                Rect imageRect = new Rect(previewRect.x + 6f, previewRect.y + 6f, previewRect.width - 12f, previewRect.height - 40f);
                GUI.DrawTexture(imageRect, previewTexture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                EditorGUI.LabelField(previewRect, asset == null ? "No preview available" : "Loading preview...", EditorStyles.centeredGreyMiniLabel);
            }

            Rect textRect = new Rect(previewRect.x + 8f, previewRect.yMax - 30f, previewRect.width - 16f, 24f);
            GUI.Label(textRect, line1 ?? string.Empty, EditorStyles.miniBoldLabel);
            if (!string.IsNullOrEmpty(line2))
            {
                Rect textRect2 = new Rect(previewRect.x + 8f, previewRect.yMax - 16f, previewRect.width - 16f, 16f);
                GUI.Label(textRect2, line2, EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSkeletonAnimationPreviewPanel()
        {
            EditorGUILayout.BeginVertical(sectionBodyStyle);
            EditorGUILayout.LabelField("Animation Preview", sectionHeaderStyle);
            EditorGUILayout.LabelField(
                previewOverrideSequence != null
                    ? previewOverrideLabel ?? "Animation preview"
                    : "Click Preview on an animation row to play it here.",
                subtitleStyle);
            EditorGUILayout.Space(6f);

            bool hasAnimationPreview = previewOverrideSequence != null && skeletonPreviewController != null && skeletonPreviewController.HasPreviewObject;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Speed", GUILayout.Width(70f));
            previewPlaybackSpeed = EditorGUILayout.Slider(previewPlaybackSpeed, 0.25f, 3f);
            EditorGUILayout.LabelField(string.Format("{0:0.00}x", previewPlaybackSpeed), GUILayout.Width(48f));
            EditorGUILayout.EndHorizontal();

            if (skeletonPreviewController != null && Mathf.Abs(skeletonPreviewController.Speed - previewPlaybackSpeed) > 0.0001f)
            {
                skeletonPreviewController.Speed = previewPlaybackSpeed;
            }

            float previewRectHeight = Mathf.Clamp(position.height * 0.30f, 190f, 280f);
            Rect previewRect = GUILayoutUtility.GetRect(1f, previewRectHeight, GUILayout.ExpandWidth(true));
            previewRect.height = previewRectHeight;
            EditorGUI.DrawRect(previewRect, PanelAltColor);

            if (hasAnimationPreview)
            {
                skeletonPreviewController.DrawPreview(previewRect);
            }
            else
            {
                EditorGUI.LabelField(previewRect, "No animation selected", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private bool TryCreateUnitAssetsFromSkeleton(SkeletonDataAsset sourceSkeletonData)
        {
            if (
                !TryValidateCreateInputs(
                    sourceSkeletonData,
                    out string validationMessage,
                    out string soFolderPath,
                    out string prefabFolderPath,
                    out string templatePrefabPath
                )
            )
            {
                createUnitAssetStatus = validationMessage;
                createUnitAssetStatusIsError = true;
                return false;
            }

            string unitName = DeriveUnitNameFromSkeletonData(sourceSkeletonData);
            int nextId = GenerateNextUnitIdFromSoFolder(soFolderPath);
            string unitKey = $"{nextId}_{unitName}";
            string soAssetPath = $"{soFolderPath}/{unitKey}.asset";
            string prefabAssetPath = $"{prefabFolderPath}/{unitKey}.prefab";

            if (
                AssetDatabase.LoadAssetAtPath<CharacterDataSO>(soAssetPath) != null
                || AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath) != null
            )
            {
                createUnitAssetStatus = $"Assets already exist: {unitKey}";
                createUnitAssetStatusIsError = true;
                return false;
            }

            CharacterDataSO createdCharacterData = null;
            bool createdCharacterDataAsset = false;
            bool createdPrefabAsset = false;
            try
            {
                createdCharacterData = CreateInstance<CharacterDataSO>();
                createdCharacterData.id = nextId;
                createdCharacterData.nameHero = unitName;
                if (createdCharacterData.level <= 0)
                {
                    createdCharacterData.level = 1;
                }

                createdCharacterData.InitializeDefaultStats();
                createdCharacterData.EnsureActionsData();
                AssetDatabase.CreateAsset(createdCharacterData, soAssetPath);
                createdCharacterDataAsset = true;

                if (!AssetDatabase.CopyAsset(templatePrefabPath, prefabAssetPath))
                {
                    createUnitAssetStatus = $"Failed to copy template prefab: {templatePrefabPath}";
                    createUnitAssetStatusIsError = true;
                    return false;
                }

                createdPrefabAsset = true;

                if (
                    !ConfigureNewUnitPrefab(
                        prefabAssetPath,
                        unitKey,
                        nextId,
                        sourceSkeletonData,
                        out string configureError
                    )
                )
                {
                    createUnitAssetStatus = configureError;
                    createUnitAssetStatusIsError = true;
                    return false;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                CharacterDataSO createdDataAsset = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(soAssetPath);
                GameObject createdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
                characterDataIndexDirty = true;
                characterData = createdDataAsset;
                SetPrefabAsset(createdPrefab);
                StartUnitViewPrefabScan(false);

                Selection.activeObject = createdPrefab != null ? (UnityEngine.Object)createdPrefab : createdDataAsset;
                if (Selection.activeObject != null)
                {
                    EditorGUIUtility.PingObject(Selection.activeObject);
                }

                createUnitAssetStatus = $"Created: {unitKey}";
                createUnitAssetStatusIsError = false;
                return true;
            }
            catch (Exception ex)
            {
                createUnitAssetStatus = $"Create failed: {ex.Message}";
                createUnitAssetStatusIsError = true;
                return false;
            }
            finally
            {
                if (createUnitAssetStatusIsError)
                {
                    if (createdPrefabAsset && AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath) != null)
                    {
                        AssetDatabase.DeleteAsset(prefabAssetPath);
                    }

                    if (createdCharacterDataAsset && AssetDatabase.LoadAssetAtPath<CharacterDataSO>(soAssetPath) != null)
                    {
                        AssetDatabase.DeleteAsset(soAssetPath);
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
        }

        private bool TryValidateCreateInputs(
            SkeletonDataAsset sourceSkeletonData,
            out string validationMessage,
            out string soFolderPath,
            out string prefabFolderPath,
            out string templatePrefabPath
        )
        {
            soFolderPath = characterDataSearchFolderPath;
            prefabFolderPath = prefabSearchFolderPath;
            templatePrefabPath = UnitCreationTemplatePrefabPath;
            validationMessage = string.Empty;

            if (sourceSkeletonData == null)
            {
                validationMessage = "Select Skeleton Data Asset first.";
                return false;
            }

            if (string.IsNullOrEmpty(soFolderPath) || !AssetDatabase.IsValidFolder(soFolderPath))
            {
                validationMessage = "SO Folder is invalid.";
                return false;
            }

            if (string.IsNullOrEmpty(prefabFolderPath) || !AssetDatabase.IsValidFolder(prefabFolderPath))
            {
                validationMessage = "Prefab Folder is invalid.";
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(templatePrefabPath) == null)
            {
                validationMessage = $"Template prefab not found: {templatePrefabPath}";
                return false;
            }

            return true;
        }

        private int GenerateNextUnitIdFromSoFolder(string soFolderPath)
        {
            if (string.IsNullOrEmpty(soFolderPath) || !AssetDatabase.IsValidFolder(soFolderPath))
            {
                return 1;
            }

            int maxId = 0;
            string[] guids = AssetDatabase.FindAssets("t:CharacterDataSO", new[] { soFolderPath });
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                CharacterDataSO data = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(assetPath);
                if (data != null && data.id > maxId)
                {
                    maxId = data.id;
                }
            }

            return maxId + 1;
        }

        private string DeriveUnitNameFromSkeletonData(SkeletonDataAsset sourceSkeletonData)
        {
            if (sourceSkeletonData == null)
            {
                return "unit";
            }

            if (TryDeriveUnitNameFromExistingPrefab(sourceSkeletonData, out string existingUnitName))
            {
                return NormalizeUnitNamePart(existingUnitName);
            }

            string baseName = sourceSkeletonData.name;
            baseName = RemoveSuffixInsensitive(baseName, "_SkeletonDataAsset");
            baseName = RemoveSuffixInsensitive(baseName, "_SkeletonData");
            baseName = RemoveSuffixInsensitive(baseName, "SkeletonDataAsset");
            baseName = RemoveSuffixInsensitive(baseName, "SkeletonData");
            string snakeCaseName = ToSnakeCase(baseName);
            return NormalizeUnitNamePart(string.IsNullOrWhiteSpace(snakeCaseName) ? baseName : snakeCaseName);
        }

        private bool ConfigureNewUnitPrefab(
            string prefabAssetPath,
            string unitKey,
            int unitId,
            SkeletonDataAsset sourceSkeletonData,
            out string error
        )
        {
            error = string.Empty;
            GameObject prefabRoot = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(prefabAssetPath);
                if (prefabRoot == null)
                {
                    error = "Failed to open new prefab for editing.";
                    return false;
                }

                prefabRoot.name = unitKey;

                UnitView prefabUnitView = prefabRoot.GetComponentInChildren<UnitView>(true);
                if (prefabUnitView != null)
                {
                    var unitViewSerializedObject = new SerializedObject(prefabUnitView);
                    SerializedProperty authoringIdProperty = unitViewSerializedObject.FindProperty("authoringUnitId");
                    if (authoringIdProperty != null)
                    {
                        authoringIdProperty.intValue = unitId;
                        unitViewSerializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                SkeletonAnimation prefabSkeletonAnimation =
                    prefabRoot.transform.Find("model")?.GetComponent<SkeletonAnimation>()
                    ?? prefabRoot.GetComponentInChildren<SkeletonAnimation>(true);
                if (prefabSkeletonAnimation == null)
                {
                    error = "Template prefab is missing SkeletonAnimation.";
                    return false;
                }

                prefabSkeletonAnimation.skeletonDataAsset = sourceSkeletonData;
                prefabSkeletonAnimation.Initialize(true);
                prefabSkeletonAnimation.LateUpdate();
                EditorUtility.SetDirty(prefabSkeletonAnimation);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabAssetPath);
                return true;
            }
            catch (Exception ex)
            {
                error = $"Failed to configure prefab: {ex.Message}";
                return false;
            }
            finally
            {
                if (prefabRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
        }

        private bool TryDeriveUnitNameFromExistingPrefab(SkeletonDataAsset sourceSkeletonData, out string unitName)
        {
            unitName = null;
            string[] searchFolders = GetSelectedUnitViewSearchFolders();
            if (searchFolders == null)
            {
                return false;
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", searchFolders);
            string fallbackUnitName = null;
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    continue;
                }

                GameObject candidatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (candidatePrefab == null)
                {
                    continue;
                }

                SkeletonAnimation candidateSkeleton = candidatePrefab.GetComponentInChildren<SkeletonAnimation>(true);
                if (candidateSkeleton == null || candidateSkeleton.skeletonDataAsset != sourceSkeletonData)
                {
                    continue;
                }

                string parsedName = RemoveLeadingIdPrefix(candidatePrefab.name);
                if (!string.IsNullOrWhiteSpace(parsedName))
                {
                    if (string.Equals(prefabPath, UnitCreationTemplatePrefabPath, StringComparison.Ordinal))
                    {
                        unitName = parsedName;
                        return true;
                    }

                    fallbackUnitName ??= parsedName;
                }
            }

            unitName = fallbackUnitName;
            return !string.IsNullOrWhiteSpace(unitName);
        }

        private static string RemoveLeadingIdPrefix(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            int index = 0;
            while (index < value.Length && char.IsDigit(value[index]))
            {
                index++;
            }

            if (index > 0 && index < value.Length && value[index] == '_')
            {
                return value.Substring(index + 1);
            }

            return value;
        }

        private static string ToSnakeCase(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return "unit";
            }

            var builder = new StringBuilder(rawValue.Length + 8);
            char previousOutput = '\0';
            for (int i = 0; i < rawValue.Length; i++)
            {
                char ch = rawValue[i];
                if (char.IsLetterOrDigit(ch))
                {
                    bool isUpper = char.IsUpper(ch);
                    bool currentIsDigit = char.IsDigit(ch);
                    bool previousIsDigit = char.IsDigit(previousOutput);
                    bool previousIsLetter = char.IsLetter(previousOutput);
                    bool shouldInsertUnderscore =
                        builder.Length > 0
                        && previousOutput != '_'
                        && (
                            (isUpper && (char.IsLower(previousOutput) || previousIsDigit))
                            || (currentIsDigit && previousIsLetter)
                            || (!currentIsDigit && previousIsDigit)
                        );

                    if (shouldInsertUnderscore)
                    {
                        builder.Append('_');
                    }

                    char lowerChar = char.ToLowerInvariant(ch);
                    builder.Append(lowerChar);
                    previousOutput = lowerChar;
                    continue;
                }

                if (builder.Length > 0 && previousOutput != '_')
                {
                    builder.Append('_');
                    previousOutput = '_';
                }
            }

            string snakeCase = builder.ToString().Trim('_');
            return string.IsNullOrWhiteSpace(snakeCase) ? "unit" : snakeCase;
        }

        private void UseSelection()
        {
            var selected = Selection.activeObject;
            if (selected is CharacterDataSO selectedCharacterData)
            {
                characterData = selectedCharacterData;
            }

            if (selected is GameObject selectedPrefab && PrefabUtility.GetPrefabAssetType(selectedPrefab) != PrefabAssetType.NotAPrefab)
            {
                if (selectedPrefab.GetComponentInChildren<UnitView>(true) != null)
                {
                    SetPrefabAsset(selectedPrefab);
                }
            }
        }

        private void CreateCharacterDataAsset()
        {
            string defaultName = characterData != null
                ? $"{characterData.id}_{characterData.nameHero}"
                : "CharacterData";

            string path = EditorUtility.SaveFilePanelInProject(
                "Create Character Data",
                defaultName,
                "asset",
                "Choose a location for the new CharacterDataSO asset");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var asset = CreateInstance<CharacterDataSO>();
            AssetDatabase.CreateAsset(asset, path);

            var serializedObject = new SerializedObject(asset);
            serializedObject.Update();
            var idProp = serializedObject.FindProperty("id");
            if (idProp != null && idProp.intValue == 0)
            {
                idProp.intValue = 1;
            }

            var nameProp = serializedObject.FindProperty("nameHero");
            if (nameProp != null && string.IsNullOrWhiteSpace(nameProp.stringValue))
            {
                nameProp.stringValue = prefabAsset != null ? prefabAsset.name : "NewUnit";
            }

            var levelProp = serializedObject.FindProperty("level");
            if (levelProp != null && levelProp.intValue <= 0)
            {
                levelProp.intValue = 1;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            asset.InitializeDefaultStats();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            characterData = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(path);
            Selection.activeObject = characterData;
            EditorGUIUtility.PingObject(characterData);
        }



        private void SyncAssetNames(string forcedUnitName = null)
        {
            if (workingPrefabRoot != null)
            {
                SavePrefabWorkingCopy();
            }

            if (characterData != null)
            {
                SyncCharacterAssetName(characterData, forcedUnitName);
            }

            if (prefabAsset != null)
            {
                SyncPrefabAssetName(prefabAsset, forcedUnitName);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void SyncCharacterAssetName(CharacterDataSO data, string forcedUnitName = null)
        {
            if (data == null)
            {
                return;
            }

            string normalizedName = NormalizeUnitNamePart(
                string.IsNullOrWhiteSpace(forcedUnitName) ? data.nameHero : forcedUnitName
            );
            if (!string.Equals(data.nameHero, normalizedName, StringComparison.Ordinal))
            {
                data.nameHero = normalizedName;
                EditorUtility.SetDirty(data);
            }

            string expectedName = GetCharacterAssetName(data);
            string assetPath = AssetDatabase.GetAssetPath(data);
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            string currentName = Path.GetFileNameWithoutExtension(assetPath);
            if (!string.Equals(currentName, expectedName, StringComparison.Ordinal))
            {
                AssetDatabase.RenameAsset(assetPath, expectedName);
            }
        }

        private void SyncPrefabAssetName(GameObject prefab, string forcedUnitName = null)
        {
            if (prefab == null)
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            string expectedName = characterData != null
                ? $"{characterData.id}_{NormalizeUnitNamePart(string.IsNullOrWhiteSpace(forcedUnitName) ? characterData.nameHero : forcedUnitName)}"
                : Path.GetFileNameWithoutExtension(assetPath);

            string currentName = Path.GetFileNameWithoutExtension(assetPath);
            if (!string.Equals(currentName, expectedName, StringComparison.Ordinal))
            {
                AssetDatabase.RenameAsset(assetPath, expectedName);
                SetPrefabAsset(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(prefab)));
            }
        }

        private static string GetCharacterAssetName(CharacterDataSO data)
        {
            if (data == null)
            {
                return "CharacterData";
            }

            string safeName = NormalizeUnitNamePart(data.nameHero);
            return $"{data.id}_{safeName}";
        }

        private string DeriveUnitNameForSaveAll()
        {
            if (prefabAsset != null)
            {
                return NormalizeUnitNamePart(prefabAsset.name);
            }

            if (characterData != null)
            {
                return NormalizeUnitNamePart(characterData.nameHero);
            }

            return "Character";
        }

        private static string NormalizeUnitNamePart(string rawName)
        {
            string value = string.IsNullOrWhiteSpace(rawName) ? "Character" : rawName.Trim();
            value = RemoveSuffixInsensitive(value, "_BattleUnit");
            value = RemoveSuffixInsensitive(value, "_Unit");
            value = value.Trim('_', ' ');
            return string.IsNullOrWhiteSpace(value) ? "Character" : value;
        }

        private static string RemoveSuffixInsensitive(string value, string suffix)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(suffix))
            {
                return value;
            }

            return value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                ? value.Substring(0, value.Length - suffix.Length)
                : value;
        }

        private void SetPrefabAsset(GameObject newPrefab)
        {
            if (prefabAsset == newPrefab && workingPrefabRoot != null)
            {
                TryAutoBindCharacterDataFromPrefab(newPrefab);
                if (previewController != null)
                {
                    previewController.BindPrefab(prefabAsset);
                }
                if (skeletonPreviewController != null)
                {
                    skeletonPreviewController.BindPrefab(prefabAsset);
                }
                return;
            }

            prefabAsset = newPrefab;
            TryAutoBindCharacterDataFromPrefab(newPrefab);
            ClearPreviewAnimationOverride(false);
            LoadPrefabWorkingCopy(newPrefab, false);
            SyncUnitViewPrefabSelection();

            if (previewController != null)
            {
                previewController.BindPrefab(prefabAsset);
            }
            if (skeletonPreviewController != null)
            {
                skeletonPreviewController.BindPrefab(prefabAsset);
            }
        }

        private void DrawUnitViewPrefabList()
        {
            EditorGUILayout.LabelField("UnitView List", sectionHeaderStyle);
            DrawUnitViewSearchFolderSettings();

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !isUnitViewPrefabScanRunning;
            string loadButtonLabel = unitViewPrefabListLoaded ? "Reload UnitView List" : "Load UnitView List";
            if (GUILayout.Button(loadButtonLabel, secondaryButtonStyle, GUILayout.Height(22f)))
            {
                StartUnitViewPrefabScan(false);
            }

            if (GUILayout.Button("Rescan Folder", secondaryButtonStyle, GUILayout.Height(22f)))
            {
                StartUnitViewPrefabScan(false);
            }
            GUI.enabled = true;

            if (isUnitViewPrefabScanRunning)
            {
                if (GUILayout.Button("Cancel", secondaryButtonStyle, GUILayout.Width(64f), GUILayout.Height(22f)))
                {
                    StopUnitViewPrefabScan();
                }
            }
            EditorGUILayout.EndHorizontal();
            DrawUnitViewModeSwitcher();

            EditorGUILayout.LabelField(unitViewPrefabScanStatus, EditorStyles.miniLabel);
            if (unitViewPrefabCacheDirty && !isUnitViewPrefabScanRunning && unitViewPrefabListLoaded)
            {
                EditorGUILayout.LabelField("Cache is stale and waiting for refresh.", EditorStyles.miniLabel);
            }

            if (!string.IsNullOrEmpty(unitViewAutoBindStatus))
            {
                EditorGUILayout.LabelField(unitViewAutoBindStatus, EditorStyles.miniLabel);
            }

            if (isUnitViewPrefabScanRunning)
            {
                float progress = unitViewPrefabScanTotal > 0
                    ? unitViewPrefabScanIndex / (float)unitViewPrefabScanTotal
                    : 0f;
                Rect progressRect = GUILayoutUtility.GetRect(1f, 18f, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(
                    progressRect,
                    progress,
                    $"{unitViewPrefabScanIndex}/{unitViewPrefabScanTotal}"
                );
            }

            using (var scope = new EditorGUILayout.ScrollViewScope(unitViewListScroll, GUILayout.Height(180f)))
            {
                unitViewListScroll = scope.scrollPosition;

                if (unitViewPrefabs.Count == 0)
                {
                    string placeholder = unitViewPrefabListLoaded
                        ? "(No UnitView prefabs found)"
                        : "(UnitView list not loaded)";
                    EditorGUILayout.LabelField(placeholder, EditorStyles.miniLabel);
                    return;
                }

                if (unitViewBrowserMode == UnitViewBrowserMode.Detail)
                {
                    DrawUnitViewDetailItems();
                }
                else
                {
                    DrawUnitViewListItems();
                }
            }
        }

        private void DrawUnitViewSearchFolderSettings()
        {
            EditorGUI.BeginChangeCheck();
            DefaultAsset soFolderAsset = GetFolderAsset(characterDataSearchFolderPath);
            var nextSoFolderAsset = (DefaultAsset)EditorGUILayout.ObjectField(
                "SO Folder",
                soFolderAsset,
                typeof(DefaultAsset),
                false
            );
            if (EditorGUI.EndChangeCheck())
            {
                SetCharacterDataSearchFolder(nextSoFolderAsset);
            }

            EditorGUI.BeginChangeCheck();
            DefaultAsset prefabFolderAsset = GetFolderAsset(prefabSearchFolderPath);
            var nextPrefabFolderAsset = (DefaultAsset)EditorGUILayout.ObjectField(
                "Prefab Folder",
                prefabFolderAsset,
                typeof(DefaultAsset),
                false
            );
            if (EditorGUI.EndChangeCheck())
            {
                SetPrefabSearchFolder(nextPrefabFolderAsset);
            }
        }

        private static DefaultAsset GetFolderAsset(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
        }

        private void SetCharacterDataSearchFolder(DefaultAsset folderAsset)
        {
            string selectedPath = folderAsset != null ? AssetDatabase.GetAssetPath(folderAsset) : null;
            string nextPath = ResolveFolderPath(selectedPath, DefaultCharacterDataSearchFolder);
            if (string.Equals(characterDataSearchFolderPath, nextPath, StringComparison.Ordinal))
            {
                return;
            }

            characterDataSearchFolderPath = nextPath;
            characterDataIndexDirty = true;
            SaveUnitViewSearchFoldersToCache();
            TryAutoBindCharacterDataFromPrefab(prefabAsset);
        }

        private void SetPrefabSearchFolder(DefaultAsset folderAsset)
        {
            string selectedPath = folderAsset != null ? AssetDatabase.GetAssetPath(folderAsset) : null;
            string nextPath = ResolveFolderPath(selectedPath, DefaultUnitViewPrefabSearchFolder);
            if (string.Equals(prefabSearchFolderPath, nextPath, StringComparison.Ordinal))
            {
                return;
            }

            prefabSearchFolderPath = nextPath;
            unitViewPrefabCacheDirty = true;
            SaveUnitViewSearchFoldersToCache();
            StartUnitViewPrefabScan(false);
        }

        private void SetSkeletonDataSearchFolder(DefaultAsset folderAsset)
        {
            string selectedPath = folderAsset != null ? AssetDatabase.GetAssetPath(folderAsset) : null;
            string nextPath = ResolveFolderPath(selectedPath, DefaultSkeletonDataSearchFolder);
            if (string.Equals(skeletonDataSearchFolderPath, nextPath, StringComparison.Ordinal))
            {
                return;
            }

            skeletonDataSearchFolderPath = nextPath;
            skeletonDataListDirty = true;
            SaveUnitViewSearchFoldersToCache();
            ReloadSkeletonDataListFromFolder();
        }

        private void DrawSkeletonDataPickerControls()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Choose Skeleton...", secondaryButtonStyle, GUILayout.Height(22f)))
            {
                SkeletonDataPickerWindow.ShowPicker(
                    skeletonDataSearchFolderPath,
                    createSkeletonDataAsset,
                    OnSkeletonDataPickedFromPopup
                );
            }

            if (GUILayout.Button("Reload Cache", secondaryButtonStyle, GUILayout.Width(96f), GUILayout.Height(22f)))
            {
                ReloadSkeletonDataListFromFolder();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Skeleton Cache: {skeletonDataListStatus}", EditorStyles.miniLabel);
        }

        private void OnSkeletonDataPickedFromPopup(SkeletonDataAsset pickedSkeletonData)
        {
            createSkeletonDataAsset = pickedSkeletonData;
            SyncSkeletonDataSelection();
            createUnitAssetStatus = string.Empty;
            createUnitAssetStatusIsError = false;
            Repaint();
        }

        private void DrawSkeletonDataListBrowser()
        {
            if (!skeletonDataListLoaded && skeletonDataListDirty)
            {
                ReloadSkeletonDataListFromFolder();
            }

            EditorGUILayout.BeginVertical(sectionBodyStyle);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Skeleton Library", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reload", secondaryButtonStyle, GUILayout.Width(64f), GUILayout.Height(20f)))
            {
                ReloadSkeletonDataListFromFolder();
            }

            bool hasSelectedSkeleton = createSkeletonDataAsset != null;
            using (new EditorGUI.DisabledScope(!hasSelectedSkeleton))
            {
                if (GUILayout.Button("Clear", secondaryButtonStyle, GUILayout.Width(56f), GUILayout.Height(20f)))
                {
                    createSkeletonDataAsset = null;
                    selectedSkeletonDataListIndex = -1;
                    createUnitAssetStatus = string.Empty;
                    createUnitAssetStatusIsError = false;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search", EditorStyles.miniLabel, GUILayout.Width(42f));
            string nextFilter = EditorGUILayout.TextField(skeletonDataListFilter ?? string.Empty, searchFieldStyle, GUILayout.MinHeight(18f));
            if (!string.Equals(nextFilter, skeletonDataListFilter, StringComparison.Ordinal))
            {
                skeletonDataListFilter = nextFilter;
            }

            if (GUILayout.Button("x", secondaryButtonStyle, GUILayout.Width(22f), GUILayout.Height(18f)))
            {
                skeletonDataListFilter = string.Empty;
            }
            EditorGUILayout.EndHorizontal();

            List<int> filteredIndices = BuildFilteredSkeletonIndices();
            string summary = $"{skeletonDataListStatus} | Showing {filteredIndices.Count}/{skeletonDataFolderAssets.Count}";
            EditorGUILayout.LabelField(summary, EditorStyles.miniLabel);

            if (skeletonDataListDirty && skeletonDataListLoaded)
            {
                EditorGUILayout.LabelField("Skeleton cache is stale and waiting for refresh.", EditorStyles.miniLabel);
            }

            using (var scope = new EditorGUILayout.ScrollViewScope(skeletonDataListScroll, GUILayout.Height(128f)))
            {
                skeletonDataListScroll = scope.scrollPosition;

                if (skeletonDataFolderAssets.Count == 0)
                {
                    string folderLabel = string.IsNullOrEmpty(skeletonDataSearchFolderPath)
                        ? "(Invalid Skeleton folder)"
                        : skeletonDataSearchFolderPath;
                    string message = skeletonDataListLoaded
                        ? $"(No SkeletonDataAsset in {folderLabel})"
                        : "(Skeleton list not loaded)";
                    EditorGUILayout.LabelField(message, EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                    return;
                }

                if (filteredIndices.Count == 0)
                {
                    EditorGUILayout.LabelField("(No result for current filter)", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                    return;
                }

                GUIStyle pathStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    clipping = TextClipping.Clip,
                    normal = { textColor = TextOnSurfaceVariant }
                };

                const float rowHeight = 36f;
                for (int n = 0; n < filteredIndices.Count; n++)
                {
                    int i = filteredIndices[n];
                    SkeletonDataAsset asset = skeletonDataFolderAssets[i];
                    if (asset == null)
                    {
                        continue;
                    }

                    bool isSelected = i == selectedSkeletonDataListIndex || asset == createSkeletonDataAsset;
                    Rect rowRect = GUILayoutUtility.GetRect(1f, rowHeight, GUILayout.ExpandWidth(true));
                    if (isSelected)
                    {
                        EditorGUI.DrawRect(rowRect, new Color(Primary.r, Primary.g, Primary.b, 0.16f));
                    }
                    else if ((n & 1) == 0)
                    {
                        EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.02f));
                    }

                    if (!isSelected && rowRect.Contains(UnityEngine.Event.current.mousePosition))
                    {
                        EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.05f));
                    }

                    string path = i >= 0 && i < skeletonDataFolderPaths.Count ? skeletonDataFolderPaths[i] : string.Empty;
                    string compactPath = GetCompactSkeletonAssetPath(path);
                    Texture icon = AssetDatabase.GetCachedIcon(path);
                    Rect iconRect = new Rect(rowRect.x + 6f, rowRect.y + 8f, 16f, 16f);
                    if (icon != null)
                    {
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                    }

                    float textLeft = iconRect.xMax + 6f;
                    Rect nameRect = new Rect(textLeft, rowRect.y + 3f, rowRect.width - (textLeft - rowRect.x) - 6f, 16f);
                    Rect pathRect = new Rect(textLeft, rowRect.y + 18f, rowRect.width - (textLeft - rowRect.x) - 6f, 14f);
                    GUI.Label(nameRect, asset.name, isSelected ? EditorStyles.miniBoldLabel : EditorStyles.miniLabel);
                    GUI.Label(pathRect, new GUIContent(compactPath, path), pathStyle);

                    if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
                    {
                        selectedSkeletonDataListIndex = i;
                        createSkeletonDataAsset = asset;
                        createUnitAssetStatus = string.Empty;
                        createUnitAssetStatusIsError = false;
                        GUI.FocusControl(null);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void ReloadSkeletonDataListFromFolder()
        {
            if (string.IsNullOrEmpty(skeletonDataSearchFolderPath) || !AssetDatabase.IsValidFolder(skeletonDataSearchFolderPath))
            {
                RebuildSkeletonDataListFromPaths(null);
                skeletonDataListLoaded = false;
                skeletonDataListStatus = "Invalid skeleton folder";
                skeletonDataListDirty = false;
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:SkeletonDataAsset", new[] { skeletonDataSearchFolderPath });
            var paths = new List<string>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
            }

            RebuildSkeletonDataListFromPaths(paths);
            skeletonDataListLoaded = true;
            skeletonDataListDirty = false;
            skeletonDataListStatus = $"Ready ({skeletonDataFolderAssets.Count})";
            UnitAuthoringPrefabCacheState.instance.SaveSkeletonDataCache(skeletonDataFolderPaths);
        }

        private void RestoreSkeletonDataListCache()
        {
            IReadOnlyList<string> cachedPaths = UnitAuthoringPrefabCacheState.instance.GetCachedSkeletonDataPaths();
            if (cachedPaths == null || cachedPaths.Count == 0)
            {
                skeletonDataListLoaded = false;
                skeletonDataListStatus = "Not loaded";
                skeletonDataListDirty = true;
                RebuildSkeletonDataListFromPaths(null);
                return;
            }

            RebuildSkeletonDataListFromPaths(cachedPaths);
            skeletonDataListLoaded = true;
            skeletonDataListStatus = skeletonDataListDirty
                ? $"Ready ({skeletonDataFolderAssets.Count}) - stale"
                : $"Ready ({skeletonDataFolderAssets.Count})";
        }

        private void RebuildSkeletonDataListFromPaths(IEnumerable<string> paths)
        {
            skeletonDataFolderAssets.Clear();
            skeletonDataFolderPaths.Clear();

            if (paths != null)
            {
                foreach (string path in paths)
                {
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }

                    SkeletonDataAsset asset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path);
                    if (asset == null)
                    {
                        continue;
                    }

                    skeletonDataFolderAssets.Add(asset);
                    skeletonDataFolderPaths.Add(path);
                }
            }

            if (skeletonDataFolderAssets.Count > 1)
            {
                var ordered = skeletonDataFolderAssets
                    .Select((asset, index) => new { asset, path = skeletonDataFolderPaths[index] })
                    .OrderBy(x => x.asset.name, StringComparer.Ordinal)
                    .ToList();
                skeletonDataFolderAssets.Clear();
                skeletonDataFolderPaths.Clear();
                for (int i = 0; i < ordered.Count; i++)
                {
                    skeletonDataFolderAssets.Add(ordered[i].asset);
                    skeletonDataFolderPaths.Add(ordered[i].path);
                }
            }

            if (createSkeletonDataAsset == null && skeletonDataFolderAssets.Count > 0)
            {
                createSkeletonDataAsset = skeletonDataFolderAssets[0];
            }

            SyncSkeletonDataSelection();
        }

        private void SyncSkeletonDataSelection()
        {
            selectedSkeletonDataListIndex = -1;
            if (createSkeletonDataAsset == null || skeletonDataFolderAssets.Count == 0)
            {
                return;
            }

            for (int i = 0; i < skeletonDataFolderAssets.Count; i++)
            {
                if (skeletonDataFolderAssets[i] == createSkeletonDataAsset)
                {
                    selectedSkeletonDataListIndex = i;
                    return;
                }
            }
        }

        private List<int> BuildFilteredSkeletonIndices()
        {
            var result = new List<int>(skeletonDataFolderAssets.Count);
            string filter = (skeletonDataListFilter ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(filter))
            {
                for (int i = 0; i < skeletonDataFolderAssets.Count; i++)
                {
                    result.Add(i);
                }

                return result;
            }

            for (int i = 0; i < skeletonDataFolderAssets.Count; i++)
            {
                SkeletonDataAsset asset = skeletonDataFolderAssets[i];
                if (asset == null)
                {
                    continue;
                }

                string path = i >= 0 && i < skeletonDataFolderPaths.Count ? skeletonDataFolderPaths[i] : string.Empty;
                bool matchName = asset.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchPath = !string.IsNullOrEmpty(path) && path.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                if (matchName || matchPath)
                {
                    result.Add(i);
                }
            }

            return result;
        }

        private string GetCompactSkeletonAssetPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return "(No path)";
            }

            string normalized = fullPath.Replace('\\', '/');
            if (!string.IsNullOrEmpty(skeletonDataSearchFolderPath))
            {
                string folderPrefix = skeletonDataSearchFolderPath.Replace('\\', '/').TrimEnd('/') + "/";
                if (normalized.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return normalized.Substring(folderPrefix.Length);
                }
            }

            const int maxLength = 56;
            if (normalized.Length <= maxLength)
            {
                return normalized;
            }

            return "..." + normalized.Substring(normalized.Length - (maxLength - 3));
        }

        private static string ResolveFolderPath(string candidatePath, string fallbackPath)
        {
            if (!string.IsNullOrEmpty(candidatePath) && AssetDatabase.IsValidFolder(candidatePath))
            {
                return candidatePath;
            }

            if (!string.IsNullOrEmpty(fallbackPath) && AssetDatabase.IsValidFolder(fallbackPath))
            {
                return fallbackPath;
            }

            return null;
        }

        private void SaveUnitViewSearchFoldersToCache()
        {
            UnitAuthoringPrefabCacheState.instance.SaveSearchFolders(
                prefabSearchFolderPath,
                characterDataSearchFolderPath,
                skeletonDataSearchFolderPath
            );
        }

        private void EnsureCharacterDataIndex()
        {
            if (!characterDataIndexDirty)
            {
                return;
            }

            characterDataById.Clear();
            characterDataIndexDirty = false;

            if (string.IsNullOrEmpty(characterDataSearchFolderPath) || !AssetDatabase.IsValidFolder(characterDataSearchFolderPath))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:CharacterDataSO", new[] { characterDataSearchFolderPath });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                CharacterDataSO data = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(path);
                if (data == null || characterDataById.ContainsKey(data.id))
                {
                    continue;
                }

                characterDataById.Add(data.id, data);
            }
        }

        private void TryAutoBindCharacterDataFromPrefab(GameObject selectedPrefab)
        {
            unitViewAutoBindStatus = string.Empty;
            if (selectedPrefab == null)
            {
                return;
            }

            UnitView selectedView = selectedPrefab.GetComponentInChildren<UnitView>(true);
            if (selectedView == null)
            {
                unitViewAutoBindStatus = "Selected prefab has no UnitView component.";
                return;
            }

            int unitId = selectedView.AuthoringUnitId;
            if (unitId <= 0)
            {
                unitViewAutoBindStatus = "UnitView AuthoringUnitId is not set (<= 0).";
                return;
            }

            EnsureCharacterDataIndex();
            if (characterDataById.TryGetValue(unitId, out CharacterDataSO matched))
            {
                characterData = matched;
                unitViewAutoBindStatus = $"Auto-bound SO #{unitId}: {matched.name}";
                return;
            }

            string folderLabel = string.IsNullOrEmpty(characterDataSearchFolderPath)
                ? "(Invalid SO folder)"
                : characterDataSearchFolderPath;
            unitViewAutoBindStatus = $"No CharacterDataSO with id={unitId} found in {folderLabel}.";
        }

        private void DrawUnitViewModeSwitcher()
        {
            int selectedMode = GUILayout.Toolbar(
                (int)unitViewBrowserMode,
                UnitViewBrowserModeLabels,
                GUILayout.Height(20f)
            );
            if (selectedMode != (int)unitViewBrowserMode)
            {
                unitViewBrowserMode = (UnitViewBrowserMode)selectedMode;
            }
        }

        private void DrawUnitViewListItems()
        {
            for (int i = 0; i < unitViewPrefabs.Count; i++)
            {
                var prefab = unitViewPrefabs[i];
                if (prefab == null)
                {
                    continue;
                }

                bool isSelected = i == selectedUnitViewListIndex || prefab == prefabAsset;
                var style = isSelected ? tabSelectedStyle : tabNormalStyle;
                if (GUILayout.Button(prefab.name, style))
                {
                    selectedUnitViewListIndex = i;
                    SetPrefabAsset(prefab);
                }
            }
        }

        private void DrawUnitViewDetailItems()
        {
            const float rowHeight = 60f;
            var pathStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = TextOnSurfaceVariant }
            };

            for (int i = 0; i < unitViewPrefabs.Count; i++)
            {
                GameObject prefab = unitViewPrefabs[i];
                if (prefab == null)
                {
                    continue;
                }

                bool isSelected = i == selectedUnitViewListIndex || prefab == prefabAsset;
                Rect rowRect = GUILayoutUtility.GetRect(1f, rowHeight, GUILayout.ExpandWidth(true));

                if (isSelected)
                {
                    EditorGUI.DrawRect(rowRect, new Color(Primary.r, Primary.g, Primary.b, 0.18f));
                }
                else if ((i & 1) == 0)
                {
                    EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.02f));
                }

                if (!isSelected && rowRect.Contains(UnityEngine.Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.05f));
                }

                Rect iconRect = new Rect(rowRect.x + 6f, rowRect.y + 6f, 48f, 48f);
                Texture2D previewTexture = TryGetPrefabPreviewTexture(prefab);
                if (previewTexture != null)
                {
                    GUI.DrawTexture(iconRect, previewTexture, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    EditorGUI.DrawRect(iconRect, new Color(1f, 1f, 1f, 0.06f));
                    GUI.Label(iconRect, "...", EditorStyles.centeredGreyMiniLabel);
                }

                float textLeft = iconRect.xMax + 8f;
                Rect nameRect = new Rect(textLeft, rowRect.y + 9f, rowRect.width - (textLeft - rowRect.x) - 6f, 18f);
                Rect pathRect = new Rect(textLeft, rowRect.y + 29f, rowRect.width - (textLeft - rowRect.x) - 6f, 18f);
                GUI.Label(nameRect, prefab.name, isSelected ? EditorStyles.miniBoldLabel : EditorStyles.miniLabel);
                string path = AssetDatabase.GetAssetPath(prefab);
                GUI.Label(pathRect, string.IsNullOrEmpty(path) ? "(No path)" : path, pathStyle);

                if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
                {
                    selectedUnitViewListIndex = i;
                    SetPrefabAsset(prefab);
                    GUI.FocusControl(null);
                }
            }
        }

        private Texture2D TryGetPrefabPreviewTexture(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            if (unitViewPreviewCache.TryGetValue(prefab, out Texture2D cachedTexture) && cachedTexture != null)
            {
                return cachedTexture;
            }

            Texture2D previewTexture = AssetPreview.GetAssetPreview(prefab);
            Texture2D miniThumbnail = AssetPreview.GetMiniThumbnail(prefab) as Texture2D;
            if (previewTexture != null)
            {
                // Ignore generic mini icon (blue cube) and prefer an actual rendered unit preview.
                if (miniThumbnail == null || previewTexture != miniThumbnail)
                {
                    unitViewPreviewCache[prefab] = previewTexture;
                    return previewTexture;
                }
            }

            previewTexture = RenderPrefabPreviewTexture(prefab, 96);
            if (previewTexture != null)
            {
                unitViewPreviewCache[prefab] = previewTexture;
                return previewTexture;
            }

            if (miniThumbnail != null)
            {
                unitViewPreviewCache[prefab] = miniThumbnail;
                return miniThumbnail;
            }

            if (previewTexture != null)
            {
                unitViewPreviewCache[prefab] = previewTexture;
                return previewTexture;
            }

            return null;
        }

        private Texture2D RenderPrefabPreviewTexture(GameObject prefab, int previewSize)
        {
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(prefabPath))
            {
                return null;
            }

            PreviewRenderUtility previewUtility = null;
            GameObject previewRoot = null;
            try
            {
                previewRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                if (previewRoot == null)
                {
                    return null;
                }

                previewRoot.transform.position = Vector3.zero;
                previewRoot.transform.rotation = Quaternion.identity;

                PrepareSpinePreviewPose(previewRoot);

                if (!TryGetRendererBounds(previewRoot, out Bounds rendererBounds))
                {
                    return null;
                }

                previewUtility = new PreviewRenderUtility(true);
                previewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
                previewUtility.camera.backgroundColor = new Color(0.10f, 0.11f, 0.13f, 1f);
                previewUtility.camera.orthographic = true;
                previewUtility.lights[0].intensity = 1.0f;
                previewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
                previewUtility.lights[1].intensity = 0.75f;
                previewUtility.lights[1].transform.rotation = Quaternion.Euler(340f, 218f, 177f);
                previewUtility.ambientColor = new Color(0.45f, 0.45f, 0.45f, 1f);

                ConfigurePreviewCamera(previewUtility.camera, rendererBounds);
                previewUtility.AddSingleGO(previewRoot);
                previewUtility.BeginStaticPreview(new Rect(0f, 0f, previewSize, previewSize));
                previewUtility.camera.Render();
                Texture2D staticPreview = previewUtility.EndStaticPreview();
                if (staticPreview != null)
                {
                    unitViewGeneratedPreviewTextures.Add(staticPreview);
                }

                return staticPreview;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (previewRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(previewRoot);
                }

                if (previewUtility != null)
                {
                    previewUtility.Cleanup();
                }
            }
        }

        private static void PrepareSpinePreviewPose(GameObject root)
        {
            SkeletonAnimation[] skeletonAnimations = root.GetComponentsInChildren<SkeletonAnimation>(true);
            for (int i = 0; i < skeletonAnimations.Length; i++)
            {
                SkeletonAnimation skeletonAnimation = skeletonAnimations[i];
                if (skeletonAnimation == null)
                {
                    continue;
                }

                skeletonAnimation.Initialize(false);
                if (skeletonAnimation.AnimationState != null && skeletonAnimation.Skeleton != null)
                {
                    Spine.Animation idleAnimation = skeletonAnimation.Skeleton.Data.FindAnimation("idle");
                    if (idleAnimation != null)
                    {
                        skeletonAnimation.AnimationState.SetAnimation(0, idleAnimation, true);
                    }

                    skeletonAnimation.AnimationState.Update(0f);
                    skeletonAnimation.AnimationState.Apply(skeletonAnimation.Skeleton);
                    skeletonAnimation.Skeleton.UpdateWorldTransform();
                }

                skeletonAnimation.LateUpdate();
            }
        }

        private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
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

        private void ClearUnitViewPreviewCache()
        {
            foreach (Texture2D texture in unitViewGeneratedPreviewTextures)
            {
                if (texture != null)
                {
                    DestroyImmediate(texture);
                }
            }

            unitViewGeneratedPreviewTextures.Clear();
            unitViewPreviewCache.Clear();
        }

        private int unitViewPrefabIndex = -1;

        private void HandleProjectChanged()
        {
            unitViewPrefabCacheDirty = true;
            characterDataIndexDirty = true;
            skeletonDataListDirty = true;
            UnitAuthoringPrefabCacheState.instance.MarkDirty();

            if (unitViewPrefabListLoaded && !isUnitViewPrefabScanRunning)
            {
                StartUnitViewPrefabScan(true);
            }

            if (skeletonDataListLoaded)
            {
                ReloadSkeletonDataListFromFolder();
            }
        }

        private void RestoreUnitViewPrefabCache()
        {
            var cache = UnitAuthoringPrefabCacheState.instance;
            unitViewPrefabCacheDirty = cache.IsDirty;
            unitViewPrefabScanFullProject = false;
            prefabSearchFolderPath = ResolveFolderPath(cache.PrefabSearchFolderPath, DefaultUnitViewPrefabSearchFolder);
            characterDataSearchFolderPath = ResolveFolderPath(cache.CharacterDataSearchFolderPath, DefaultCharacterDataSearchFolder);
            skeletonDataSearchFolderPath = ResolveFolderPath(cache.SkeletonDataSearchFolderPath, DefaultSkeletonDataSearchFolder);
            skeletonDataListDirty = true;
            SaveUnitViewSearchFoldersToCache();
            characterDataIndexDirty = true;
            RestoreSkeletonDataListCache();

            IReadOnlyList<string> cachedPaths = cache.GetCachedPrefabPaths();
            if (cachedPaths == null || cachedPaths.Count == 0)
            {
                unitViewPrefabListLoaded = false;
                unitViewPrefabScanStatus = "Not loaded";
                return;
            }

            RebuildUnitViewPrefabListFromPaths(cachedPaths, false);
            unitViewPrefabListLoaded = true;
            unitViewPrefabScanStatus = unitViewPrefabCacheDirty
                ? $"Ready ({unitViewPrefabs.Count}) - stale"
                : $"Ready ({unitViewPrefabs.Count})";

            if (unitViewPrefabCacheDirty)
            {
                StartUnitViewPrefabScan(true);
            }
        }

        private void StartUnitViewPrefabScan(bool silentStatus)
        {
            scannedUnitViewPrefabPaths.Clear();
            scannedUnitViewPrefabPathSet.Clear();

            string[] searchFolders = GetSelectedUnitViewSearchFolders();
            string[] guids = searchFolders == null
                ? Array.Empty<string>()
                : AssetDatabase.FindAssets("t:Prefab", searchFolders);

            unitViewPrefabScanGuids = guids ?? Array.Empty<string>();
            unitViewPrefabScanIndex = 0;
            unitViewPrefabScanTotal = unitViewPrefabScanGuids.Length;
            unitViewPrefabScanFullProject = false;
            isUnitViewPrefabScanRunning = true;

            if (!silentStatus)
            {
                unitViewPrefabScanStatus = searchFolders == null
                    ? "Invalid prefab folder"
                    : unitViewPrefabScanTotal == 0
                    ? "No prefabs found for scan"
                    : "Loading...";
            }

            if (unitViewPrefabScanTotal == 0)
            {
                CompleteUnitViewPrefabScan();
            }

            Repaint();
        }

        private void ProcessUnitViewPrefabScan()
        {
            if (!isUnitViewPrefabScanRunning)
            {
                return;
            }

            double startTime = EditorApplication.timeSinceStartup;
            int processedCount = 0;
            while (unitViewPrefabScanIndex < unitViewPrefabScanTotal)
            {
                if (processedCount >= UnitViewPrefabScanBatchSize)
                {
                    break;
                }

                if (EditorApplication.timeSinceStartup - startTime >= UnitViewPrefabScanFrameBudgetSeconds)
                {
                    break;
                }

                processedCount++;
                string guid = unitViewPrefabScanGuids[unitViewPrefabScanIndex++];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                if (
                    prefab.GetComponentInChildren<UnitView>(true) != null
                    && scannedUnitViewPrefabPathSet.Add(path)
                )
                {
                    scannedUnitViewPrefabPaths.Add(path);
                }
            }

            if (unitViewPrefabScanTotal > 0)
            {
                unitViewPrefabScanStatus = $"Loading {unitViewPrefabScanIndex}/{unitViewPrefabScanTotal}";
            }

            if (unitViewPrefabScanIndex >= unitViewPrefabScanTotal)
            {
                CompleteUnitViewPrefabScan();
            }
            else if (processedCount > 0)
            {
                Repaint();
            }
        }

        private void CompleteUnitViewPrefabScan()
        {
            isUnitViewPrefabScanRunning = false;
            RebuildUnitViewPrefabListFromPaths(scannedUnitViewPrefabPaths, false);
            unitViewPrefabListLoaded = true;
            unitViewPrefabCacheDirty = false;
            unitViewPrefabScanStatus = $"Ready ({unitViewPrefabs.Count})";

            UnitAuthoringPrefabCacheState.instance.SaveCache(
                scannedUnitViewPrefabPaths,
                unitViewPrefabScanFullProject
            );

            unitViewPrefabScanGuids = Array.Empty<string>();
            scannedUnitViewPrefabPaths.Clear();
            scannedUnitViewPrefabPathSet.Clear();
            Repaint();
        }

        private void StopUnitViewPrefabScan()
        {
            isUnitViewPrefabScanRunning = false;
            unitViewPrefabScanGuids = Array.Empty<string>();
            unitViewPrefabScanIndex = 0;
            unitViewPrefabScanTotal = 0;
            scannedUnitViewPrefabPaths.Clear();
            scannedUnitViewPrefabPathSet.Clear();
            unitViewPrefabScanStatus = unitViewPrefabListLoaded
                ? $"Ready ({unitViewPrefabs.Count})"
                : "Not loaded";
            Repaint();
        }

        private string[] GetSelectedUnitViewSearchFolders()
        {
            if (string.IsNullOrEmpty(prefabSearchFolderPath) || !AssetDatabase.IsValidFolder(prefabSearchFolderPath))
            {
                return null;
            }

            return new[] { prefabSearchFolderPath };
        }

        private void RebuildUnitViewPrefabListFromPaths(IEnumerable<string> prefabPaths, bool validateUnitView)
        {
            ClearUnitViewPreviewCache();
            unitViewPrefabs.Clear();
            if (prefabPaths != null)
            {
                foreach (string path in prefabPaths)
                {
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }

                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null)
                    {
                        continue;
                    }

                    if (validateUnitView && prefab.GetComponentInChildren<UnitView>(true) == null)
                    {
                        continue;
                    }

                    unitViewPrefabs.Add(prefab);
                }
            }

            unitViewPrefabs.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            unitViewPrefabLabels = BuildPrefabLabels(unitViewPrefabs);
            SyncUnitViewPrefabSelection();
        }

        private static string[] BuildPrefabLabels(IReadOnlyList<GameObject> prefabs)
        {
            var labels = new List<string>(prefabs.Count);
            for (int i = 0; i < prefabs.Count; i++)
            {
                var prefab = prefabs[i];
                string path = AssetDatabase.GetAssetPath(prefab);
                labels.Add(string.IsNullOrEmpty(path) ? prefab.name : $"{prefab.name} ({path})");
            }

            return labels.ToArray();
        }

        private void SyncUnitViewPrefabSelection()
        {
            unitViewPrefabIndex = -1;
            selectedUnitViewListIndex = -1;
            if (prefabAsset == null || unitViewPrefabs.Count == 0)
            {
                return;
            }

            for (int i = 0; i < unitViewPrefabs.Count; i++)
            {
                if (unitViewPrefabs[i] == prefabAsset)
                {
                    unitViewPrefabIndex = i;
                    selectedUnitViewListIndex = i;
                    return;
                }
            }
        }

        private void EnsureWorkingPrefabLoaded(bool includeSkeletonMetadata)
        {
            if (prefabAsset == null)
            {
                if (workingPrefabRoot != null)
                {
                    UnloadPrefabWorkingCopy();
                }

                return;
            }

            if (workingPrefabRoot == null)
            {
                LoadPrefabWorkingCopy(prefabAsset, includeSkeletonMetadata);
                return;
            }

            if (includeSkeletonMetadata)
            {
                EnsureSkeletonMetadataLoaded();
            }
        }

        private void LoadPrefabWorkingCopy(GameObject sourcePrefab, bool includeSkeletonMetadata)
        {
            UnloadPrefabWorkingCopy();

            if (sourcePrefab == null)
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(sourcePrefab);
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            workingPrefabPath = assetPath;
            workingPrefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
            RefreshPrefabCache(includeSkeletonMetadata);
        }

        private void RefreshPrefabCache(bool includeSkeletonMetadata)
        {
            animationNames.Clear();
            eventNames.Clear();
            animationInfos.Clear();
            skeletonMetadataLoaded = false;

            if (workingPrefabRoot == null)
            {
                skeletonAnimation = null;
                skeletonDataAsset = null;
                actionSequenceRunner = null;
                behitBehavior = null;
                animationHandle = null;
                unitView = null;
                return;
            }

            skeletonAnimation = workingPrefabRoot.GetComponentInChildren<SkeletonAnimation>(true);
            skeletonDataAsset = skeletonAnimation != null ? skeletonAnimation.skeletonDataAsset : null;
            actionSequenceRunner = workingPrefabRoot.GetComponentInChildren<ActionSequenceRunner>(true);
            behitBehavior = workingPrefabRoot.GetComponentInChildren<BehitBehavior>(true);
            animationHandle = workingPrefabRoot.GetComponentInChildren<AnimationHandle>(true);
            unitView = workingPrefabRoot.GetComponentInChildren<UnitView>(true);

            if (includeSkeletonMetadata)
            {
                EnsureSkeletonMetadataLoaded();
            }
        }

        private void EnsureSkeletonMetadataLoaded()
        {
            if (skeletonMetadataLoaded)
            {
                return;
            }

            animationNames.Clear();
            eventNames.Clear();
            animationInfos.Clear();

            if (skeletonDataAsset != null)
            {
                try
                {
                    SkeletonData skeletonData = skeletonDataAsset.GetSkeletonData(true);
                    if (skeletonData != null)
                    {
                        animationInfos.Clear();
                        foreach (var anim in skeletonData.Animations)
                        {
                            animationNames.Add(anim.Name);

                            var animEventCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                            var animEventOrder = new List<string>();
                            foreach (var timeline in anim.Timelines)
                            {
                                if (timeline is EventTimeline eventTimeline)
                                {
                                    foreach (var e in eventTimeline.Events)
                                    {
                                        string eventName = e?.Data?.Name;
                                        if (string.IsNullOrEmpty(eventName))
                                        {
                                            continue;
                                        }

                                        if (animEventCounts.TryGetValue(eventName, out int count))
                                        {
                                            animEventCounts[eventName] = count + 1;
                                        }
                                        else
                                        {
                                            animEventCounts[eventName] = 1;
                                            animEventOrder.Add(eventName);
                                        }
                                    }
                                }
                            }

                            animationInfos.Add(new AnimationInfo
                            {
                                Name = anim.Name,
                                Duration = anim.Duration,
                                EventNames = animEventOrder.Count > 0
                                    ? string.Join(", ", animEventOrder.Select(name => $"{name}(x {animEventCounts[name]})"))
                                    : "-"
                            });
                        }
                        eventNames.AddRange(skeletonData.Events.Select(e => e.Name));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UnitAuthoringWindow] Failed to read skeleton data: {ex.Message}");
                }
            }

            skeletonMetadataLoaded = true;
        }

        private void SavePrefabWorkingCopy()
        {
            if (workingPrefabRoot == null || string.IsNullOrEmpty(workingPrefabPath))
            {
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(workingPrefabRoot, workingPrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void SaveAll()
        {
            SavePrefabWorkingCopy();
            SyncAssetNames(DeriveUnitNameForSaveAll());
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private Texture2D MakeGradientTex(int width, int height, Color leftColor, Color rightColor)
        {
            Color[] pix = new Color[width * height];
            for (int x = 0; x < width; x++)
            {
                float t = (float)x / (width > 1 ? width - 1 : 1);
                Color col = Color.Lerp(leftColor, rightColor, t);
                for (int y = 0; y < height; y++)
                {
                    pix[y * width + x] = col;
                }
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void BuildStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            texSurfaceLowest = MakeTex(2, 2, SurfaceContainerLowest);
            texSurfaceLow = MakeTex(2, 2, SurfaceContainerLow);
            texSurfaceHigh = MakeTex(2, 2, SurfaceContainerHigh);
            texSurfaceHighest = MakeTex(2, 2, SurfaceContainerHighest);
            texPrimaryGradient = MakeGradientTex(64, 1, Primary, PrimaryContainer);
            texSecondaryContainer = MakeTex(2, 2, SecondaryContainer);

            iconAssetSetup = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/UnitAuthoring/Icons/inventory_2.png");
            iconCharacterData = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/UnitAuthoring/Icons/person_outline.png");
            iconSkeletonData = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/UnitAuthoring/Icons/settings_accessibility.png");
            iconPrefabAuthoring = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/UnitAuthoring/Icons/account_tree.png");
            iconSkillSequences = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/UnitAuthoring/Icons/event_repeat.png");
            iconSkillPreview = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/UnitAuthoring/Icons/play_circle_outline.png");
            tabIcons = new Texture2D[] { iconAssetSetup, iconCharacterData, iconSkeletonData, iconPrefabAuthoring, iconSkillSequences, iconSkillPreview };

            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = TextOnSurface }
            };

            subtitleStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                wordWrap = true,
                normal = { textColor = TextOnSurfaceVariant }
            };

            sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = TextOnSurface }
            };

            sectionBodyStyle = new GUIStyle()
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 4, 4),
                normal = { background = texSurfaceLowest }
            };

            cardStyle = new GUIStyle()
            {
                padding = new RectOffset(16, 16, 16, 16),
                margin = new RectOffset(4, 4, 4, 4),
                normal = { background = texSurfaceLow }
            };

            chipStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 24f,
                padding = new RectOffset(8, 8, 4, 4)
            };

            chipStyleSmall = new GUIStyle(chipStyle)
            {
                fixedHeight = 20f
            };

            primaryButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 28f,
                padding = new RectOffset(10, 10, 6, 6),
                normal = { background = texPrimaryGradient, textColor = new Color(0.2f, 0.04f, 0) },
                hover = { background = texPrimaryGradient, textColor = new Color(0, 0, 0) },
                active = { background = texPrimaryGradient, textColor = new Color(0.1f, 0.02f, 0) },
                border = new RectOffset(4, 4, 4, 4)
            };

            secondaryButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 26f,
                padding = new RectOffset(10, 10, 5, 5),
                normal = { background = texSecondaryContainer, textColor = TextOnSurface },
                hover = { background = texSurfaceHighest, textColor = TextOnSurface },
                border = new RectOffset(4, 4, 4, 4)
            };

            dangerButtonStyle = new GUIStyle(secondaryButtonStyle);

            previewIconButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 18f,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            panelLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                normal = { textColor = TextOnSurfaceVariant }
            };

            searchFieldStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = 11,
                fixedHeight = 18f,
                normal = { background = texSurfaceHighest, textColor = TextOnSurface }
            };

            tabNormalStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = TextOnSurfaceVariant },
                hover = { textColor = TextOnSurface }
            };

            tabSelectedStyle = new GUIStyle(tabNormalStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Primary },
                hover = { textColor = Primary }
            };
        }

        private void DrawStatusChip(string label, string value, Color background)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = background;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.Height(26f));
            GUILayout.Label($"{label}: {value}", chipStyleSmall);
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = previous;
        }

        private void DrawKeyValueRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, panelLabelStyle, GUILayout.Width(110f));
            EditorGUILayout.LabelField(string.IsNullOrEmpty(value) ? "-" : value, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private static string[] GetSortingLayerNames()
        {
            var layers = SortingLayer.layers;
            if (layers == null || layers.Length == 0)
            {
                return new[] { "Default" };
            }

            var names = new string[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                names[i] = layers[i].name;
            }

            return names;
        }

        private void UnloadPrefabWorkingCopy()
        {
            ClearPreviewAnimationOverride(false);

            if (workingPrefabRoot != null)
            {
                if (Selection.activeGameObject != null && (Selection.activeGameObject == workingPrefabRoot || Selection.activeGameObject.transform.IsChildOf(workingPrefabRoot.transform)))
                {
                    Selection.activeObject = null;
                }
                PrefabUtility.UnloadPrefabContents(workingPrefabRoot);
            }

            workingPrefabRoot = null;
            workingPrefabPath = null;
            skeletonAnimation = null;
            skeletonDataAsset = null;
            actionSequenceRunner = null;
            behitBehavior = null;
            animationHandle = null;
            unitView = null;
            animationNames.Clear();
            eventNames.Clear();
            animationInfos.Clear();
            skeletonMetadataLoaded = false;

            if (previewController != null)
            {
                previewBoundPrefab = null;
                previewBoundSequence = null;
                previewController.BindPrefab(null);
            }

            if (skeletonPreviewController != null)
            {
                skeletonPreviewBoundPrefab = null;
                skeletonPreviewController.BindPrefab(null);
            }

            draggedPreviewMarkerName = null;
        }
    }
}

#endif
