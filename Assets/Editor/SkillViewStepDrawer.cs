#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GameSystems.Battle.Editor
{
    [CustomPropertyDrawer(typeof(SkillViewStep))]
    public class SkillViewStepDrawer : PropertyDrawer
    {
        private const float LineHeight = 18f;
        private const float VSpacing = 2f;

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

            // Header + editable rows + spacing.
            return (LineHeight + VSpacing) * 18f + 8f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var stepTypeProp = property.FindPropertyRelative("stepType");
            var targetTypeProp = property.FindPropertyRelative("targetType");
            var animationNameProp = property.FindPropertyRelative("animationName");
            var fallbackAnimationProp = property.FindPropertyRelative("fallbackAnimationName");
            var vfxProp = property.FindPropertyRelative("vfxPrefab");

            string title = BuildTitle(stepTypeProp, targetTypeProp, animationNameProp, fallbackAnimationProp, vfxProp);
            var headerRect = new Rect(position.x, position.y, position.width, LineHeight);
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, title, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = position.y + LineHeight + 4f;

                DrawLine(position, ref y, stepTypeProp, "Step Type");
                DrawLine(position, ref y, targetTypeProp, "Target Type");
                DrawLine(position, ref y, property.FindPropertyRelative("moveMode"), "Move Mode");
                DrawLine(position, ref y, animationNameProp, "Animation");
                DrawLine(position, ref y, fallbackAnimationProp, "Fallback");
                DrawLine(position, ref y, property.FindPropertyRelative("loop"), "Loop");
                DrawLine(position, ref y, property.FindPropertyRelative("delay"), "Delay");
                DrawLine(position, ref y, property.FindPropertyRelative("duration"), "Duration");
                DrawLine(position, ref y, property.FindPropertyRelative("moveDistance"), "Move Distance");
                DrawLine(position, ref y, property.FindPropertyRelative("sortingOrder"), "Sorting Order");
                DrawLine(position, ref y, property.FindPropertyRelative("flipX"), "Flip X");
                DrawLine(position, ref y, property.FindPropertyRelative("worldPosition"), "World Position");
                DrawLine(position, ref y, property.FindPropertyRelative("offset"), "Offset");
                DrawLine(position, ref y, vfxProp, "VFX Prefab");
                DrawLine(position, ref y, property.FindPropertyRelative("waitForAnimationEnd"), "Wait For Anim End");
                DrawLine(position, ref y, property.FindPropertyRelative("triggerHitEffect"), "Trigger Hit Effect");
                DrawLine(position, ref y, property.FindPropertyRelative("hitCount"), "Hit Count");

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private static string BuildTitle(
            SerializedProperty stepTypeProp,
            SerializedProperty targetTypeProp,
            SerializedProperty animationNameProp,
            SerializedProperty fallbackAnimationProp,
            SerializedProperty vfxProp)
        {
            string stepType = stepTypeProp != null ? stepTypeProp.enumDisplayNames[stepTypeProp.enumValueIndex] : "Step";
            string targetType = targetTypeProp != null ? targetTypeProp.enumDisplayNames[targetTypeProp.enumValueIndex] : "Target";
            string animation = animationNameProp != null && !string.IsNullOrEmpty(animationNameProp.stringValue)
                ? animationNameProp.stringValue
                : "skill";
            string fallback = fallbackAnimationProp != null && !string.IsNullOrEmpty(fallbackAnimationProp.stringValue)
                ? fallbackAnimationProp.stringValue
                : animation;
            string vfx = vfxProp != null && vfxProp.objectReferenceValue != null ? " +VFX" : string.Empty;

            return $"{stepType} | {targetType} | {animation} / {fallback}{vfx}";
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
    }
}

#endif
