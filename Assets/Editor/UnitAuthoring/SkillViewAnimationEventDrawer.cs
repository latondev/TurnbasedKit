#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using GameSystems.Battle;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Battle.Editor
{
    [CustomPropertyDrawer(typeof(SkillViewAnimationEvent))]
    public sealed class SkillViewAnimationEventDrawer : PropertyDrawer
    {
        private static IReadOnlyList<string> eventNameOptions = Array.Empty<string>();

        public static void SetEventOptions(IReadOnlyList<string> options)
        {
            eventNameOptions = options ?? Array.Empty<string>();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            SkillViewAnimationEventType eventType = GetEventType(property);
            float height = EditorGUIUtility.singleLineHeight + 4f;
            height += EditorGUIUtility.singleLineHeight + 2f; // Event Type
            height += EditorGUIUtility.singleLineHeight + 2f; // Timing
            height += EditorGUIUtility.singleLineHeight + 2f; // Animation Event Name
            height += EditorGUIUtility.singleLineHeight + 2f; // Target Type

            if (eventType == SkillViewAnimationEventType.SpawnVfx)
            {
                height += EditorGUIUtility.singleLineHeight + 2f; // Spawn Socket
                height += EditorGUIUtility.singleLineHeight + 2f; // Vfx Prefab
            }
            else if (eventType == SkillViewAnimationEventType.TriggerHit)
            {
                height += EditorGUIUtility.singleLineHeight + 2f; // Trigger Hit Effect
                height += EditorGUIUtility.singleLineHeight + 2f; // Hit Count
            }

            height += EditorGUIUtility.singleLineHeight + 2f; // Enabled
            return height + 4f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var eventTypeProp = property.FindPropertyRelative("eventType");
            var timingProp = property.FindPropertyRelative("timing");
            var animationEventNameProp = property.FindPropertyRelative("animationEventName");
            var targetTypeProp = property.FindPropertyRelative("targetType");
            var spawnSocketProp = property.FindPropertyRelative("spawnSocket");
            var vfxPrefabProp = property.FindPropertyRelative("vfxPrefab");
            var triggerHitEffectProp = property.FindPropertyRelative("triggerHitEffect");
            var hitCountProp = property.FindPropertyRelative("hitCount");
            var enabledProp = property.FindPropertyRelative("enabled");

            SkillViewAnimationEventType eventType = GetEventType(property);
            string title = BuildTitle(property, eventType);

            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, title, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                Rect fieldRect = new Rect(
                    position.x,
                    position.y + EditorGUIUtility.singleLineHeight + 4f,
                    position.width,
                    EditorGUIUtility.singleLineHeight);

                DrawField(ref fieldRect, eventTypeProp, "Event Type");
                DrawField(ref fieldRect, timingProp, "Timing");
                DrawAnimationEventNameField(ref fieldRect, animationEventNameProp);
                DrawField(ref fieldRect, targetTypeProp, "Target Type");

                if (eventType == SkillViewAnimationEventType.SpawnVfx)
                {
                    DrawField(ref fieldRect, spawnSocketProp, "Spawn Socket");
                    DrawField(ref fieldRect, vfxPrefabProp, "VFX Prefab");
                }
                else if (eventType == SkillViewAnimationEventType.TriggerHit)
                {
                    DrawField(ref fieldRect, triggerHitEffectProp, "Trigger Hit Effect");
                    DrawField(ref fieldRect, hitCountProp, "Hit Count");
                }

                DrawField(ref fieldRect, enabledProp, "Enabled (toggle)");

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private static string BuildTitle(SerializedProperty property, SkillViewAnimationEventType eventType)
        {
            string timingLabel = GetEnumLabel(property.FindPropertyRelative("timing"));
            string targetLabel = GetEnumLabel(property.FindPropertyRelative("targetType"));
            string title = $"{eventType} [{timingLabel}] -> {targetLabel}";

            if (eventType == SkillViewAnimationEventType.TriggerHit)
            {
                bool triggerHitEffect = property.FindPropertyRelative("triggerHitEffect")?.boolValue ?? true;
                title += triggerHitEffect ? " [Hit Effect]" : " [Logic Hit]";
            }
            else if (eventType == SkillViewAnimationEventType.SpawnVfx)
            {
                var spawnSocketProp = property.FindPropertyRelative("spawnSocket");
                if (spawnSocketProp != null && spawnSocketProp.enumValueIndex != (int)UnitSocketPoint.None)
                {
                    title += $" [{spawnSocketProp.enumDisplayNames[spawnSocketProp.enumValueIndex]}]";
                }
            }

            var enabledProp = property.FindPropertyRelative("enabled");
            if (enabledProp != null && !enabledProp.boolValue)
            {
                title += " [Disabled]";
            }

            return title;
        }

        private static void DrawField(ref Rect rect, SerializedProperty property, string label)
        {
            if (property == null)
            {
                return;
            }

            float height = EditorGUI.GetPropertyHeight(property, true);
            Rect fieldRect = new Rect(rect.x, rect.y, rect.width, height);
            EditorGUI.PropertyField(fieldRect, property, new GUIContent(label), true);
            rect.y += height + 2f;
        }

        private void DrawAnimationEventNameField(ref Rect rect, SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            if (eventNameOptions == null || eventNameOptions.Count == 0)
            {
                DrawField(ref rect, property, "Animation Event Name");
                return;
            }

            if (
                !string.IsNullOrWhiteSpace(property.stringValue)
                && !HasOption(eventNameOptions, property.stringValue))
            {
                property.stringValue = string.Empty;
            }

            string[] popupOptions = BuildPopupOptions(eventNameOptions);
            int currentIndex = Array.IndexOf(popupOptions, property.stringValue);
            if (currentIndex < 0)
            {
                currentIndex = Array.IndexOf(popupOptions, "<None>");
                if (currentIndex < 0)
                {
                    currentIndex = 0;
                }
            }

            Rect fieldRect = EditorGUI.PrefixLabel(rect, new GUIContent("Animation Event Name"));
            int nextIndex = EditorGUI.Popup(fieldRect, currentIndex, popupOptions);
            if (nextIndex >= 0 && nextIndex < popupOptions.Length && nextIndex != currentIndex)
            {
                string nextValue = popupOptions[nextIndex] == "<None>" ? string.Empty : popupOptions[nextIndex];
                if (property.stringValue != nextValue)
                {
                    property.stringValue = nextValue;
                }
            }

            rect.y += EditorGUIUtility.singleLineHeight + 2f;
        }

        private static string[] BuildPopupOptions(IReadOnlyList<string> options)
        {
            var list = new System.Collections.Generic.List<string>();
            if (options != null)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    string option = options[i];
                    if (string.IsNullOrWhiteSpace(option))
                    {
                        continue;
                    }

                    if (!list.Contains(option))
                    {
                        list.Add(option);
                    }
                }
            }

            list.Add("<None>");
            return list.ToArray();
        }

        private static bool HasOption(IReadOnlyList<string> options, string value)
        {
            if (options == null || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            for (int i = 0; i < options.Count; i++)
            {
                if (string.Equals(options[i], value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static SkillViewAnimationEventType GetEventType(SerializedProperty property)
        {
            var eventTypeProp = property != null ? property.FindPropertyRelative("eventType") : null;
            return eventTypeProp != null
                ? (SkillViewAnimationEventType)eventTypeProp.enumValueIndex
                : SkillViewAnimationEventType.SpawnVfx;
        }

        private static string GetEnumLabel(SerializedProperty property)
        {
            if (property == null || property.enumDisplayNames == null || property.enumDisplayNames.Length == 0)
            {
                return "?";
            }

            int index = Mathf.Clamp(property.enumValueIndex, 0, property.enumDisplayNames.Length - 1);
            return property.enumDisplayNames[index];
        }
    }
}

#endif
