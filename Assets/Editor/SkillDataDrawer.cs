#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using GameSystems.Battle;
using GameSystems.Skills;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GameSystems.Skills.Editor
{
    [CustomPropertyDrawer(typeof(SkillData))]
    public class SkillDataDrawer : PropertyDrawer
    {
        private const float LineHeight = 18f;

        private const float VSpacing = 10f;
        private const float StepIndexWidth = 58f;
        private const float StepButtonWidth = 24f;
        private const float StepButtonGap = 2f;
        private const float StepControlRightWidth = StepButtonWidth * 3f + StepButtonGap * 2f;
        private const float StepRowContentPadding = 12f;
        private const float StepSelectionExpandedBlockPadding = 4f;
        private const float StepSelectionExpandedContentSpacing = 3f;
        private const float StepSelectionExpandedTailPadding = 8f;

        private static readonly Color LocalOverrideRowColor = new Color(0.98f, 0.56f, 0.24f, 0.12f);
        private static readonly Color LocalOverrideSummaryColor = new Color(1f, 0.76f, 0.35f, 1f);
        private static readonly Dictionary<string, ReorderableList> StepSelectionListCache =
            new Dictionary<string, ReorderableList>();
        private static readonly GUIContent EditOverrideContent;
        private static readonly GUIContent HideOverrideContent;
        private static readonly GUIContent ResetOverrideContent = new GUIContent(
            "↺",
            "Revert the local override back to the source step"
        );

        private enum BulkStepSelectionMode
        {
            Append,
            Replace,
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                return LineHeight;
            }

            if (!property.isExpanded)
            {
                return LineHeight + 4f;
            }

            float height = (LineHeight + VSpacing) * 22f + 8f;
            var stepSelectionsProp = property.FindPropertyRelative("stepSelections");
            if (stepSelectionsProp != null)
            {
                height += LineHeight + VSpacing;
                if (stepSelectionsProp.isExpanded)
                {
                    for (int i = 0; i < stepSelectionsProp.arraySize; i++)
                    {
                        height += GetStepSelectionHeight(
                            stepSelectionsProp.GetArrayElementAtIndex(i)
                        );
                    }
                }
            }
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var idProp = property.FindPropertyRelative("skillId");
            var nameProp = property.FindPropertyRelative("skillName");
            var categoryProp = property.FindPropertyRelative("category");
            var damageTypeProp = property.FindPropertyRelative("damageType");
            var damageProp = property.FindPropertyRelative("baseDamage");
            var cooldownProp = property.FindPropertyRelative("baseCooldown");
            var manaProp = property.FindPropertyRelative("manaCost");
            var stepSelectionsProp = property.FindPropertyRelative("stepSelections");

            var headerRect = new Rect(position.x, position.y, position.width, LineHeight);
            string title = BuildTitle(
                idProp,
                nameProp,
                categoryProp,
                damageTypeProp,
                damageProp,
                cooldownProp,
                manaProp
            );
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, title, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = position.y + LineHeight + 4f;

                DrawLine(position, ref y, property.FindPropertyRelative("skillId"), "Skill Id");
                DrawLine(position, ref y, property.FindPropertyRelative("skillName"), "Skill Name");
                DrawLine(
                    position,
                    ref y,
                    property.FindPropertyRelative("description"),
                    "Description"
                );
                DrawLine(position, ref y, categoryProp, "Category");
                DrawLine(position, ref y, damageTypeProp, "Damage Type");
                DrawPairLine(
                    position,
                    ref y,
                    property.FindPropertyRelative("currentLevel"),
                    property.FindPropertyRelative("maxLevel"),
                    "Current Level",
                    "Max Level"
                );
                DrawPairLine(
                    position,
                    ref y,
                    property.FindPropertyRelative("requiredLevel"),
                    manaProp,
                    "Required Level",
                    "Mana Cost"
                );
                DrawLine(position, ref y, property.FindPropertyRelative("isUnlocked"), "Unlocked");
                DrawTripleLine(
                    position,
                    ref y,
                    property.FindPropertyRelative("baseCooldown"),
                    property.FindPropertyRelative("currentCooldown"),
                    property.FindPropertyRelative("isOnCooldown"),
                    "Base CD",
                    "Current CD",
                    "On CD"
                );
                DrawPairLine(
                    position,
                    ref y,
                    damageProp,
                    property.FindPropertyRelative("damagePerLevel"),
                    "Base Damage",
                    "Dmg / Lv"
                );
                DrawPairLine(
                    position,
                    ref y,
                    property.FindPropertyRelative("range"),
                    property.FindPropertyRelative("maxTargets"),
                    "Range",
                    "Max Targets"
                );
                DrawTripleLine(
                    position,
                    ref y,
                    property.FindPropertyRelative("effectType"),
                    property.FindPropertyRelative("effectDuration"),
                    property.FindPropertyRelative("effectValue"),
                    "Effect Type",
                    "Effect Duration",
                    "Effect Value"
                );
                DrawPairLine(
                    position,
                    ref y,
                    property.FindPropertyRelative("castTime"),
                    property.FindPropertyRelative("totalCasts"),
                    "Cast Time",
                    "Total Casts"
                );
                DrawLine(position, ref y, property.FindPropertyRelative("icon"), "Icon");

                DrawStepSelectionListEditor(position, ref y, stepSelectionsProp, fieldInfo);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private static string BuildTitle(
            SerializedProperty idProp,
            SerializedProperty nameProp,
            SerializedProperty categoryProp,
            SerializedProperty damageTypeProp,
            SerializedProperty damageProp,
            SerializedProperty cooldownProp,
            SerializedProperty manaProp
        )
        {
            string id = idProp != null ? idProp.stringValue : "SkillData";
            string name =
                nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue)
                    ? nameProp.stringValue
                    : "Unnamed";
            string category =
                categoryProp != null
                    ? categoryProp.enumDisplayNames[categoryProp.enumValueIndex]
                    : "Unknown";
            string damageType =
                damageTypeProp != null
                    ? damageTypeProp.enumDisplayNames[damageTypeProp.enumValueIndex]
                    : "Unknown";
            float damage = damageProp != null ? damageProp.floatValue : 0f;
            float cooldown = cooldownProp != null ? cooldownProp.floatValue : 0f;
            int mana = manaProp != null ? manaProp.intValue : 0;

            return $"{name} [{id}]  {category}/{damageType}  DMG {damage:F0}  CD {cooldown:F1}s  MP {mana}";
        }

        private static void DrawLine(
            Rect position,
            ref float y,
            SerializedProperty prop,
            string label
        )
        {
            if (prop == null)
            {
                return;
            }

            var rect = new Rect(
                position.x,
                y,
                position.width,
                EditorGUI.GetPropertyHeight(prop, true)
            );
            EditorGUI.PropertyField(rect, prop, new GUIContent(label), true);
            y += rect.height + VSpacing;
        }

        private static void DrawPairLine(
            Rect position,
            ref float y,
            SerializedProperty first,
            SerializedProperty second,
            string firstLabel,
            string secondLabel
        )
        {
            if (first == null || second == null)
            {
                return;
            }

            float blockHeight =
                Mathf.Max(
                    EditorGUI.GetPropertyHeight(first, true),
                    EditorGUI.GetPropertyHeight(second, true)
                )
                + LineHeight
                + VSpacing;
            var left = new Rect(position.x, y, position.width * 0.5f - 4f, blockHeight);
            var right = new Rect(
                position.x + position.width * 0.5f + 4f,
                y,
                position.width * 0.5f - 4f,
                blockHeight
            );

            DrawPropertyBlock(left, first, firstLabel);
            DrawPropertyBlock(right, second, secondLabel);

            y += blockHeight + VSpacing;
        }

        private struct StepOption
        {
            public SkillViewSequence Sequence;
            public int StepIndex;
            public string SequenceLabel;
            public string Label;
        }

        private static StepOption[] stepOptionCache = null;
        private static string[] stepPopupLabels = null;
        private static bool stepOptionCacheDirty = true;

        static SkillDataDrawer()
        {
            EditOverrideContent = CreateIconContent(
                new[] { "d_editicon.sml", "editicon.sml" },
                "Edit",
                "Create or edit a local copy for this character"
            );
            HideOverrideContent = CreateIconContent(
                new[]
                {
                    "d_winbtn_win_close",
                    "winbtn_win_close",
                    "d_winbtn_mac_close",
                    "winbtn_mac_close",
                },
                "Hide",
                "Hide the local override editor without reverting the step"
            );
            EditorApplication.projectChanged += MarkStepOptionCacheDirty;
            AssemblyReloadEvents.afterAssemblyReload += MarkStepOptionCacheDirty;
        }

        private static void MarkStepOptionCacheDirty()
        {
            stepOptionCacheDirty = true;
            stepOptionCache = null;
            stepPopupLabels = null;
        }

        private static void EnsureStepOptionCache()
        {
            if (!stepOptionCacheDirty && stepOptionCache != null && stepPopupLabels != null)
            {
                return;
            }

            RefreshStepOptionCache();
        }

        private static void RefreshStepOptionCache()
        {
            var options = new List<StepOption>();
            string[] guids = AssetDatabase.FindAssets("t:SkillViewSequence");

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var sequence = AssetDatabase.LoadAssetAtPath<SkillViewSequence>(path);
                if (sequence == null || sequence.Steps == null)
                {
                    continue;
                }

                string sequenceLabel = !string.IsNullOrWhiteSpace(sequence.SequenceId)
                    ? sequence.SequenceId
                    : sequence.name;
                for (int stepIndex = 0; stepIndex < sequence.Steps.Count; stepIndex++)
                {
                    var step = sequence.Steps[stepIndex];
                    if (step == null)
                    {
                        continue;
                    }

                    options.Add(
                        new StepOption
                        {
                            Sequence = sequence,
                            StepIndex = stepIndex,
                            SequenceLabel = sequenceLabel,
                            Label = BuildStepLabel(sequenceLabel, stepIndex, step),
                        }
                    );
                }
            }

            options.Sort(
                (a, b) =>
                {
                    int sequenceCompare = string.Compare(
                        a.SequenceLabel,
                        b.SequenceLabel,
                        StringComparison.OrdinalIgnoreCase
                    );
                    if (sequenceCompare != 0)
                    {
                        return sequenceCompare;
                    }

                    return a.StepIndex.CompareTo(b.StepIndex);
                }
            );

            stepOptionCache = options.ToArray();
            stepPopupLabels = new string[stepOptionCache.Length + 1];
            stepPopupLabels[0] = "<None>";

            for (int i = 0; i < stepOptionCache.Length; i++)
            {
                stepPopupLabels[i + 1] = stepOptionCache[i].Label;
            }

            stepOptionCacheDirty = false;
        }

        private static string BuildStepLabel(
            string sequenceLabel,
            int stepIndex,
            SkillViewStep step
        )
        {
            if (step == null)
            {
                return $"{sequenceLabel} / #{stepIndex} <Null>";
            }

            string animationLabel = string.IsNullOrWhiteSpace(step.AnimationName)
                ? string.Empty
                : $" [{step.AnimationName}]";
            return $"{sequenceLabel} / #{stepIndex} {step.StepType}{animationLabel}";
        }

        private static string BuildMissingStepLabel(SkillViewSequence sequence, int stepIndex)
        {
            if (sequence == null)
            {
                return "<None>";
            }

            string sequenceLabel = !string.IsNullOrWhiteSpace(sequence.SequenceId)
                ? sequence.SequenceId
                : sequence.name;
            return $"{sequenceLabel} / Missing step #{stepIndex}";
        }

        private static int FindStepOptionIndex(SkillViewSequence sequence, int stepIndex)
        {
            if (sequence == null || stepOptionCache == null)
            {
                return -1;
            }

            for (int i = 0; i < stepOptionCache.Length; i++)
            {
                if (
                    stepOptionCache[i].Sequence == sequence
                    && stepOptionCache[i].StepIndex == stepIndex
                )
                {
                    return i + 1;
                }
            }

            return -1;
        }

        private static string[] BuildPopupLabels(
            SkillViewSequence sequence,
            int stepIndex,
            out int currentIndex
        )
        {
            EnsureStepOptionCache();

            int foundIndex = FindStepOptionIndex(sequence, stepIndex);
            if (foundIndex >= 0)
            {
                currentIndex = foundIndex;
                return stepPopupLabels;
            }

            if (sequence == null)
            {
                currentIndex = 0;
                return stepPopupLabels;
            }

            var labels = new string[stepPopupLabels.Length + 1];
            labels[0] = "<None>";
            labels[1] = BuildMissingStepLabel(sequence, stepIndex);
            Array.Copy(stepPopupLabels, 1, labels, 2, stepPopupLabels.Length - 1);
            currentIndex = 1;
            return labels;
        }

        private static void DrawStepSelectionListEditor(
            Rect position,
            ref float y,
            SerializedProperty stepSelectionsProp,
            FieldInfo skillDataFieldInfo
        )
        {
            if (stepSelectionsProp == null)
            {
                return;
            }

            EnsureStepOptionCache();

            Rect foldoutRect = new Rect(
                position.x,
                y,
                position.width - 140f,
                EditorGUIUtility.singleLineHeight
            );
            stepSelectionsProp.isExpanded = EditorGUI.Foldout(
                foldoutRect,
                stepSelectionsProp.isExpanded,
                "Step Skills",
                true
            );

            Rect addRect = new Rect(
                position.xMax - 138f,
                y,
                24f,
                EditorGUIUtility.singleLineHeight
            );
            if (
                GUI.Button(
                    addRect,
                    new GUIContent("+", "Add an empty step selection"),
                    EditorStyles.miniButton
                )
            )
            {
                stepSelectionsProp.isExpanded = true;
                stepSelectionsProp.arraySize++;
                var elem = stepSelectionsProp.GetArrayElementAtIndex(
                    stepSelectionsProp.arraySize - 1
                );
                elem.FindPropertyRelative("sequence").objectReferenceValue = null;
                elem.FindPropertyRelative("stepIndex").intValue = -1;
                var useLocalOverrideProp = elem.FindPropertyRelative("useLocalOverride");
                if (useLocalOverrideProp != null)
                {
                    useLocalOverrideProp.boolValue = false;
                }

                elem.isExpanded = false;
                var localOverrideStepProp = elem.FindPropertyRelative("localOverrideStep");
                if (localOverrideStepProp != null)
                {
                    localOverrideStepProp.isExpanded = false;
                }
                ClearLegacyStepSequenceFields(stepSelectionsProp);
                InvalidateSkillDataRuntimeCache(stepSelectionsProp, skillDataFieldInfo);
            }

            Rect removeRect = new Rect(
                position.xMax - 112f,
                y,
                56f,
                EditorGUIUtility.singleLineHeight
            );
            if (
                GUI.Button(
                    removeRect,
                    new GUIContent("Remove", "Clear all step selections"),
                    EditorStyles.miniButton
                )
            )
            {
                ClearAllStepSelections(stepSelectionsProp, skillDataFieldInfo);
            }

            Rect replaceRect = new Rect(
                position.xMax - 54f,
                y,
                30f,
                EditorGUIUtility.singleLineHeight
            );
            if (
                GUI.Button(
                    replaceRect,
                    new GUIContent("Set", "Replace the list with all steps from one sequence"),
                    EditorStyles.miniButton
                )
            )
            {
                ShowBulkMenu(
                    stepSelectionsProp,
                    replaceRect,
                    BulkStepSelectionMode.Replace,
                    skillDataFieldInfo
                );
            }

            Rect refreshRect = new Rect(
                position.xMax - 24f,
                y,
                24f,
                EditorGUIUtility.singleLineHeight
            );
            if (GUI.Button(refreshRect, "↻", EditorStyles.miniButton))
            {
                MarkStepOptionCacheDirty();
                EnsureStepOptionCache();
            }

            y += foldoutRect.height + VSpacing;

            if (!stepSelectionsProp.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            float listHeight = GetStepSelectionListHeight(stepSelectionsProp);
            if (listHeight > 0f)
            {
                Rect listRect = new Rect(position.x + 2f, y, position.width - 4f, listHeight);
                GetStepSelectionReorderableList(stepSelectionsProp, skillDataFieldInfo).DoList(
                    listRect
                );
                y += listHeight;
            }

            EditorGUI.indentLevel--;
        }

        private static float GetStepSelectionHeight(SerializedProperty selectionProp)
        {
            if (selectionProp == null)
            {
                return LineHeight + VSpacing;
            }

            float height = GetStepSelectionContentHeight(selectionProp) + VSpacing;
            return Mathf.Ceil(height);
        }

        private static float GetStepSelectionContentHeight(SerializedProperty selectionProp)
        {
            float height = EditorGUIUtility.singleLineHeight;
            var useLocalOverrideProp = selectionProp.FindPropertyRelative("useLocalOverride");
            var localOverrideStepProp = selectionProp.FindPropertyRelative("localOverrideStep");

            if (
                selectionProp.isExpanded
                && useLocalOverrideProp != null
                && useLocalOverrideProp.boolValue
                && localOverrideStepProp != null
            )
            {
                height +=
                    VSpacing
                    + StepSelectionExpandedBlockPadding
                    + EditorGUIUtility.singleLineHeight
                    + StepSelectionExpandedContentSpacing
                    + Mathf.Ceil(EditorGUI.GetPropertyHeight(localOverrideStepProp, true))
                    + StepSelectionExpandedTailPadding;
            }

            return Mathf.Ceil(height);
        }

        private static float GetStepSelectionListHeight(SerializedProperty stepSelectionsProp)
        {
            if (stepSelectionsProp == null || !stepSelectionsProp.isExpanded)
            {
                return 0f;
            }

            float height = 0f;
            for (int i = 0; i < stepSelectionsProp.arraySize; i++)
            {
                height += GetStepSelectionHeight(stepSelectionsProp.GetArrayElementAtIndex(i));
            }

            return Mathf.Ceil(height);
        }

        private static ReorderableList GetStepSelectionReorderableList(
            SerializedProperty stepSelectionsProp,
            FieldInfo skillDataFieldInfo
        )
        {
            if (stepSelectionsProp == null)
            {
                return null;
            }

            string cacheKey = GetStepSelectionListCacheKey(stepSelectionsProp);
            if (!StepSelectionListCache.TryGetValue(cacheKey, out var list) || list == null)
            {
                list = new ReorderableList(
                    stepSelectionsProp.serializedObject,
                    stepSelectionsProp,
                    true,
                    false,
                    false,
                    false
                );
                list.headerHeight = 0f;
                list.footerHeight = 0f;
                list.elementHeightCallback = index =>
                {
                    var currentProp = list.serializedProperty;
                    if (currentProp == null || index < 0 || index >= currentProp.arraySize)
                    {
                        return LineHeight + VSpacing;
                    }

                    var element = currentProp.GetArrayElementAtIndex(index);
                    return GetStepSelectionHeight(element);
                };
                list.drawElementCallback = (rect, index, active, focused) =>
                {
                    DrawStepSelectionElement(rect, list.serializedProperty, index, skillDataFieldInfo);
                };
                list.onReorderCallbackWithDetails = (reorderableList, oldIndex, newIndex) =>
                {
                    HandleStepSelectionReordered(
                        reorderableList.serializedProperty,
                        skillDataFieldInfo
                    );
                };

                StepSelectionListCache[cacheKey] = list;
            }
            else
            {
                list.serializedProperty = stepSelectionsProp;
                list.elementHeightCallback = index =>
                {
                    var currentProp = list.serializedProperty;
                    if (currentProp == null || index < 0 || index >= currentProp.arraySize)
                    {
                        return LineHeight + VSpacing;
                    }

                    var element = currentProp.GetArrayElementAtIndex(index);
                    return GetStepSelectionHeight(element);
                };
            }

            return list;
        }

        private static string GetStepSelectionListCacheKey(SerializedProperty stepSelectionsProp)
        {
            var targetObject = stepSelectionsProp.serializedObject.targetObject;
            int instanceId = targetObject != null ? targetObject.GetInstanceID() : 0;
            return $"{instanceId}:{stepSelectionsProp.propertyPath}";
        }

        private static void DrawStepSelectionElement(
            Rect rect,
            SerializedProperty stepSelectionsProp,
            int index,
            FieldInfo skillDataFieldInfo
        )
        {
            if (stepSelectionsProp == null || index < 0 || index >= stepSelectionsProp.arraySize)
            {
                return;
            }

            var elem = stepSelectionsProp.GetArrayElementAtIndex(index);
            var sequenceProp = elem.FindPropertyRelative("sequence");
            var stepIndexProp = elem.FindPropertyRelative("stepIndex");
            var useLocalOverrideProp = elem.FindPropertyRelative("useLocalOverride");
            var localOverrideStepProp = elem.FindPropertyRelative("localOverrideStep");
            bool hasLocalOverride = useLocalOverrideProp != null && useLocalOverrideProp.boolValue;
            bool hasSourceStep =
                FindStepOptionIndex(
                    sequenceProp.objectReferenceValue as SkillViewSequence,
                    stepIndexProp.intValue
                ) >= 0;
            bool canEdit = hasSourceStep || hasLocalOverride;
            string overrideSummary =
                hasLocalOverride && localOverrideStepProp != null
                    ? BuildLocalOverrideSummary(
                        sequenceProp.objectReferenceValue as SkillViewSequence,
                        stepIndexProp.intValue,
                        localOverrideStepProp
                    )
                    : string.Empty;

            float rowHeight = GetStepSelectionContentHeight(elem);
            Rect fullRowRect = new Rect(rect.x + 2f, rect.y, rect.width - 4f, rowHeight);
            EditorGUI.DrawRect(
                fullRowRect,
                hasLocalOverride
                    ? LocalOverrideRowColor
                    : (index % 2 == 0 ? new Color(0f, 0f, 0f, 0.15f) : Color.clear)
            );

            Rect indexRect = new Rect(
                rect.x + 4f,
                rect.y,
                StepIndexWidth,
                EditorGUIUtility.singleLineHeight
            );
            GUI.Label(indexRect, $"Step {index}", EditorStyles.miniLabel);

            Rect rowRect = new Rect(
                indexRect.xMax + 4f,
                rect.y,
                Mathf.Max(40f, rect.width - (StepIndexWidth + StepControlRightWidth + 18f)),
                EditorGUIUtility.singleLineHeight
            );
            int currentIndex;
            string[] labels = BuildPopupLabels(
                sequenceProp.objectReferenceValue as SkillViewSequence,
                stepIndexProp.intValue,
                out currentIndex
            );
            bool usesCanonicalLabels = labels == stepPopupLabels;
            if (
                hasLocalOverride
                && labels != null
                && currentIndex >= 0
                && currentIndex < labels.Length
            )
            {
                labels = (string[])labels.Clone();
                labels[currentIndex] = $"{labels[currentIndex]} [override]";
            }

            int nextIndex = EditorGUI.Popup(rowRect, currentIndex, labels);
            if (nextIndex != currentIndex)
            {
                if (nextIndex == 0)
                {
                    sequenceProp.objectReferenceValue = null;
                    stepIndexProp.intValue = -1;
                }
                else if (usesCanonicalLabels)
                {
                    int optionIndex = nextIndex - 1;
                    if (optionIndex >= 0 && optionIndex < stepOptionCache.Length)
                    {
                        sequenceProp.objectReferenceValue = stepOptionCache[optionIndex].Sequence;
                        stepIndexProp.intValue = stepOptionCache[optionIndex].StepIndex;
                    }
                }
                else if (nextIndex > 1)
                {
                    int optionIndex = nextIndex - 2;
                    if (optionIndex >= 0 && optionIndex < stepOptionCache.Length)
                    {
                        sequenceProp.objectReferenceValue = stepOptionCache[optionIndex].Sequence;
                        stepIndexProp.intValue = stepOptionCache[optionIndex].StepIndex;
                    }
                }

                if (useLocalOverrideProp != null)
                {
                    useLocalOverrideProp.boolValue = false;
                }

                InvalidateSkillDataRuntimeCache(stepSelectionsProp, skillDataFieldInfo);
            }

            Rect editRect = new Rect(
                rect.xMax - StepControlRightWidth,
                rect.y,
                StepButtonWidth,
                EditorGUIUtility.singleLineHeight
            );
            Rect resetRect = new Rect(
                editRect.xMax + StepButtonGap,
                rect.y,
                StepButtonWidth,
                EditorGUIUtility.singleLineHeight
            );
            Rect rowRemoveRect = new Rect(
                resetRect.xMax + StepButtonGap,
                rect.y,
                StepButtonWidth,
                EditorGUIUtility.singleLineHeight
            );

            bool previousEnabled = GUI.enabled;
            GUI.enabled = canEdit;
            GUIContent toggleContent =
                hasLocalOverride && elem.isExpanded ? HideOverrideContent : EditOverrideContent;
            if (GUI.Button(editRect, toggleContent, EditorStyles.miniButton))
            {
                if (hasLocalOverride && elem.isExpanded)
                {
                    elem.isExpanded = false;
                    if (localOverrideStepProp != null)
                    {
                        localOverrideStepProp.isExpanded = false;
                    }
                }
                else
                {
                    bool copied = false;
                    if (hasSourceStep && !hasLocalOverride)
                    {
                        copied = CopySourceStepToLocalOverride(
                            sequenceProp.objectReferenceValue as SkillViewSequence,
                            stepIndexProp.intValue,
                            localOverrideStepProp
                        );
                        if (copied && useLocalOverrideProp != null)
                        {
                            useLocalOverrideProp.boolValue = true;
                        }
                    }

                    if (hasLocalOverride || copied)
                    {
                        elem.isExpanded = true;
                        if (localOverrideStepProp != null)
                        {
                            localOverrideStepProp.isExpanded = true;
                        }
                    }
                }
            }
            GUI.enabled = previousEnabled;

            GUI.enabled = hasLocalOverride;
            if (GUI.Button(resetRect, ResetOverrideContent, EditorStyles.miniButton))
            {
                if (useLocalOverrideProp != null)
                {
                    useLocalOverrideProp.boolValue = false;
                }

                elem.isExpanded = false;
                if (localOverrideStepProp != null)
                {
                    localOverrideStepProp.isExpanded = false;
                }

                InvalidateSkillDataRuntimeCache(stepSelectionsProp, skillDataFieldInfo);
            }
            GUI.enabled = previousEnabled;

            if (GUI.Button(rowRemoveRect, "-", EditorStyles.miniButton))
            {
                int oldSize = stepSelectionsProp.arraySize;
                stepSelectionsProp.DeleteArrayElementAtIndex(index);
                if (stepSelectionsProp.arraySize == oldSize)
                {
                    stepSelectionsProp.DeleteArrayElementAtIndex(index);
                }

                ClearLegacyStepSequenceFields(stepSelectionsProp);
                InvalidateSkillDataRuntimeCache(stepSelectionsProp, skillDataFieldInfo);
                return;
            }

            if (hasLocalOverride && elem.isExpanded && localOverrideStepProp != null)
            {
                float overrideHeight = Mathf.Ceil(
                    EditorGUI.GetPropertyHeight(localOverrideStepProp, true)
                );
                float blockY = rect.y + EditorGUIUtility.singleLineHeight + VSpacing;
                float contentX = rect.x + StepRowContentPadding;
                float contentWidth = rect.width - (StepRowContentPadding * 2f);
                Rect overrideBackground = new Rect(
                    rect.x + StepSelectionExpandedBlockPadding,
                    blockY,
                    rect.width - (StepSelectionExpandedBlockPadding * 2f),
                    StepSelectionExpandedBlockPadding
                        + EditorGUIUtility.singleLineHeight
                        + StepSelectionExpandedContentSpacing
                        + overrideHeight
                        + StepSelectionExpandedTailPadding
                );
                EditorGUI.DrawRect(overrideBackground, new Color(0f, 0f, 0f, 0.18f));

                Rect summaryRect = new Rect(
                    contentX,
                    blockY + StepSelectionExpandedBlockPadding,
                    contentWidth,
                    EditorGUIUtility.singleLineHeight
                );
                Color previousColor = GUI.color;
                GUI.color = LocalOverrideSummaryColor;
                EditorGUI.LabelField(summaryRect, overrideSummary, EditorStyles.miniLabel);
                GUI.color = previousColor;

                Rect overrideRect = new Rect(
                    contentX,
                    summaryRect.yMax + StepSelectionExpandedContentSpacing,
                    contentWidth,
                    overrideHeight
                );
                EditorGUI.PropertyField(
                    overrideRect,
                    localOverrideStepProp,
                    new GUIContent("Character Override"),
                    true
                );
            }
        }

        private static void HandleStepSelectionReordered(
            SerializedProperty stepSelectionsProp,
            FieldInfo skillDataFieldInfo
        )
        {
            if (stepSelectionsProp == null)
            {
                return;
            }

            ClearLegacyStepSequenceFields(stepSelectionsProp);
            InvalidateSkillDataRuntimeCache(stepSelectionsProp, skillDataFieldInfo);
        }

        private static void InvalidateSkillDataRuntimeCache(
            SerializedProperty stepSelectionsProp,
            FieldInfo skillDataFieldInfo
        )
        {
            if (stepSelectionsProp == null)
            {
                return;
            }

            var serializedObject = stepSelectionsProp.serializedObject;
            if (serializedObject == null)
            {
                return;
            }

            var targets = serializedObject.targetObjects;
            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                var targetObject = targets[i];
                if (targetObject == null)
                {
                    continue;
                }

                if (skillDataFieldInfo != null)
                {
                    object boxedSkillData = skillDataFieldInfo.GetValue(targetObject);
                    if (boxedSkillData is SkillData skillData)
                    {
                        skillData.InvalidateViewSequenceCache();
                    }
                }

                EditorUtility.SetDirty(targetObject);
            }
        }

        private static string BuildLocalOverrideSummary(
            SkillViewSequence sourceSequence,
            int stepIndex,
            SerializedProperty localOverrideStepProp
        )
        {
            var sourceStep = GetSourceStep(sourceSequence, stepIndex);
            if (localOverrideStepProp == null)
            {
                return "Character override";
            }

            if (sourceStep == null)
            {
                return "Local override only";
            }

            var changedFields = new List<string>(8);
            var stepTypeProp = localOverrideStepProp.FindPropertyRelative("stepType");
            var targetTypeProp = localOverrideStepProp.FindPropertyRelative("targetType");
            var moveModeProp = localOverrideStepProp.FindPropertyRelative("moveMode");
            var animationNameProp = localOverrideStepProp.FindPropertyRelative("animationName");
            var fallbackAnimationNameProp = localOverrideStepProp.FindPropertyRelative(
                "fallbackAnimationName"
            );
            var loopProp = localOverrideStepProp.FindPropertyRelative("loop");
            var delayProp = localOverrideStepProp.FindPropertyRelative("delay");
            var durationProp = localOverrideStepProp.FindPropertyRelative("duration");
            var moveDistanceProp = localOverrideStepProp.FindPropertyRelative("moveDistance");
            var sortingOrderProp = localOverrideStepProp.FindPropertyRelative("sortingOrder");
            var flipXProp = localOverrideStepProp.FindPropertyRelative("flipX");
            var worldPositionProp = localOverrideStepProp.FindPropertyRelative("worldPosition");
            var offsetProp = localOverrideStepProp.FindPropertyRelative("offset");
            var vfxPrefabProp = localOverrideStepProp.FindPropertyRelative("vfxPrefab");
            var waitForAnimationEndProp = localOverrideStepProp.FindPropertyRelative(
                "waitForAnimationEnd"
            );
            var triggerHitEffectProp = localOverrideStepProp.FindPropertyRelative(
                "triggerHitEffect"
            );
            var hitCountProp = localOverrideStepProp.FindPropertyRelative("hitCount");

            AppendDiffField(
                changedFields,
                sourceStep.StepType != (SkillViewStepType)stepTypeProp.enumValueIndex,
                GetStepFieldLabel("stepType")
            );
            AppendDiffField(
                changedFields,
                sourceStep.TargetType != (SkillViewTargetType)targetTypeProp.enumValueIndex,
                GetStepFieldLabel("targetType")
            );
            AppendDiffField(
                changedFields,
                sourceStep.MoveMode != (SkillViewMoveMode)moveModeProp.enumValueIndex,
                GetStepFieldLabel("moveMode")
            );
            AppendDiffField(
                changedFields,
                !string.Equals(
                    sourceStep.AnimationName ?? string.Empty,
                    animationNameProp.stringValue ?? string.Empty,
                    StringComparison.Ordinal
                ),
                GetStepFieldLabel("animationName")
            );
            AppendDiffField(
                changedFields,
                !string.Equals(
                    sourceStep.FallbackAnimationName ?? string.Empty,
                    fallbackAnimationNameProp.stringValue ?? string.Empty,
                    StringComparison.Ordinal
                ),
                GetStepFieldLabel("fallbackAnimationName")
            );
            AppendDiffField(
                changedFields,
                sourceStep.Loop != loopProp.boolValue,
                GetStepFieldLabel("loop")
            );
            AppendDiffField(
                changedFields,
                Mathf.Abs(sourceStep.Delay - delayProp.floatValue) > 0.0001f,
                GetStepFieldLabel("delay")
            );
            AppendDiffField(
                changedFields,
                Mathf.Abs(sourceStep.Duration - durationProp.floatValue) > 0.0001f,
                GetStepFieldLabel("duration")
            );
            AppendDiffField(
                changedFields,
                Mathf.Abs(sourceStep.MoveDistance - moveDistanceProp.floatValue) > 0.0001f,
                GetStepFieldLabel("moveDistance")
            );
            AppendDiffField(
                changedFields,
                sourceStep.SortingOrder != sortingOrderProp.intValue,
                GetStepFieldLabel("sortingOrder")
            );
            AppendDiffField(
                changedFields,
                sourceStep.FlipX != flipXProp.boolValue,
                GetStepFieldLabel("flipX")
            );
            AppendDiffField(
                changedFields,
                sourceStep.WorldPosition != worldPositionProp.vector3Value,
                GetStepFieldLabel("worldPosition")
            );
            AppendDiffField(
                changedFields,
                sourceStep.Offset != offsetProp.vector3Value,
                GetStepFieldLabel("offset")
            );
            AppendDiffField(
                changedFields,
                sourceStep.VfxPrefab != vfxPrefabProp.objectReferenceValue,
                GetStepFieldLabel("vfxPrefab")
            );
            AppendDiffField(
                changedFields,
                sourceStep.WaitForAnimationEnd != waitForAnimationEndProp.boolValue,
                GetStepFieldLabel("waitForAnimationEnd")
            );
            AppendDiffField(
                changedFields,
                sourceStep.TriggerHitEffect != triggerHitEffectProp.boolValue,
                GetStepFieldLabel("triggerHitEffect")
            );
            AppendDiffField(
                changedFields,
                sourceStep.HitCount != hitCountProp.intValue,
                GetStepFieldLabel("hitCount")
            );

            if (changedFields.Count == 0)
            {
                return "No field changes from source";
            }

            int visibleCount = Mathf.Min(4, changedFields.Count);
            string summary =
                "Changed: " + string.Join(", ", changedFields.GetRange(0, visibleCount));
            if (changedFields.Count > visibleCount)
            {
                summary += $" +{changedFields.Count - visibleCount} more";
            }

            return summary;
        }

        private static SkillViewStep GetSourceStep(SkillViewSequence sourceSequence, int stepIndex)
        {
            if (
                sourceSequence == null
                || sourceSequence.Steps == null
                || stepIndex < 0
                || stepIndex >= sourceSequence.Steps.Count
            )
            {
                return null;
            }

            return sourceSequence.Steps[stepIndex];
        }

        private static void AppendDiffField(
            List<string> changedFields,
            bool isDifferent,
            string displayName
        )
        {
            if (changedFields == null || !isDifferent)
            {
                return;
            }

            changedFields.Add(displayName);
        }

        private static string GetStepFieldLabel(string propertyName)
        {
            switch (propertyName)
            {
                case "stepType":
                    return "Step Type";
                case "targetType":
                    return "Target";
                case "moveMode":
                    return "Move Mode";
                case "animationName":
                    return "Animation";
                case "fallbackAnimationName":
                    return "Fallback";
                case "loop":
                    return "Loop";
                case "delay":
                    return "Delay";
                case "duration":
                    return "Duration";
                case "moveDistance":
                    return "Move Dist";
                case "sortingOrder":
                    return "Sort";
                case "flipX":
                    return "Flip X";
                case "worldPosition":
                    return "World Pos";
                case "offset":
                    return "Offset";
                case "vfxPrefab":
                    return "VFX";
                case "waitForAnimationEnd":
                    return "Wait";
                case "triggerHitEffect":
                    return "Hit Fx";
                case "hitCount":
                    return "Hits";
                default:
                    return propertyName;
            }
        }

        private static bool CopySourceStepToLocalOverride(
            SkillViewSequence sourceSequence,
            int stepIndex,
            SerializedProperty localOverrideStepProp
        )
        {
            if (sourceSequence == null || localOverrideStepProp == null)
            {
                return false;
            }

            var sourceSo = new SerializedObject(sourceSequence);
            sourceSo.Update();

            var stepsProp = sourceSo.FindProperty("steps");
            if (stepsProp == null || stepIndex < 0 || stepIndex >= stepsProp.arraySize)
            {
                return false;
            }

            var sourceStepProp = stepsProp.GetArrayElementAtIndex(stepIndex);
            if (sourceStepProp == null)
            {
                return false;
            }

            CopySkillViewStepProperties(sourceStepProp, localOverrideStepProp);
            localOverrideStepProp.isExpanded = true;
            return true;
        }

        private static void CopySkillViewStepProperties(
            SerializedProperty sourceStepProp,
            SerializedProperty destStepProp
        )
        {
            if (sourceStepProp == null || destStepProp == null)
            {
                return;
            }

            CopyEnumProperty(sourceStepProp, destStepProp, "stepType");
            CopyEnumProperty(sourceStepProp, destStepProp, "targetType");
            CopyEnumProperty(sourceStepProp, destStepProp, "moveMode");
            CopyStringProperty(sourceStepProp, destStepProp, "animationName");
            CopyStringProperty(sourceStepProp, destStepProp, "fallbackAnimationName");
            CopyBoolProperty(sourceStepProp, destStepProp, "loop");
            CopyFloatProperty(sourceStepProp, destStepProp, "delay");
            CopyFloatProperty(sourceStepProp, destStepProp, "duration");
            CopyFloatProperty(sourceStepProp, destStepProp, "moveDistance");
            CopyIntProperty(sourceStepProp, destStepProp, "sortingOrder");
            CopyBoolProperty(sourceStepProp, destStepProp, "flipX");
            CopyVector3Property(sourceStepProp, destStepProp, "worldPosition");
            CopyVector3Property(sourceStepProp, destStepProp, "offset");
            CopyObjectReferenceProperty(sourceStepProp, destStepProp, "vfxPrefab");
            CopyBoolProperty(sourceStepProp, destStepProp, "waitForAnimationEnd");
            CopyBoolProperty(sourceStepProp, destStepProp, "triggerHitEffect");
            CopyIntProperty(sourceStepProp, destStepProp, "hitCount");
        }

        private static void CopyEnumProperty(
            SerializedProperty sourceStepProp,
            SerializedProperty destStepProp,
            string propertyName
        )
        {
            var sourceProp = sourceStepProp.FindPropertyRelative(propertyName);
            var destProp = destStepProp.FindPropertyRelative(propertyName);
            if (sourceProp == null || destProp == null)
            {
                return;
            }

            destProp.enumValueIndex = sourceProp.enumValueIndex;
        }

        private static void CopyStringProperty(
            SerializedProperty sourceStepProp,
            SerializedProperty destStepProp,
            string propertyName
        )
        {
            var sourceProp = sourceStepProp.FindPropertyRelative(propertyName);
            var destProp = destStepProp.FindPropertyRelative(propertyName);
            if (sourceProp == null || destProp == null)
            {
                return;
            }

            destProp.stringValue = sourceProp.stringValue;
        }

        private static void CopyBoolProperty(
            SerializedProperty sourceStepProp,
            SerializedProperty destStepProp,
            string propertyName
        )
        {
            var sourceProp = sourceStepProp.FindPropertyRelative(propertyName);
            var destProp = destStepProp.FindPropertyRelative(propertyName);
            if (sourceProp == null || destProp == null)
            {
                return;
            }

            destProp.boolValue = sourceProp.boolValue;
        }

        private static void CopyFloatProperty(
            SerializedProperty sourceStepProp,
            SerializedProperty destStepProp,
            string propertyName
        )
        {
            var sourceProp = sourceStepProp.FindPropertyRelative(propertyName);
            var destProp = destStepProp.FindPropertyRelative(propertyName);
            if (sourceProp == null || destProp == null)
            {
                return;
            }

            destProp.floatValue = sourceProp.floatValue;
        }

        private static void CopyIntProperty(
            SerializedProperty sourceStepProp,
            SerializedProperty destStepProp,
            string propertyName
        )
        {
            var sourceProp = sourceStepProp.FindPropertyRelative(propertyName);
            var destProp = destStepProp.FindPropertyRelative(propertyName);
            if (sourceProp == null || destProp == null)
            {
                return;
            }

            destProp.intValue = sourceProp.intValue;
        }

        private static void CopyVector3Property(
            SerializedProperty sourceStepProp,
            SerializedProperty destStepProp,
            string propertyName
        )
        {
            var sourceProp = sourceStepProp.FindPropertyRelative(propertyName);
            var destProp = destStepProp.FindPropertyRelative(propertyName);
            if (sourceProp == null || destProp == null)
            {
                return;
            }

            destProp.vector3Value = sourceProp.vector3Value;
        }

        private static void CopyObjectReferenceProperty(
            SerializedProperty sourceStepProp,
            SerializedProperty destStepProp,
            string propertyName
        )
        {
            var sourceProp = sourceStepProp.FindPropertyRelative(propertyName);
            var destProp = destStepProp.FindPropertyRelative(propertyName);
            if (sourceProp == null || destProp == null)
            {
                return;
            }

            destProp.objectReferenceValue = sourceProp.objectReferenceValue;
        }

        private static GUIContent CreateIconContent(
            string[] iconNames,
            string fallbackText,
            string tooltip
        )
        {
            return new GUIContent(fallbackText, tooltip);
        }

        private static void ShowBulkMenu(
            SerializedProperty stepSelectionsProp,
            Rect anchorRect,
            BulkStepSelectionMode mode,
            FieldInfo skillDataFieldInfo
        )
        {
            if (stepSelectionsProp == null)
            {
                return;
            }

            EnsureStepOptionCache();

            var serializedObject = stepSelectionsProp.serializedObject;
            string propertyPath = stepSelectionsProp.propertyPath;
            var targetObjects = serializedObject != null ? serializedObject.targetObjects : null;
            if (
                targetObjects == null
                || targetObjects.Length == 0
                || string.IsNullOrEmpty(propertyPath)
            )
            {
                return;
            }

            var targetCopy = new UnityEngine.Object[targetObjects.Length];
            Array.Copy(targetObjects, targetCopy, targetObjects.Length);

            var menu = new GenericMenu();
            if (stepOptionCache == null || stepOptionCache.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No SkillViewSequence assets found"));
                menu.DropDown(anchorRect);
                return;
            }

            var uniqueSequences = GetUniqueSequencesFromStepOptions();
            for (int i = 0; i < uniqueSequences.Count; i++)
            {
                var sequence = uniqueSequences[i];
                if (sequence == null || sequence.Steps == null || sequence.Steps.Count == 0)
                {
                    continue;
                }

                string sequenceLabel = !string.IsNullOrWhiteSpace(sequence.SequenceId)
                    ? sequence.SequenceId
                    : sequence.name;
                string menuLabel = $"{sequenceLabel} ({sequence.Steps.Count} steps)";

                SkillViewSequence capturedSequence = sequence;
                menu.AddItem(
                    new GUIContent(menuLabel),
                    false,
                    () =>
                    {
                        ScheduleBulkSelectionApply(
                            targetCopy,
                            propertyPath,
                            capturedSequence,
                            mode,
                            skillDataFieldInfo
                        );
                    }
                );
            }

            menu.DropDown(anchorRect);
        }

        private static List<SkillViewSequence> GetUniqueSequencesFromStepOptions()
        {
            var uniqueSequences = new List<SkillViewSequence>();
            if (stepOptionCache == null || stepOptionCache.Length == 0)
            {
                return uniqueSequences;
            }

            var seenSequences = new HashSet<SkillViewSequence>();
            for (int i = 0; i < stepOptionCache.Length; i++)
            {
                var sequence = stepOptionCache[i].Sequence;
                if (sequence == null || !seenSequences.Add(sequence))
                {
                    continue;
                }

                uniqueSequences.Add(sequence);
            }

            return uniqueSequences;
        }

        private static void ScheduleBulkSelectionApply(
            UnityEngine.Object[] targetObjects,
            string propertyPath,
            SkillViewSequence sequence,
            BulkStepSelectionMode mode,
            FieldInfo skillDataFieldInfo
        )
        {
            if (
                targetObjects == null
                || targetObjects.Length == 0
                || sequence == null
                || string.IsNullOrEmpty(propertyPath)
            )
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                ApplyBulkSelectionToTargets(
                    targetObjects,
                    propertyPath,
                    sequence,
                    mode,
                    skillDataFieldInfo
                );
            };
        }

        private static void ApplyBulkSelectionToTargets(
            UnityEngine.Object[] targetObjects,
            string propertyPath,
            SkillViewSequence sequence,
            BulkStepSelectionMode mode,
            FieldInfo skillDataFieldInfo
        )
        {
            if (
                targetObjects == null
                || targetObjects.Length == 0
                || sequence == null
                || string.IsNullOrEmpty(propertyPath)
            )
            {
                return;
            }

            string undoName =
                mode == BulkStepSelectionMode.Replace
                    ? "Replace Step Skills"
                    : "Append Step Skills";
            Undo.RecordObjects(targetObjects, undoName);

            for (int i = 0; i < targetObjects.Length; i++)
            {
                var targetObject = targetObjects[i];
                if (targetObject == null)
                {
                    continue;
                }

                var so = new SerializedObject(targetObject);
                so.Update();

                var stepSelectionsProp = so.FindProperty(propertyPath);
                if (stepSelectionsProp == null)
                {
                    continue;
                }

                if (mode == BulkStepSelectionMode.Replace)
                {
                    stepSelectionsProp.ClearArray();
                }

                AppendAllStepsFromSequence(stepSelectionsProp, sequence);
                ClearLegacyStepSequenceFields(so, propertyPath);
                so.ApplyModifiedProperties();
                InvalidateSkillDataRuntimeCache(stepSelectionsProp, skillDataFieldInfo);
                EditorUtility.SetDirty(targetObject);
            }
        }

        private static void ClearAllStepSelections(
            SerializedProperty stepSelectionsProp,
            FieldInfo skillDataFieldInfo
        )
        {
            if (stepSelectionsProp == null)
            {
                return;
            }

            stepSelectionsProp.ClearArray();
            ClearLegacyStepSequenceFields(
                stepSelectionsProp.serializedObject,
                stepSelectionsProp.propertyPath
            );
            InvalidateSkillDataRuntimeCache(stepSelectionsProp, skillDataFieldInfo);
        }

        private static void ClearLegacyStepSequenceFields(SerializedProperty stepSelectionsProp)
        {
            if (stepSelectionsProp == null)
            {
                return;
            }

            ClearLegacyStepSequenceFields(
                stepSelectionsProp.serializedObject,
                stepSelectionsProp.propertyPath
            );
        }

        private static void ClearLegacyStepSequenceFields(
            SerializedObject serializedObject,
            string stepSelectionsPath
        )
        {
            if (serializedObject == null || string.IsNullOrEmpty(stepSelectionsPath))
            {
                return;
            }

            string parentPath = GetParentPropertyPath(stepSelectionsPath);
            if (string.IsNullOrEmpty(parentPath))
            {
                return;
            }

            var viewSequenceProp = serializedObject.FindProperty($"{parentPath}.viewSequence");
            if (viewSequenceProp != null)
            {
                viewSequenceProp.objectReferenceValue = null;
            }

            var legacySequencesProp = serializedObject.FindProperty(
                $"{parentPath}.legacyStepSequences"
            );
            if (legacySequencesProp != null)
            {
                legacySequencesProp.ClearArray();
            }
        }

        private static string GetParentPropertyPath(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
            {
                return string.Empty;
            }

            int lastDot = propertyPath.LastIndexOf('.');
            if (lastDot < 0)
            {
                return string.Empty;
            }

            return propertyPath.Substring(0, lastDot);
        }

        private static void AppendAllStepsFromSequence(
            SerializedProperty stepSelectionsProp,
            SkillViewSequence sequence
        )
        {
            if (stepSelectionsProp == null || sequence == null || sequence.Steps == null)
            {
                return;
            }

            stepSelectionsProp.isExpanded = true;

            for (int i = 0; i < sequence.Steps.Count; i++)
            {
                if (sequence.Steps[i] == null)
                {
                    continue;
                }

                int newIndex = stepSelectionsProp.arraySize;
                stepSelectionsProp.arraySize++;
                var elem = stepSelectionsProp.GetArrayElementAtIndex(newIndex);
                var sequenceProp = elem.FindPropertyRelative("sequence");
                var stepIndexProp = elem.FindPropertyRelative("stepIndex");
                if (sequenceProp != null)
                {
                    sequenceProp.objectReferenceValue = sequence;
                }
                if (stepIndexProp != null)
                {
                    stepIndexProp.intValue = i;
                }
            }
        }

        private static void DrawTripleLine(
            Rect position,
            ref float y,
            SerializedProperty first,
            SerializedProperty second,
            SerializedProperty third,
            string firstLabel,
            string secondLabel,
            string thirdLabel
        )
        {
            if (first == null || second == null || third == null)
            {
                return;
            }

            float blockHeight = Mathf.Max(
                EditorGUI.GetPropertyHeight(first, true),
                Mathf.Max(
                    EditorGUI.GetPropertyHeight(second, true),
                    EditorGUI.GetPropertyHeight(third, true)
                )
            );
            blockHeight += LineHeight + VSpacing;

            float thirdWidth = position.width / 3f;
            var firstRect = new Rect(position.x, y, thirdWidth - 4f, blockHeight);
            var secondRect = new Rect(position.x + thirdWidth, y, thirdWidth - 4f, blockHeight);
            var thirdRect = new Rect(position.x + thirdWidth * 2f, y, thirdWidth - 4f, blockHeight);

            DrawPropertyBlock(firstRect, first, firstLabel);
            DrawPropertyBlock(secondRect, second, secondLabel);
            DrawPropertyBlock(thirdRect, third, thirdLabel);

            y += blockHeight + VSpacing;
        }

        private static void DrawPropertyBlock(
            Rect position,
            SerializedProperty property,
            string label
        )
        {
            if (property == null)
            {
                return;
            }

            Rect indentedRect = EditorGUI.IndentedRect(position);
            Rect labelRect = new Rect(
                indentedRect.x,
                indentedRect.y,
                indentedRect.width,
                LineHeight
            );
            Rect fieldRect = new Rect(
                indentedRect.x,
                indentedRect.y + LineHeight + 1f,
                indentedRect.width,
                Mathf.Max(EditorGUI.GetPropertyHeight(property, true), LineHeight)
            );

            EditorGUI.LabelField(labelRect, label, EditorStyles.miniLabel);
            EditorGUI.PropertyField(fieldRect, property, GUIContent.none, true);
        }

        // DrawSequenceField removed
    }
}

#endif
