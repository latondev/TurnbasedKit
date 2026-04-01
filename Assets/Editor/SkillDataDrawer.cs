#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using GameSystems.Battle;
using GameSystems.Skills;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Skills.Editor
{
    [CustomPropertyDrawer(typeof(SkillData))]
    public class SkillDataDrawer : PropertyDrawer
    {
        private const float LineHeight = 18f;
        private const float VSpacing = 2f;

        private enum BulkStepSelectionMode
        {
            Append,
            Replace
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
                    height += (LineHeight + VSpacing) * (stepSelectionsProp.arraySize);
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
            string title = BuildTitle(idProp, nameProp, categoryProp, damageTypeProp, damageProp, cooldownProp, manaProp);
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, title, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = position.y + LineHeight + 4f;

                DrawLine(position, ref y, property.FindPropertyRelative("skillId"), "Skill Id");
                DrawLine(position, ref y, property.FindPropertyRelative("skillName"), "Skill Name");
                DrawLine(position, ref y, property.FindPropertyRelative("description"), "Description");
                DrawLine(position, ref y, categoryProp, "Category");
                DrawLine(position, ref y, damageTypeProp, "Damage Type");
                DrawPairLine(position, ref y, property.FindPropertyRelative("currentLevel"), property.FindPropertyRelative("maxLevel"), "Current Level", "Max Level");
                DrawPairLine(position, ref y, property.FindPropertyRelative("requiredLevel"), manaProp, "Required Level", "Mana Cost");
                DrawLine(position, ref y, property.FindPropertyRelative("isUnlocked"), "Unlocked");
                DrawTripleLine(position, ref y,
                    property.FindPropertyRelative("baseCooldown"),
                    property.FindPropertyRelative("currentCooldown"),
                    property.FindPropertyRelative("isOnCooldown"),
                    "Base CD",
                    "Current CD",
                    "On CD");
                DrawPairLine(position, ref y, damageProp, property.FindPropertyRelative("damagePerLevel"), "Base Damage", "Dmg / Lv");
                DrawPairLine(position, ref y, property.FindPropertyRelative("range"), property.FindPropertyRelative("maxTargets"), "Range", "Max Targets");
                DrawTripleLine(position, ref y,
                    property.FindPropertyRelative("effectType"),
                    property.FindPropertyRelative("effectDuration"),
                    property.FindPropertyRelative("effectValue"),
                    "Effect Type",
                    "Effect Duration",
                    "Effect Value");
                DrawPairLine(position, ref y, property.FindPropertyRelative("castTime"), property.FindPropertyRelative("totalCasts"), "Cast Time", "Total Casts");
                DrawLine(position, ref y, property.FindPropertyRelative("icon"), "Icon");

                DrawStepSelectionListEditor(position, ref y, stepSelectionsProp);

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
            SerializedProperty manaProp)
        {
            string id = idProp != null ? idProp.stringValue : "SkillData";
            string name = nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue) ? nameProp.stringValue : "Unnamed";
            string category = categoryProp != null ? categoryProp.enumDisplayNames[categoryProp.enumValueIndex] : "Unknown";
            string damageType = damageTypeProp != null ? damageTypeProp.enumDisplayNames[damageTypeProp.enumValueIndex] : "Unknown";
            float damage = damageProp != null ? damageProp.floatValue : 0f;
            float cooldown = cooldownProp != null ? cooldownProp.floatValue : 0f;
            int mana = manaProp != null ? manaProp.intValue : 0;

            return $"{name} [{id}]  {category}/{damageType}  DMG {damage:F0}  CD {cooldown:F1}s  MP {mana}";
        }

        private static void DrawLine(Rect position, ref float y, SerializedProperty prop, string label)
        {
            if (prop == null)
            {
                return;
            }

            var rect = new Rect(position.x, y, position.width, EditorGUI.GetPropertyHeight(prop, true));
            EditorGUI.PropertyField(rect, prop, new GUIContent(label), true);
            y += rect.height + VSpacing;
        }

        private static void DrawPairLine(Rect position, ref float y, SerializedProperty first, SerializedProperty second, string firstLabel, string secondLabel)
        {
            if (first == null || second == null)
            {
                return;
            }

            float blockHeight = Mathf.Max(EditorGUI.GetPropertyHeight(first, true), EditorGUI.GetPropertyHeight(second, true)) + LineHeight + VSpacing;
            var left = new Rect(position.x, y, position.width * 0.5f - 4f, blockHeight);
            var right = new Rect(position.x + position.width * 0.5f + 4f, y, position.width * 0.5f - 4f, blockHeight);

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

                string sequenceLabel = !string.IsNullOrWhiteSpace(sequence.SequenceId) ? sequence.SequenceId : sequence.name;
                for (int stepIndex = 0; stepIndex < sequence.Steps.Count; stepIndex++)
                {
                    var step = sequence.Steps[stepIndex];
                    if (step == null)
                    {
                        continue;
                    }

                    options.Add(new StepOption
                    {
                        Sequence = sequence,
                        StepIndex = stepIndex,
                        SequenceLabel = sequenceLabel,
                        Label = BuildStepLabel(sequenceLabel, stepIndex, step)
                    });
                }
            }

            options.Sort((a, b) =>
            {
                int sequenceCompare = string.Compare(a.SequenceLabel, b.SequenceLabel, StringComparison.OrdinalIgnoreCase);
                if (sequenceCompare != 0)
                {
                    return sequenceCompare;
                }

                return a.StepIndex.CompareTo(b.StepIndex);
            });

            stepOptionCache = options.ToArray();
            stepPopupLabels = new string[stepOptionCache.Length + 1];
            stepPopupLabels[0] = "<None>";

            for (int i = 0; i < stepOptionCache.Length; i++)
            {
                stepPopupLabels[i + 1] = stepOptionCache[i].Label;
            }

            stepOptionCacheDirty = false;
        }

        private static string BuildStepLabel(string sequenceLabel, int stepIndex, SkillViewStep step)
        {
            if (step == null)
            {
                return $"{sequenceLabel} / #{stepIndex} <Null>";
            }

            string animationLabel = string.IsNullOrWhiteSpace(step.AnimationName) ? string.Empty : $" [{step.AnimationName}]";
            return $"{sequenceLabel} / #{stepIndex} {step.StepType}{animationLabel}";
        }

        private static string BuildMissingStepLabel(SkillViewSequence sequence, int stepIndex)
        {
            if (sequence == null)
            {
                return "<None>";
            }

            string sequenceLabel = !string.IsNullOrWhiteSpace(sequence.SequenceId) ? sequence.SequenceId : sequence.name;
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
                if (stepOptionCache[i].Sequence == sequence && stepOptionCache[i].StepIndex == stepIndex)
                {
                    return i + 1;
                }
            }

            return -1;
        }

        private static string[] BuildPopupLabels(SkillViewSequence sequence, int stepIndex, out int currentIndex)
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

        private static void DrawStepSelectionListEditor(Rect position, ref float y, SerializedProperty stepSelectionsProp)
        {
            if (stepSelectionsProp == null)
            {
                return;
            }

            EnsureStepOptionCache();

            Rect foldoutRect = new Rect(position.x, y, position.width - 140f, EditorGUIUtility.singleLineHeight);
            stepSelectionsProp.isExpanded = EditorGUI.Foldout(foldoutRect, stepSelectionsProp.isExpanded, "Step Skills", true);

            Rect addRect = new Rect(position.xMax - 138f, y, 24f, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(addRect, new GUIContent("+", "Add an empty step selection"), EditorStyles.miniButton))
            {
                stepSelectionsProp.isExpanded = true;
                stepSelectionsProp.arraySize++;
                var elem = stepSelectionsProp.GetArrayElementAtIndex(stepSelectionsProp.arraySize - 1);
                elem.FindPropertyRelative("sequence").objectReferenceValue = null;
                elem.FindPropertyRelative("stepIndex").intValue = -1;
                ClearLegacyStepSequenceFields(stepSelectionsProp);
            }

            Rect removeRect = new Rect(position.xMax - 112f, y, 56f, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(removeRect, new GUIContent("Remove", "Clear all step selections"), EditorStyles.miniButton))
            {
                ClearAllStepSelections(stepSelectionsProp);
            }

            Rect replaceRect = new Rect(position.xMax - 54f, y, 30f, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(replaceRect, new GUIContent("Set", "Replace the list with all steps from one sequence"), EditorStyles.miniButton))
            {
                ShowBulkMenu(stepSelectionsProp, replaceRect, BulkStepSelectionMode.Replace);
            }

            Rect refreshRect = new Rect(position.xMax - 24f, y, 24f, EditorGUIUtility.singleLineHeight);
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
            for (int i = 0; i < stepSelectionsProp.arraySize; i++)
            {
                var elem = stepSelectionsProp.GetArrayElementAtIndex(i);
                var sequenceProp = elem.FindPropertyRelative("sequence");
                var stepIndexProp = elem.FindPropertyRelative("stepIndex");

                Rect indexRect = new Rect(position.x + 4f, y, 58f, EditorGUIUtility.singleLineHeight);
                GUI.Label(indexRect, $"Step {i}", EditorStyles.miniLabel);

                Rect rowRect = new Rect(indexRect.xMax + 4f, y, position.width - 92f, EditorGUIUtility.singleLineHeight);
                int currentIndex;
                string[] labels = BuildPopupLabels(sequenceProp.objectReferenceValue as SkillViewSequence, stepIndexProp.intValue, out currentIndex);
                int nextIndex = EditorGUI.Popup(rowRect, currentIndex, labels);

                if (nextIndex != currentIndex)
                {
                    if (nextIndex == 0)
                    {
                        sequenceProp.objectReferenceValue = null;
                        stepIndexProp.intValue = -1;
                    }
                    else if (labels == stepPopupLabels)
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
                }

                Rect rowRemoveRect = new Rect(position.xMax - 24f, y, 24f, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(rowRemoveRect, "-", EditorStyles.miniButton))
                {
                    int oldSize = stepSelectionsProp.arraySize;
                    stepSelectionsProp.DeleteArrayElementAtIndex(i);
                    if (stepSelectionsProp.arraySize == oldSize)
                    {
                        stepSelectionsProp.DeleteArrayElementAtIndex(i);
                    }

                    ClearLegacyStepSequenceFields(stepSelectionsProp);
                    i--;
                }

                y += rowRect.height + VSpacing;
            }

            EditorGUI.indentLevel--;
        }

        private static void ShowBulkMenu(SerializedProperty stepSelectionsProp, Rect anchorRect, BulkStepSelectionMode mode)
        {
            if (stepSelectionsProp == null)
            {
                return;
            }

            EnsureStepOptionCache();

            var serializedObject = stepSelectionsProp.serializedObject;
            string propertyPath = stepSelectionsProp.propertyPath;
            var targetObjects = serializedObject != null ? serializedObject.targetObjects : null;
            if (targetObjects == null || targetObjects.Length == 0 || string.IsNullOrEmpty(propertyPath))
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

                string sequenceLabel = !string.IsNullOrWhiteSpace(sequence.SequenceId) ? sequence.SequenceId : sequence.name;
                string menuLabel = $"{sequenceLabel} ({sequence.Steps.Count} steps)";

                SkillViewSequence capturedSequence = sequence;
                menu.AddItem(new GUIContent(menuLabel), false, () =>
                {
                    ScheduleBulkSelectionApply(targetCopy, propertyPath, capturedSequence, mode);
                });
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

        private static void ScheduleBulkSelectionApply(UnityEngine.Object[] targetObjects, string propertyPath, SkillViewSequence sequence, BulkStepSelectionMode mode)
        {
            if (targetObjects == null || targetObjects.Length == 0 || sequence == null || string.IsNullOrEmpty(propertyPath))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                ApplyBulkSelectionToTargets(targetObjects, propertyPath, sequence, mode);
            };
        }

        private static void ApplyBulkSelectionToTargets(UnityEngine.Object[] targetObjects, string propertyPath, SkillViewSequence sequence, BulkStepSelectionMode mode)
        {
            if (targetObjects == null || targetObjects.Length == 0 || sequence == null || string.IsNullOrEmpty(propertyPath))
            {
                return;
            }

            string undoName = mode == BulkStepSelectionMode.Replace ? "Replace Step Skills" : "Append Step Skills";
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
                EditorUtility.SetDirty(targetObject);
            }
        }

        private static void ClearAllStepSelections(SerializedProperty stepSelectionsProp)
        {
            if (stepSelectionsProp == null)
            {
                return;
            }

            stepSelectionsProp.ClearArray();
            ClearLegacyStepSequenceFields(stepSelectionsProp.serializedObject, stepSelectionsProp.propertyPath);
        }

        private static void ClearLegacyStepSequenceFields(SerializedProperty stepSelectionsProp)
        {
            if (stepSelectionsProp == null)
            {
                return;
            }

            ClearLegacyStepSequenceFields(stepSelectionsProp.serializedObject, stepSelectionsProp.propertyPath);
        }

        private static void ClearLegacyStepSequenceFields(SerializedObject serializedObject, string stepSelectionsPath)
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

            var legacySequencesProp = serializedObject.FindProperty($"{parentPath}.legacyStepSequences");
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

        private static void AppendAllStepsFromSequence(SerializedProperty stepSelectionsProp, SkillViewSequence sequence)
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
            string thirdLabel)
        {
            if (first == null || second == null || third == null)
            {
                return;
            }

            float blockHeight = Mathf.Max(
                EditorGUI.GetPropertyHeight(first, true),
                Mathf.Max(EditorGUI.GetPropertyHeight(second, true), EditorGUI.GetPropertyHeight(third, true)));
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

        private static void DrawPropertyBlock(Rect position, SerializedProperty property, string label)
        {
            if (property == null)
            {
                return;
            }

            Rect indentedRect = EditorGUI.IndentedRect(position);
            Rect labelRect = new Rect(indentedRect.x, indentedRect.y, indentedRect.width, LineHeight);
            Rect fieldRect = new Rect(indentedRect.x, indentedRect.y + LineHeight + 1f, indentedRect.width, Mathf.Max(EditorGUI.GetPropertyHeight(property, true), LineHeight));

            EditorGUI.LabelField(labelRect, label, EditorStyles.miniLabel);
            EditorGUI.PropertyField(fieldRect, property, GUIContent.none, true);
        }

        // DrawSequenceField removed
    }
}

#endif
