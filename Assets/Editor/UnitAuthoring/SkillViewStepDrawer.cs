using System;
using System.Collections.Generic;
using GameSystems.Battle;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Battle.Editor
{
    [CustomPropertyDrawer(typeof(SkillViewStep))]
    public class SkillViewStepDrawer : PropertyDrawer
    {
        private static IReadOnlyList<string> animationOptions = Array.Empty<string>();

        public static void SetAnimationOptions(IReadOnlyList<string> options)
        {
            animationOptions = options ?? Array.Empty<string>();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
            
            float height = EditorGUIUtility.singleLineHeight + 4f; // Header
            var stepTypeProp = property.FindPropertyRelative("stepType");
            SkillViewStepType stepType = (SkillViewStepType)stepTypeProp.enumValueIndex;

            height += EditorGUIUtility.singleLineHeight + 2f; // StepType

            if (ShowTargetType(stepType)) height += EditorGUIUtility.singleLineHeight + 2f;
            if (ShowMoveMode(stepType)) height += EditorGUIUtility.singleLineHeight + 2f;
            if (ShowAnimationParams(stepType))
            {
                height += (EditorGUIUtility.singleLineHeight + 2f) * 2;
            }
            if (ShowLoop(stepType)) height += EditorGUIUtility.singleLineHeight + 2f;
            
            height += EditorGUIUtility.singleLineHeight + 2f; // delay

            if (ShowDuration(stepType)) height += EditorGUIUtility.singleLineHeight + 2f;
            if (ShowMoveDistance(stepType)) height += EditorGUIUtility.singleLineHeight + 2f;
            if (ShowSortingOrder(stepType)) height += EditorGUIUtility.singleLineHeight + 2f;
            if (ShowFlipX(stepType)) height += EditorGUIUtility.singleLineHeight + 2f;
            if (ShowPosition(stepType)) height += EditorGUIUtility.singleLineHeight + 2f;
            if (ShowOffset(stepType)) height += EditorGUIUtility.singleLineHeight + 2f;
            if (ShowVfx(stepType)) height += EditorGUIUtility.singleLineHeight + 2f;
            if (ShowWaitNext(stepType)) height += EditorGUIUtility.singleLineHeight + 2f;
            if (ShowTriggerHit(stepType)) height += EditorGUIUtility.singleLineHeight + 2f;
            if (ShowHitCount(stepType)) height += EditorGUIUtility.singleLineHeight + 2f;
            
            return height + 4f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            var stepTypeProp = property.FindPropertyRelative("stepType");
            var animationNameProp = property.FindPropertyRelative("animationName");
            var vfxPrefabProp = property.FindPropertyRelative("vfxPrefab");

            SkillViewStepType stepType = (SkillViewStepType)stepTypeProp.enumValueIndex;
            
            string title = $"{stepType}";
            if (stepType == SkillViewStepType.PlayAnimation || stepType == SkillViewStepType.MoveToTarget || stepType == SkillViewStepType.MoveBack || stepType == SkillViewStepType.SetIdleAnimation)
            {
                title += $" [{animationNameProp.stringValue}]";
            }
            else if (stepType == SkillViewStepType.SpawnVfx && vfxPrefabProp.objectReferenceValue != null)
            {
                title += $" [{vfxPrefabProp.objectReferenceValue.name}]";
            }

            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, title, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                Rect fieldRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 4f, position.width, EditorGUIUtility.singleLineHeight);
                
                EditorGUI.PropertyField(fieldRect, stepTypeProp);
                fieldRect.y += EditorGUIUtility.singleLineHeight + 2f;

                if (ShowTargetType(stepType)) DrawField(ref fieldRect, property.FindPropertyRelative("targetType"));
                if (ShowMoveMode(stepType)) DrawField(ref fieldRect, property.FindPropertyRelative("moveMode"));
                if (ShowAnimationParams(stepType))
                {
                    DrawAnimationField(ref fieldRect, animationNameProp, "Animation Name");
                    DrawAnimationField(ref fieldRect, property.FindPropertyRelative("fallbackAnimationName"), "Fallback Animation");
                }
                if (ShowLoop(stepType)) DrawField(ref fieldRect, property.FindPropertyRelative("loop"));
                
                DrawField(ref fieldRect, property.FindPropertyRelative("delay"));

                if (ShowDuration(stepType)) DrawField(ref fieldRect, property.FindPropertyRelative("duration"));
                if (ShowMoveDistance(stepType)) DrawField(ref fieldRect, property.FindPropertyRelative("moveDistance"));
                if (ShowSortingOrder(stepType)) DrawField(ref fieldRect, property.FindPropertyRelative("sortingOrder"));
                if (ShowFlipX(stepType)) DrawField(ref fieldRect, property.FindPropertyRelative("flipX"));
                if (ShowPosition(stepType)) DrawField(ref fieldRect, property.FindPropertyRelative("worldPosition"));
                if (ShowOffset(stepType)) DrawField(ref fieldRect, property.FindPropertyRelative("offset"));
                if (ShowVfx(stepType)) DrawField(ref fieldRect, vfxPrefabProp);
                if (ShowWaitNext(stepType)) DrawField(ref fieldRect, property.FindPropertyRelative("waitForAnimationEnd"));
                if (ShowTriggerHit(stepType)) DrawField(ref fieldRect, property.FindPropertyRelative("triggerHitEffect"));
                if (ShowHitCount(stepType)) DrawField(ref fieldRect, property.FindPropertyRelative("hitCount"));

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private void DrawField(ref Rect rect, SerializedProperty property)
        {
            if (property == null) return;
            EditorGUI.PropertyField(rect, property, true);
            rect.y += EditorGUIUtility.singleLineHeight + 2f;
        }

        private void DrawAnimationField(ref Rect rect, SerializedProperty property, string label)
        {
            if (property == null)
            {
                return;
            }

            if (animationOptions == null || animationOptions.Count == 0)
            {
                EditorGUI.PropertyField(rect, property, new GUIContent(label), true);
                rect.y += EditorGUIUtility.singleLineHeight + 2f;
                return;
            }

            if (!string.IsNullOrWhiteSpace(property.stringValue) && !HasOption(animationOptions, property.stringValue))
            {
                property.stringValue = animationOptions[0];
            }

            string[] popupOptions = BuildPopupOptions(animationOptions);
            int currentIndex = Array.IndexOf(popupOptions, property.stringValue);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            Rect fieldRect = EditorGUI.PrefixLabel(rect, new GUIContent(label));
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
            var list = new List<string>();
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

        private bool ShowTargetType(SkillViewStepType type) => type != SkillViewStepType.Wait && type != SkillViewStepType.ResetSortingOrder && type != SkillViewStepType.SetSortingOrder && type != SkillViewStepType.SetIdleAnimation && type != SkillViewStepType.SetFlipX;
        private bool ShowMoveMode(SkillViewStepType type) => type == SkillViewStepType.MoveToTarget;
        private bool ShowAnimationParams(SkillViewStepType type) => type == SkillViewStepType.MoveToTarget || type == SkillViewStepType.MoveBack || type == SkillViewStepType.PlayAnimation || type == SkillViewStepType.SetIdleAnimation;
        private bool ShowLoop(SkillViewStepType type) => type == SkillViewStepType.PlayAnimation || type == SkillViewStepType.SetIdleAnimation;
        private bool ShowDuration(SkillViewStepType type) => type == SkillViewStepType.MoveToTarget || type == SkillViewStepType.MoveBack || type == SkillViewStepType.PlayAnimation || type == SkillViewStepType.Wait || type == SkillViewStepType.SetIdleAnimation;
        private bool ShowMoveDistance(SkillViewStepType type) => type == SkillViewStepType.MoveToTarget;
        private bool ShowSortingOrder(SkillViewStepType type) => type == SkillViewStepType.SetSortingOrder;
        private bool ShowFlipX(SkillViewStepType type) => type == SkillViewStepType.SetFlipX;
        private bool ShowPosition(SkillViewStepType type) => type == SkillViewStepType.SpawnVfx;
        private bool ShowOffset(SkillViewStepType type) => type == SkillViewStepType.MoveToTarget || type == SkillViewStepType.SpawnVfx;
        private bool ShowVfx(SkillViewStepType type) => type == SkillViewStepType.SpawnVfx;
        private bool ShowWaitNext(SkillViewStepType type) => type == SkillViewStepType.PlayAnimation || type == SkillViewStepType.SpawnVfx || type == SkillViewStepType.TriggerHit;
        private bool ShowTriggerHit(SkillViewStepType type) => type == SkillViewStepType.TriggerHit;
        private bool ShowHitCount(SkillViewStepType type) => type == SkillViewStepType.TriggerHit;
    }
}
