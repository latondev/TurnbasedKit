#if UNITY_EDITOR

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

            // Header + editable rows + spacing between rows.
            return (LineHeight + VSpacing) * 18f + 8f;
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
            var elementProp = property.FindPropertyRelative("element");
            var damageProp = property.FindPropertyRelative("baseDamage");
            var cooldownProp = property.FindPropertyRelative("baseCooldown");
            var manaProp = property.FindPropertyRelative("manaCost");
            var viewSequenceProp = property.FindPropertyRelative("viewSequence");

            var headerRect = new Rect(position.x, position.y, position.width, LineHeight);
            string title = BuildTitle(idProp, nameProp, categoryProp, elementProp, damageProp, cooldownProp, manaProp);
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, title, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = position.y + LineHeight + 4f;

                DrawLine(position, ref y, property.FindPropertyRelative("skillId"), "Skill Id");
                DrawLine(position, ref y, property.FindPropertyRelative("skillName"), "Skill Name");
                DrawLine(position, ref y, property.FindPropertyRelative("description"), "Description");
                DrawLine(position, ref y, categoryProp, "Category");
                DrawLine(position, ref y, elementProp, "Element");
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

                DrawSequenceField(position, ref y, viewSequenceProp);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private static string BuildTitle(
            SerializedProperty idProp,
            SerializedProperty nameProp,
            SerializedProperty categoryProp,
            SerializedProperty elementProp,
            SerializedProperty damageProp,
            SerializedProperty cooldownProp,
            SerializedProperty manaProp)
        {
            string id = idProp != null ? idProp.stringValue : "SkillData";
            string name = nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue) ? nameProp.stringValue : "Unnamed";
            string category = categoryProp != null ? categoryProp.enumDisplayNames[categoryProp.enumValueIndex] : "Unknown";
            string element = elementProp != null ? elementProp.enumDisplayNames[elementProp.enumValueIndex] : "Unknown";
            float damage = damageProp != null ? damageProp.floatValue : 0f;
            float cooldown = cooldownProp != null ? cooldownProp.floatValue : 0f;
            int mana = manaProp != null ? manaProp.intValue : 0;

            return $"{name} [{id}]  {category}/{element}  DMG {damage:F0}  CD {cooldown:F1}s  MP {mana}";
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

            float lineHeight = Mathf.Max(EditorGUI.GetPropertyHeight(first, true), EditorGUI.GetPropertyHeight(second, true));
            var left = new Rect(position.x, y, position.width * 0.5f - 4f, lineHeight);
            var right = new Rect(position.x + position.width * 0.5f + 4f, y, position.width * 0.5f - 4f, lineHeight);

            EditorGUI.PropertyField(left, first, new GUIContent(firstLabel), true);
            EditorGUI.PropertyField(right, second, new GUIContent(secondLabel), true);

            y += lineHeight + VSpacing;
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

            float lineHeight = Mathf.Max(
                EditorGUI.GetPropertyHeight(first, true),
                Mathf.Max(EditorGUI.GetPropertyHeight(second, true), EditorGUI.GetPropertyHeight(third, true)));

            float thirdWidth = position.width / 3f;
            var firstRect = new Rect(position.x, y, thirdWidth - 4f, lineHeight);
            var secondRect = new Rect(position.x + thirdWidth, y, thirdWidth - 4f, lineHeight);
            var thirdRect = new Rect(position.x + thirdWidth * 2f, y, thirdWidth - 4f, lineHeight);

            EditorGUI.PropertyField(firstRect, first, new GUIContent(firstLabel), true);
            EditorGUI.PropertyField(secondRect, second, new GUIContent(secondLabel), true);
            EditorGUI.PropertyField(thirdRect, third, new GUIContent(thirdLabel), true);

            y += lineHeight + VSpacing;
        }

        private static void DrawSequenceField(Rect position, ref float y, SerializedProperty sequenceProp)
        {
            if (sequenceProp == null)
            {
                return;
            }

            var rect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(rect, sequenceProp, new GUIContent("View Sequence"));

            y += rect.height + VSpacing;

            if (sequenceProp.objectReferenceValue is SkillViewSequence sequence)
            {
                var infoRect = new Rect(position.x, y, position.width, LineHeight);
                EditorGUI.LabelField(infoRect, $"Sequence: {sequence.SequenceId}  Steps: {sequence.Steps?.Count ?? 0}", EditorStyles.miniLabel);
                y += LineHeight + VSpacing;
            }
        }
    }
}

#endif
