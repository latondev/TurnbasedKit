#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static readonly Color AccentColor = new Color(0.98f, 0.56f, 0.24f);
        private static readonly Color AccentSoftColor = new Color(0.98f, 0.56f, 0.24f, 0.16f);
        private static readonly Color PanelColor = new Color(0.14f, 0.15f, 0.18f, 1f);
        private static readonly Color PanelAltColor = new Color(0.17f, 0.18f, 0.22f, 1f);
        private static readonly Color GoodColor = new Color(0.28f, 0.72f, 0.38f);
        private static readonly Color WarnColor = new Color(0.88f, 0.62f, 0.18f);
        private static readonly Color BadColor = new Color(0.82f, 0.28f, 0.28f);

        [SerializeField] private CharacterDataSO characterData;
        [SerializeField] private GameObject prefabAsset;
        [SerializeField] private Vector2 scrollPosition;
        [SerializeField] private int currentTab = 0;
        private readonly string[] tabNames = { "Asset Setup", "Character Data", "Skeleton Data", "Prefab Authoring", "Skill sequences", "Asset Previews" };
        [SerializeField] private string skillSearchFilter = string.Empty;
        [SerializeField] private string animationSearchFilter = string.Empty;
        [SerializeField] private string eventSearchFilter = string.Empty;

        private GameObject workingPrefabRoot;
        private string workingPrefabPath;
        private SkeletonAnimation skeletonAnimation;
        private SkeletonDataAsset skeletonDataAsset;
        private AttackBehavior attackBehavior;
        private SkillBehavior skillBehavior;
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
        [MenuItem("Tools/Battle/Unit Authoring")]
        public static void ShowWindow()
        {
            var window = GetWindow<UnitAuthoringWindow>("Unit Authoring");
            window.minSize = new Vector2(980f, 760f);
        }

        private void OnEnable()
        {
            if (prefabAsset != null && workingPrefabRoot == null)
            {
                LoadPrefabWorkingCopy(prefabAsset);
            }
        }

        private void OnDisable()
        {
            UnloadPrefabWorkingCopy();
        }

        private void OnGUI()
        {
            BuildStyles();
            DrawHeader();

            EditorGUILayout.Space(6f);

            if (workingPrefabRoot == null && prefabAsset != null)
            {
                LoadPrefabWorkingCopy(prefabAsset);
            }

            EditorGUILayout.BeginHorizontal();

            DrawSidebar();

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            switch (currentTab)
            {
                case 0: DrawAssetBindingTab(); break;
                case 1: DrawCharacterSection(); break;
                case 2: DrawSkeletonSection(); break;
                case 3: DrawPrefabSection(); break;
                case 4: DrawSkillSequencesSection(); break;
                case 5: DrawPreviewsTab(); break;
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);
            DrawBottomActions();
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
                if (isSelected)
                {
                    EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.05f));
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), AccentColor);
                }

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    currentTab = i;
                    GUI.FocusControl(null);
                }

                GUIStyle labelStyle = isSelected ? tabSelectedStyle : tabNormalStyle;
                Rect textRect = new Rect(rect.x + (isSelected ? 12f : 8f), rect.y, rect.width - 8f, rect.height);
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
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("Asset Setup", sectionHeaderStyle);
            EditorGUILayout.LabelField(
                "Bind one CharacterDataSO and one prefab, then keep both assets synced to the same unit id.",
                subtitleStyle);
            EditorGUILayout.Space(12f);

            EditorGUI.BeginChangeCheck();
            var nextCharacterData = (CharacterDataSO)EditorGUILayout.ObjectField("Character Data SO", characterData, typeof(CharacterDataSO), false);
            if (EditorGUI.EndChangeCheck())
            {
                characterData = nextCharacterData;
            }

            EditorGUI.BeginChangeCheck();
            var nextPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab Asset", prefabAsset, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                SetPrefabAsset(nextPrefab);
            }

            EditorGUILayout.Space(12f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Selection", primaryButtonStyle))
            {
                UseSelection();
            }

            if (GUILayout.Button("Create Character SO", secondaryButtonStyle))
            {
                CreateCharacterDataAsset();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Sync Asset Names", secondaryButtonStyle))
            {
                SyncAssetNames();
            }

            if (GUILayout.Button("Reload Prefab", secondaryButtonStyle) && prefabAsset != null)
            {
                LoadPrefabWorkingCopy(prefabAsset);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Character Id", GUILayout.Width(80f));
            if (characterData != null)
            {
                EditorGUILayout.LabelField(characterData.id.ToString(), EditorStyles.miniBoldLabel);
            }
            else
            {
                EditorGUILayout.LabelField("-", EditorStyles.miniBoldLabel);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSkeletonSection()
        {
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("Skeleton Data", sectionHeaderStyle);
            EditorGUILayout.LabelField("Source animations and Spine events are read from the prefab's SkeletonAnimation.", subtitleStyle);
            EditorGUILayout.Space(4f);
            if (skeletonAnimation == null)
            {
                EditorGUILayout.HelpBox("No SkeletonAnimation found in the selected prefab.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            string skeletonDataAssetName = skeletonDataAsset != null ? skeletonDataAsset.name : "None";
            EditorGUILayout.LabelField($"Skeleton Host: {skeletonAnimation.name}", sectionHeaderStyle);
            DrawKeyValueRow("Skeleton Data", skeletonDataAssetName);
            DrawKeyValueRow("Animation Count", animationNames.Count.ToString());
            DrawKeyValueRow("Event Count", eventNames.Count.ToString());
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(12f);

            EditorGUILayout.BeginVertical(GUILayout.Width(290f));
            DrawAssetPreviewPanel(
                "Skeleton Preview",
                skeletonDataAsset != null ? skeletonDataAsset : skeletonAnimation,
                skeletonDataAsset != null ? skeletonDataAsset.name : skeletonAnimation.name,
                skeletonDataAsset != null ? $"Animations: {animationNames.Count}" : $"Events: {eventNames.Count}");
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);

            // ANIMATIONS DETAILS
            EditorGUILayout.LabelField("Animations Detail", sectionHeaderStyle);
            EditorGUILayout.LabelField("Available animations, duration, and what components/skills use them.", subtitleStyle);
            EditorGUILayout.Space(4f);

            BuildAnimationUsageCache();

            Rect listRect = EditorGUILayout.BeginVertical(sectionBodyStyle);
            
            // Draw table header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Animation Name", EditorStyles.miniBoldLabel, GUILayout.Width(160f));
            EditorGUILayout.LabelField("Duration", EditorStyles.miniBoldLabel, GUILayout.Width(60f));
            EditorGUILayout.LabelField("Events", EditorStyles.miniBoldLabel, GUILayout.Width(120f));
            EditorGUILayout.LabelField("Usage Info", EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
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
                Rect useRect = new Rect(rowRect.x + 348f, rowRect.y + 2f, rowRect.width - 348f, 18f);

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
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8f);
            DrawPreviewList("Event Preview", eventNames, 20);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private void BuildAnimationUsageCache()
        {
            animationUsageCache.Clear();
            if (animationInfos.Count == 0) return;

            var attackSo = attackBehavior != null ? new SerializedObject(attackBehavior) : null;
            var skillSo = skillBehavior != null ? new SerializedObject(skillBehavior) : null;
            var behitSo = behitBehavior != null ? new SerializedObject(behitBehavior) : null;

            SerializedObject charSo = null;
            if (characterData != null) charSo = new SerializedObject(characterData);

            List<SerializedObject> seqBasicSOs = GetSeqSOs(charSo, "skillBasic");
            List<SerializedObject> seqUltiSOs = GetSeqSOs(charSo, "skillUltimate");
            List<SerializedObject> seqPassSOs = GetSeqSOs(charSo, "skillPassive");
            List<SerializedObject> seqAwakSOs = GetSeqSOs(charSo, "skillAwaken");

            foreach (var anim in animationInfos)
            {
                var usages = new List<string>();

                if (attackSo != null)
                {
                    CheckProp(usages, anim.Name, attackSo, "attackAnimation", "AttackBehavior(Attack)");
                    CheckProp(usages, anim.Name, attackSo, "idleAnimation", "AttackBehavior(Idle)");
                }

                if (skillSo != null)
                {
                    CheckProp(usages, anim.Name, skillSo, "skillAnimation", "SkillBehavior(Skill)");
                    CheckProp(usages, anim.Name, skillSo, "idleAnimation", "SkillBehavior(Idle)");
                    CheckProp(usages, anim.Name, skillSo, "moveGo", "SkillBehavior(MoveGo)");
                    CheckProp(usages, anim.Name, skillSo, "moveBack", "SkillBehavior(MoveBack)");
                }

                if (behitSo != null)
                {
                    CheckProp(usages, anim.Name, behitSo, "behitAnimation", "BehitBehavior(Behit)");
                    CheckProp(usages, anim.Name, behitSo, "dieAnimation", "BehitBehavior(Die)");
                    CheckProp(usages, anim.Name, behitSo, "downAnimation", "BehitBehavior(Down)");
                    CheckProp(usages, anim.Name, behitSo, "upAnimation", "BehitBehavior(Up)");
                    CheckProp(usages, anim.Name, behitSo, "idleAnimation", "BehitBehavior(Idle)");
                }

                CheckSeqAnimList(usages, anim.Name, seqBasicSOs, "Basic Skill");
                CheckSeqAnimList(usages, anim.Name, seqUltiSOs, "Ultimate Skill");
                CheckSeqAnimList(usages, anim.Name, seqPassSOs, "Passive Skill");
                CheckSeqAnimList(usages, anim.Name, seqAwakSOs, "Awaken Skill");

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
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("Character Data", sectionHeaderStyle);
            EditorGUILayout.LabelField("Edit stats, identity, and skill slots directly from the linked SO.", subtitleStyle);
            EditorGUILayout.Space(4f);
            if (characterData == null)
            {
                EditorGUILayout.HelpBox("Assign a CharacterDataSO to edit unit data.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            var characterSo = new SerializedObject(characterData);
            characterSo.Update();

            EditorGUILayout.BeginHorizontal();
            DrawCharacterProperty(characterSo, "id", "Id");
            DrawCharacterProperty(characterSo, "level", "Level");
            DrawCharacterProperty(characterSo, "isUnlock", "Unlocked");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawCharacterProperty(characterSo, "nameHero", "Name");
            DrawCharacterProperty(characterSo, "type", "Type");
            DrawCharacterProperty(characterSo, "rarity", "Rarity");
            EditorGUILayout.EndHorizontal();

            DrawCharacterProperty(characterSo, "stats", "Stats");

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Skills", sectionHeaderStyle);
            DrawSkillProperty(characterSo, "skillBasic", "Basic Skill");
            DrawSkillProperty(characterSo, "skillUltimate", "Ultimate Skill");
            DrawSkillProperty(characterSo, "skillPassive", "Passive Skill");
            DrawSkillProperty(characterSo, "skillAwaken", "Awaken Skill");

            if (characterSo.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(characterData);
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
        private SkillViewSequence selectedLibrarySequence;
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

            var stepsProp = sequenceSo.FindProperty("steps");
            if (stepsProp != null)
            {
                EditorGUILayout.PropertyField(stepsProp, true);
            }

            if (sequenceSo.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(sequence);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawComponentStringSection()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Component Mapping", sectionHeaderStyle);

            DrawComponentStrings("Attack Behavior", attackBehavior, new[]
            {
                "attackAnimation", "idleAnimation", "eventHit"
            });

            DrawComponentStrings("Skill Behavior", skillBehavior, new[]
            {
                "skillAnimation", "idleAnimation", "eventHit", "eventFalldown", "moveGo", "moveBack"
            });

            DrawComponentStrings("Behit Behavior", behitBehavior, new[]
            {
                "behitAnimation", "dieAnimation", "downAnimation", "upAnimation", "idleAnimation"
            });
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
                sortingLayerName.stringValue = EditorGUILayout.TextField("Sorting Layer", sortingLayerName.stringValue);
            }

            var sortingOrder = handleSo.FindProperty("sortingOrder");
            if (sortingOrder != null)
            {
                sortingOrder.intValue = EditorGUILayout.IntField("Sorting Order", sortingOrder.intValue);
            }

            if (handleSo.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(animationHandle);
            }
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
                property.stringValue = EditorGUILayout.TextField(label, property.stringValue);
                return;
            }

            var popupOptions = BuildPopupOptions(options, property.stringValue, filter);
            int currentIndex = Array.IndexOf(popupOptions, property.stringValue);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = EditorGUILayout.Popup(label, currentIndex, popupOptions);
            if (nextIndex >= 0 && nextIndex < popupOptions.Length)
            {
                property.stringValue = nextIndex == 0 ? string.Empty : popupOptions[nextIndex];
            }
        }

        private string[] BuildPopupOptions(IReadOnlyList<string> options, string currentValue, string filter = null)
        {
            var list = new List<string> { "<None>" };
            string normalizedFilter = string.IsNullOrWhiteSpace(filter) ? string.Empty : filter.Trim();

            if (!string.IsNullOrWhiteSpace(currentValue) && !list.Contains(currentValue))
            {
                list.Add(currentValue);
            }

            foreach (var option in options)
            {
                if (string.IsNullOrWhiteSpace(option))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(normalizedFilter) && option.IndexOf(normalizedFilter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (!list.Contains(option))
                {
                    list.Add(option);
                }
            }

            return list.ToArray();
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



        private void DrawPreviewsTab()
        {
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("Asset Previews", sectionHeaderStyle);
            EditorGUILayout.LabelField("Large previews of the currently bound assets.", subtitleStyle);
            EditorGUILayout.Space(8f);

            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            DrawAssetPreviewPanel(
                "Character Data SO",
                characterData,
                characterData != null ? characterData.name : "No CharacterDataSO selected",
                characterData != null ? $"Id: {characterData.id}" : null,
                256f);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(12f);

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            DrawAssetPreviewPanel(
                "Prefab Asset",
                prefabAsset,
                prefabAsset != null ? prefabAsset.name : "No prefab selected",
                prefabAsset != null ? workingPrefabPath : null,
                256f);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
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

        private void UseSelection()
        {
            var selected = Selection.activeObject;
            if (selected is CharacterDataSO selectedCharacterData)
            {
                characterData = selectedCharacterData;
            }

            if (selected is GameObject selectedPrefab && PrefabUtility.GetPrefabAssetType(selectedPrefab) != PrefabAssetType.NotAPrefab)
            {
                SetPrefabAsset(selectedPrefab);
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



        private void SyncAssetNames()
        {
            if (workingPrefabRoot != null)
            {
                SavePrefabWorkingCopy();
            }

            if (characterData != null)
            {
                SyncCharacterAssetName(characterData);
            }

            if (prefabAsset != null)
            {
                SyncPrefabAssetName(prefabAsset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void SyncCharacterAssetName(CharacterDataSO data)
        {
            if (data == null)
            {
                return;
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

        private void SyncPrefabAssetName(GameObject prefab)
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
                ? GetPrefabAssetName(characterData)
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

            string safeName = string.IsNullOrWhiteSpace(data.nameHero) ? "Character" : data.nameHero.Trim();
            return $"{data.id}_{safeName}";
        }

        private static string GetPrefabAssetName(CharacterDataSO data)
        {
            if (data == null)
            {
                return "CharacterPrefab";
            }

            string safeName = string.IsNullOrWhiteSpace(data.nameHero) ? "Character" : data.nameHero.Trim();
            return $"{data.id}_{safeName}_Unit";
        }

        private void SetPrefabAsset(GameObject newPrefab)
        {
            if (prefabAsset == newPrefab && workingPrefabRoot != null)
            {
                return;
            }

            prefabAsset = newPrefab;
            LoadPrefabWorkingCopy(newPrefab);
        }

        private void LoadPrefabWorkingCopy(GameObject sourcePrefab)
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
            RefreshPrefabCache();
        }

        private void RefreshPrefabCache()
        {
            animationNames.Clear();
            eventNames.Clear();

            if (workingPrefabRoot == null)
            {
                skeletonAnimation = null;
                skeletonDataAsset = null;
                attackBehavior = null;
                skillBehavior = null;
                behitBehavior = null;
                animationHandle = null;
                unitView = null;
                return;
            }

            skeletonAnimation = workingPrefabRoot.GetComponentInChildren<SkeletonAnimation>(true);
            skeletonDataAsset = skeletonAnimation != null ? skeletonAnimation.skeletonDataAsset : null;
            attackBehavior = workingPrefabRoot.GetComponentInChildren<AttackBehavior>(true);
            skillBehavior = workingPrefabRoot.GetComponentInChildren<SkillBehavior>(true);
            behitBehavior = workingPrefabRoot.GetComponentInChildren<BehitBehavior>(true);
            animationHandle = workingPrefabRoot.GetComponentInChildren<AnimationHandle>(true);
            unitView = workingPrefabRoot.GetComponentInChildren<UnitView>(true);

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

                            var animEvents = new List<string>();
                            foreach (var timeline in anim.Timelines)
                            {
                                if (timeline is EventTimeline eventTimeline)
                                {
                                    foreach (var e in eventTimeline.Events)
                                    {
                                        if (!animEvents.Contains(e.Data.Name))
                                        {
                                            animEvents.Add(e.Data.Name);
                                        }
                                    }
                                }
                            }

                            animationInfos.Add(new AnimationInfo
                            {
                                Name = anim.Name,
                                Duration = anim.Duration,
                                EventNames = animEvents.Count > 0 ? string.Join(", ", animEvents) : "-"
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
            SyncAssetNames();
        }

        private void BuildStyles()
        {
            if (titleStyle != null
                && subtitleStyle != null
                && sectionHeaderStyle != null
                && sectionBodyStyle != null
                && cardStyle != null
                && chipStyle != null
                && chipStyleSmall != null
                && primaryButtonStyle != null
                && secondaryButtonStyle != null
                && dangerButtonStyle != null
                && panelLabelStyle != null
                && searchFieldStyle != null
                && tabNormalStyle != null
                && tabSelectedStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = AccentColor }
            };

            subtitleStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                wordWrap = true,
                normal = { textColor = new Color(0.86f, 0.88f, 0.92f) }
            };

            sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.95f, 0.98f) }
            };

            sectionBodyStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 4, 4)
            };

            cardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(16, 16, 16, 16),
                margin = new RectOffset(4, 4, 4, 4)
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

            primaryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fixedHeight = 28f,
                padding = new RectOffset(10, 10, 6, 6)
            };

            secondaryButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontStyle = FontStyle.Bold,
                fixedHeight = 26f,
                padding = new RectOffset(10, 10, 5, 5)
            };

            dangerButtonStyle = new GUIStyle(secondaryButtonStyle);

            panelLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.76f, 0.84f) }
            };

            searchFieldStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = 11,
                fixedHeight = 18f
            };

            tabNormalStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.65f, 0.7f, 0.75f) },
                hover = { textColor = new Color(0.9f, 0.95f, 1f) }
            };

            tabSelectedStyle = new GUIStyle(tabNormalStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = AccentColor },
                hover = { textColor = AccentColor }
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

        private void UnloadPrefabWorkingCopy()
        {
            if (workingPrefabRoot != null)
            {
                PrefabUtility.UnloadPrefabContents(workingPrefabRoot);
            }

            workingPrefabRoot = null;
            workingPrefabPath = null;
            skeletonAnimation = null;
            skeletonDataAsset = null;
            attackBehavior = null;
            skillBehavior = null;
            behitBehavior = null;
            animationHandle = null;
            unitView = null;
            animationNames.Clear();
            eventNames.Clear();
            animationInfos.Clear();
        }
    }
}

#endif
