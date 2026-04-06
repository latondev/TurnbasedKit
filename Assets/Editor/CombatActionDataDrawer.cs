#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using GameSystems.Battle;
using GameSystems.Skills;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GameSystems.Battle.Editor
{
    [CustomPropertyDrawer(typeof(CombatActionData))]
    public class CombatActionDataDrawer : PropertyDrawer
    {
        private static float LineHeight => EditorGUIUtility.singleLineHeight;
        private const float VSpacing = 6f;
        private const float HeaderSpacing = 4f;
        private const float BottomPadding = 6f;

        private const float StepIndexWidth = 58f;
        private const float StepButtonWidth = 24f;
        private const float StepButtonGap = 2f;
        private const float StepControlRightWidth = StepButtonWidth * 3f + StepButtonGap * 2f;
        private const float StepExpandedPadding = 4f;

        private struct StepOption
        {
            public SkillViewSequence Sequence;
            public int StepIndex;
            public string SequenceLabel;
            public string Label;
        }

        private static readonly Dictionary<string, ReorderableList> StepSelectionListCache =
            new Dictionary<string, ReorderableList>();
        private static StepOption[] stepOptionCache;
        private static string[] stepPopupLabels;
        private static bool stepOptionCacheDirty = true;

        static CombatActionDataDrawer()
        {
            EditorApplication.projectChanged += MarkStepOptionCacheDirty;
            AssemblyReloadEvents.afterAssemblyReload += MarkStepOptionCacheDirty;
        }

        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                return LineHeight;
            }

            if (!property.isExpanded)
            {
                return LineHeight + HeaderSpacing;
            }

            float h = LineHeight + HeaderSpacing; // Foldout header + spacing to first line.
            h += GetLineHeight(property, "actionKind");

            var stepSelectionsProp = property.FindPropertyRelative("stepSelections");
            if (stepSelectionsProp != null)
            {
                h += LineHeight + VSpacing;
                if (stepSelectionsProp.isExpanded)
                {
                    h += GetStepSelectionListHeight(stepSelectionsProp) + VSpacing;
                }
            }

            return h + BottomPadding;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var actionKindProp = property.FindPropertyRelative("actionKind");

            string actionKind = actionKindProp != null
                ? actionKindProp.enumDisplayNames[actionKindProp.enumValueIndex]
                : "Unknown";
            string title = $"Action: {actionKind}";

            Rect headerRect = new Rect(position.x, position.y, position.width, LineHeight);
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, title, true);

            if (property.isExpanded)
            {
                float y = position.y + LineHeight + HeaderSpacing;
                DrawLine(position, ref y, actionKindProp, "Action Kind");

                DrawStepSelectionListEditor(
                    position,
                    ref y,
                    property.FindPropertyRelative("stepSelections")
                );
            }

            EditorGUI.EndProperty();
        }

        private static float GetLineHeight(SerializedProperty root, string childPath)
        {
            var p = root.FindPropertyRelative(childPath);
            if (p == null)
            {
                return 0f;
            }

            return EditorGUI.GetPropertyHeight(p, true) + VSpacing;
        }

        private static void DrawLine(Rect position, ref float y, SerializedProperty prop, string label)
        {
            if (prop == null)
            {
                return;
            }

            float h = EditorGUI.GetPropertyHeight(prop, true);
            Rect row = new Rect(position.x, y, position.width, h);
            EditorGUI.PropertyField(row, prop, new GUIContent(label), true);
            y += h + VSpacing;
        }

        private static void DrawStepSelectionListEditor(
            Rect position,
            ref float y,
            SerializedProperty stepSelectionsProp
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
                "Step Selections",
                true
            );

            Rect addRect = new Rect(position.xMax - 138f, y, 24f, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(addRect, new GUIContent("+", "Add empty step"), EditorStyles.miniButton))
            {
                AddEmptyStepSelection(stepSelectionsProp);
            }

            Rect removeRect = new Rect(position.xMax - 112f, y, 56f, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(removeRect, new GUIContent("Remove", "Clear all steps"), EditorStyles.miniButton))
            {
                stepSelectionsProp.ClearArray();
            }

            Rect setRect = new Rect(position.xMax - 54f, y, 30f, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(setRect, new GUIContent("Set", "Replace by sequence"), EditorStyles.miniButton))
            {
                ShowSetSequenceMenu(stepSelectionsProp, setRect);
            }

            Rect refreshRect = new Rect(position.xMax - 24f, y, 24f, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(refreshRect, "R", EditorStyles.miniButton))
            {
                MarkStepOptionCacheDirty();
                EnsureStepOptionCache();
            }

            y += foldoutRect.height + VSpacing;

            if (!stepSelectionsProp.isExpanded)
            {
                return;
            }

            float listHeight = GetStepSelectionListHeight(stepSelectionsProp);
            if (listHeight > 0f)
            {
                Rect listRect = new Rect(position.x + 2f, y, position.width - 4f, listHeight);
                GetStepSelectionReorderableList(stepSelectionsProp).DoList(listRect);
                y += listHeight + VSpacing;
            }
        }

        private static void AddEmptyStepSelection(SerializedProperty stepSelectionsProp)
        {
            stepSelectionsProp.isExpanded = true;
            stepSelectionsProp.arraySize++;
            var elem = stepSelectionsProp.GetArrayElementAtIndex(stepSelectionsProp.arraySize - 1);
            elem.FindPropertyRelative("sequence").objectReferenceValue = null;
            elem.FindPropertyRelative("stepIndex").intValue = -1;

            var useOverrideProp = elem.FindPropertyRelative("useLocalOverride");
            if (useOverrideProp != null)
            {
                useOverrideProp.boolValue = false;
            }

            var localOverrideProp = elem.FindPropertyRelative("localOverrideStep");
            if (localOverrideProp != null)
            {
                localOverrideProp.isExpanded = false;
            }

            elem.isExpanded = false;
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
                    int seqCompare = string.Compare(
                        a.SequenceLabel,
                        b.SequenceLabel,
                        StringComparison.OrdinalIgnoreCase
                    );
                    if (seqCompare != 0)
                    {
                        return seqCompare;
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

            int found = FindStepOptionIndex(sequence, stepIndex);
            if (found >= 0)
            {
                currentIndex = found;
                return stepPopupLabels;
            }

            if (sequence == null)
            {
                currentIndex = 0;
                return stepPopupLabels;
            }

            string[] labels = new string[stepPopupLabels.Length + 1];
            labels[0] = "<None>";
            labels[1] = $"{sequence.name} / Missing step #{stepIndex}";
            Array.Copy(stepPopupLabels, 1, labels, 2, stepPopupLabels.Length - 1);
            currentIndex = 1;
            return labels;
        }

        private static float GetStepSelectionListHeight(SerializedProperty stepSelectionsProp)
        {
            if (stepSelectionsProp == null || !stepSelectionsProp.isExpanded)
            {
                return 0f;
            }

            return GetStepSelectionReorderableList(stepSelectionsProp).GetHeight();
        }

        private static float GetStepSelectionHeight(SerializedProperty selectionProp)
        {
            if (selectionProp == null)
            {
                return LineHeight + VSpacing;
            }

            float h = EditorGUIUtility.singleLineHeight + VSpacing;
            var useOverrideProp = selectionProp.FindPropertyRelative("useLocalOverride");
            var localOverrideProp = selectionProp.FindPropertyRelative("localOverrideStep");
            if (
                selectionProp.isExpanded
                && useOverrideProp != null
                && useOverrideProp.boolValue
                && localOverrideProp != null
            )
            {
                h += StepExpandedPadding;
                h += EditorGUI.GetPropertyHeight(localOverrideProp, true) + VSpacing;
            }

            return Mathf.Ceil(h);
        }

        private static ReorderableList GetStepSelectionReorderableList(SerializedProperty stepSelectionsProp)
        {
            string cacheKey = GetStepSelectionListCacheKey(stepSelectionsProp);
            if (StepSelectionListCache.TryGetValue(cacheKey, out var list) && list != null)
            {
                list.serializedProperty = stepSelectionsProp;
                return list;
            }

            list = new ReorderableList(
                stepSelectionsProp.serializedObject,
                stepSelectionsProp,
                true,
                false,
                false,
                false
            );
            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                DrawStepSelectionElement(rect, list.serializedProperty, index);
            };
            list.elementHeightCallback = index =>
            {
                if (index < 0 || index >= list.serializedProperty.arraySize)
                {
                    return EditorGUIUtility.singleLineHeight + VSpacing;
                }

                return GetStepSelectionHeight(list.serializedProperty.GetArrayElementAtIndex(index));
            };
            StepSelectionListCache[cacheKey] = list;
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
            int index
        )
        {
            if (stepSelectionsProp == null || index < 0 || index >= stepSelectionsProp.arraySize)
            {
                return;
            }

            var elem = stepSelectionsProp.GetArrayElementAtIndex(index);
            var sequenceProp = elem.FindPropertyRelative("sequence");
            var stepIndexProp = elem.FindPropertyRelative("stepIndex");
            var useOverrideProp = elem.FindPropertyRelative("useLocalOverride");
            var localOverrideProp = elem.FindPropertyRelative("localOverrideStep");

            Rect row = new Rect(rect.x, rect.y + 1f, rect.width, EditorGUIUtility.singleLineHeight);
            Rect indexRect = new Rect(row.x + 2f, row.y, StepIndexWidth, row.height);
            Rect popupRect = new Rect(
                indexRect.xMax + StepButtonGap,
                row.y,
                Mathf.Max(20f, row.width - StepIndexWidth - StepControlRightWidth - StepButtonGap * 3f),
                row.height
            );
            Rect idRect = new Rect(popupRect.xMax + StepButtonGap, row.y, StepButtonWidth, row.height);
            Rect resetRect = new Rect(idRect.xMax + StepButtonGap, row.y, StepButtonWidth, row.height);
            Rect removeRect = new Rect(resetRect.xMax + StepButtonGap, row.y, StepButtonWidth, row.height);

            GUI.Label(indexRect, $"Step {index}", EditorStyles.miniLabel);

            SkillViewSequence sequence = sequenceProp != null
                ? sequenceProp.objectReferenceValue as SkillViewSequence
                : null;
            int stepIndex = stepIndexProp != null ? stepIndexProp.intValue : -1;

            string[] labels = BuildPopupLabels(sequence, stepIndex, out int currentPopupIndex);
            int nextPopupIndex = EditorGUI.Popup(popupRect, currentPopupIndex, labels);
            if (nextPopupIndex != currentPopupIndex)
            {
                if (nextPopupIndex <= 0)
                {
                    sequenceProp.objectReferenceValue = null;
                    stepIndexProp.intValue = -1;
                }
                else
                {
                    int resolvedIndex = nextPopupIndex == 1 && currentPopupIndex == 1
                        ? -1
                        : nextPopupIndex - (labels.Length != stepPopupLabels.Length ? 2 : 1);
                    if (resolvedIndex >= 0 && resolvedIndex < stepOptionCache.Length)
                    {
                        sequenceProp.objectReferenceValue = stepOptionCache[resolvedIndex].Sequence;
                        stepIndexProp.intValue = stepOptionCache[resolvedIndex].StepIndex;
                    }
                }

                if (useOverrideProp != null)
                {
                    useOverrideProp.boolValue = false;
                }
                if (localOverrideProp != null)
                {
                    localOverrideProp.isExpanded = false;
                }
                elem.isExpanded = false;
            }

            bool hasOverride = useOverrideProp != null && useOverrideProp.boolValue;
            if (GUI.Button(idRect, hasOverride ? "id" : ":id", EditorStyles.miniButton))
            {
                if (!hasOverride)
                {
                    TryActivateLocalOverride(elem);
                    hasOverride = useOverrideProp != null && useOverrideProp.boolValue;
                }

                elem.isExpanded = hasOverride && !elem.isExpanded;
            }

            using (new EditorGUI.DisabledScope(!hasOverride))
            {
                if (GUI.Button(resetRect, "R", EditorStyles.miniButton))
                {
                    if (useOverrideProp != null)
                    {
                        useOverrideProp.boolValue = false;
                    }

                    var local = elem.FindPropertyRelative("localOverrideStep");
                    if (local != null)
                    {
                        local.isExpanded = false;
                    }
                    elem.isExpanded = false;
                }
            }

            if (GUI.Button(removeRect, "-", EditorStyles.miniButton))
            {
                int oldSize = stepSelectionsProp.arraySize;
                stepSelectionsProp.DeleteArrayElementAtIndex(index);
                if (stepSelectionsProp.arraySize == oldSize)
                {
                    stepSelectionsProp.DeleteArrayElementAtIndex(index);
                }
                return;
            }

            if (
                elem.isExpanded
                && useOverrideProp != null
                && useOverrideProp.boolValue
                && localOverrideProp != null
            )
            {
                float y = row.yMax + StepExpandedPadding;
                float h = EditorGUI.GetPropertyHeight(localOverrideProp, true);
                Rect overrideRect = new Rect(
                    rect.x + 8f,
                    y,
                    rect.width - 10f,
                    h
                );
                EditorGUI.PropertyField(
                    overrideRect,
                    localOverrideProp,
                    new GUIContent("Local Override"),
                    true
                );
            }
        }

        private static void TryActivateLocalOverride(SerializedProperty elementProp)
        {
            var useOverrideProp = elementProp.FindPropertyRelative("useLocalOverride");
            var localOverrideProp = elementProp.FindPropertyRelative("localOverrideStep");
            var sequenceProp = elementProp.FindPropertyRelative("sequence");
            var stepIndexProp = elementProp.FindPropertyRelative("stepIndex");

            if (
                useOverrideProp == null
                || localOverrideProp == null
                || sequenceProp == null
                || stepIndexProp == null
            )
            {
                return;
            }

            var sequence = sequenceProp.objectReferenceValue as SkillViewSequence;
            int stepIndex = stepIndexProp.intValue;
            if (sequence == null || sequence.Steps == null || stepIndex < 0 || stepIndex >= sequence.Steps.Count)
            {
                return;
            }

            SkillViewStep source = sequence.Steps[stepIndex];
            if (source == null)
            {
                return;
            }

            WriteStepToProperty(localOverrideProp, source);
            useOverrideProp.boolValue = true;
        }

        private static void WriteStepToProperty(SerializedProperty targetProp, SkillViewStep source)
        {
            if (targetProp == null || source == null)
            {
                return;
            }

            SetEnum(targetProp, "stepType", (int)source.StepType);
            SetEnum(targetProp, "targetType", (int)source.TargetType);
            SetEnum(targetProp, "moveMode", (int)source.MoveMode);
            SetString(targetProp, "animationName", source.AnimationName);
            SetString(targetProp, "fallbackAnimationName", source.FallbackAnimationName);
            SetBool(targetProp, "loop", source.Loop);
            SetFloat(targetProp, "delay", source.Delay);
            SetFloat(targetProp, "duration", source.Duration);
            SetFloat(targetProp, "moveDistance", source.MoveDistance);
            SetInt(targetProp, "sortingOrder", source.SortingOrder);
            SetBool(targetProp, "flipX", source.FlipX);
            SetVector3(targetProp, "worldPosition", source.WorldPosition);
            SetVector3(targetProp, "offset", source.Offset);
            SetObjectRef(targetProp, "vfxPrefab", source.VfxPrefab);
            SetAnimationEvents(targetProp, source.AnimationEvents);
            SetBool(targetProp, "waitForAnimationEnd", source.WaitForAnimationEnd);
            SetBool(targetProp, "triggerHitEffect", source.TriggerHitEffect);
            SetInt(targetProp, "hitCount", source.HitCount);
        }

        private static void SetAnimationEvents(
            SerializedProperty targetProp,
            IReadOnlyList<SkillViewAnimationEvent> sourceEvents
        )
        {
            if (targetProp == null)
            {
                return;
            }

            var eventsProp = targetProp.FindPropertyRelative("animationEvents");
            if (eventsProp == null)
            {
                return;
            }

            eventsProp.ClearArray();
            if (sourceEvents == null || sourceEvents.Count == 0)
            {
                return;
            }

            eventsProp.arraySize = sourceEvents.Count;
            for (int i = 0; i < sourceEvents.Count; i++)
            {
                var sourceEvent = sourceEvents[i];
                var targetEventProp = eventsProp.GetArrayElementAtIndex(i);
                if (sourceEvent == null || targetEventProp == null)
                {
                    continue;
                }

                SetEnum(targetEventProp, "eventType", (int)sourceEvent.EventType);
                SetEnum(targetEventProp, "timing", (int)sourceEvent.Timing);
                SetString(targetEventProp, "animationEventName", sourceEvent.AnimationEventName);
                SetEnum(targetEventProp, "targetType", (int)sourceEvent.TargetType);
                SetEnum(targetEventProp, "spawnSocket", (int)sourceEvent.SpawnSocket);
                SetVector3(targetEventProp, "offset", sourceEvent.Offset);
                SetVector3(targetEventProp, "worldPosition", sourceEvent.WorldPosition);
                SetObjectRef(targetEventProp, "vfxPrefab", sourceEvent.VfxPrefab);
                SetBool(targetEventProp, "triggerHitEffect", sourceEvent.TriggerHitEffect);
                SetInt(targetEventProp, "hitCount", sourceEvent.HitCount);
                SetBool(targetEventProp, "enabled", sourceEvent.Enabled);
            }
        }

        private static void SetEnum(SerializedProperty root, string name, int value)
        {
            var p = root.FindPropertyRelative(name);
            if (p != null)
            {
                p.enumValueIndex = value;
            }
        }

        private static void SetString(SerializedProperty root, string name, string value)
        {
            var p = root.FindPropertyRelative(name);
            if (p != null)
            {
                p.stringValue = value ?? string.Empty;
            }
        }

        private static void SetBool(SerializedProperty root, string name, bool value)
        {
            var p = root.FindPropertyRelative(name);
            if (p != null)
            {
                p.boolValue = value;
            }
        }

        private static void SetFloat(SerializedProperty root, string name, float value)
        {
            var p = root.FindPropertyRelative(name);
            if (p != null)
            {
                p.floatValue = value;
            }
        }

        private static void SetInt(SerializedProperty root, string name, int value)
        {
            var p = root.FindPropertyRelative(name);
            if (p != null)
            {
                p.intValue = value;
            }
        }

        private static void SetVector3(SerializedProperty root, string name, Vector3 value)
        {
            var p = root.FindPropertyRelative(name);
            if (p != null)
            {
                p.vector3Value = value;
            }
        }

        private static void SetObjectRef(SerializedProperty root, string name, UnityEngine.Object value)
        {
            var p = root.FindPropertyRelative(name);
            if (p != null)
            {
                p.objectReferenceValue = value;
            }
        }

        private static void ShowSetSequenceMenu(SerializedProperty stepSelectionsProp, Rect buttonRect)
        {
            var menu = new GenericMenu();
            string propertyPath = stepSelectionsProp.propertyPath;
            var targetObject = stepSelectionsProp.serializedObject.targetObject;
            int targetInstanceId = targetObject != null ? targetObject.GetInstanceID() : 0;

            EnsureStepOptionCache();
            var uniqueSequences = new List<SkillViewSequence>();
            var seen = new HashSet<SkillViewSequence>();
            if (stepOptionCache != null)
            {
                for (int i = 0; i < stepOptionCache.Length; i++)
                {
                    var sequence = stepOptionCache[i].Sequence;
                    if (sequence != null && seen.Add(sequence))
                    {
                        uniqueSequences.Add(sequence);
                    }
                }
            }

            uniqueSequences.Sort(
                (a, b) =>
                {
                    string aLabel = !string.IsNullOrWhiteSpace(a.SequenceId) ? a.SequenceId : a.name;
                    string bLabel = !string.IsNullOrWhiteSpace(b.SequenceId) ? b.SequenceId : b.name;
                    return string.Compare(aLabel, bLabel, StringComparison.OrdinalIgnoreCase);
                }
            );

            if (uniqueSequences.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No SkillViewSequence assets found"));
            }
            else
            {
                for (int i = 0; i < uniqueSequences.Count; i++)
                {
                    SkillViewSequence captured = uniqueSequences[i];
                    string label = !string.IsNullOrWhiteSpace(captured.SequenceId)
                        ? captured.SequenceId
                        : captured.name;
                    menu.AddItem(
                        new GUIContent(label),
                        false,
                        () =>
                        {
                            UnityEngine.Object target = EditorUtility.InstanceIDToObject(targetInstanceId);
                            if (target == null)
                            {
                                return;
                            }

                            var so = new SerializedObject(target);
                            var prop = so.FindProperty(propertyPath);
                            if (prop == null)
                            {
                                return;
                            }

                            prop.ClearArray();
                            if (captured != null && captured.Steps != null)
                            {
                                for (int stepIndex = 0; stepIndex < captured.Steps.Count; stepIndex++)
                                {
                                    if (captured.Steps[stepIndex] == null)
                                    {
                                        continue;
                                    }

                                    int newIndex = prop.arraySize;
                                    prop.arraySize++;
                                    var elem = prop.GetArrayElementAtIndex(newIndex);
                                    elem.FindPropertyRelative("sequence").objectReferenceValue = captured;
                                    elem.FindPropertyRelative("stepIndex").intValue = stepIndex;
                                    var useOverrideProp = elem.FindPropertyRelative("useLocalOverride");
                                    if (useOverrideProp != null)
                                    {
                                        useOverrideProp.boolValue = false;
                                    }
                                    elem.isExpanded = false;
                                }
                            }

                            so.ApplyModifiedProperties();
                            EditorUtility.SetDirty(so.targetObject);
                        }
                    );
                }
            }

            menu.DropDown(buttonRect);
        }
    }
}

#endif

