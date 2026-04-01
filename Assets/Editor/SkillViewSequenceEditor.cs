#if UNITY_EDITOR

using GameSystems.Battle;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Battle.Editor
{
    [CustomEditor(typeof(SkillViewSequence))]
    public class SkillViewSequenceEditor : UnityEditor.Editor
    {
        private SerializedProperty _sequenceIdProp;
        private SerializedProperty _animationNameProp;
        private SerializedProperty _fallbackAnimationNameProp;
        private SerializedProperty _hitEventNameProp;
        private SerializedProperty _falldownEventNameProp;
        private SerializedProperty _idleAnimationNameProp;
        private SerializedProperty _stepsProp;

        private void OnEnable()
        {
            _sequenceIdProp = serializedObject.FindProperty("sequenceId");
            _animationNameProp = serializedObject.FindProperty("animationName");
            _fallbackAnimationNameProp = serializedObject.FindProperty("fallbackAnimationName");
            _hitEventNameProp = serializedObject.FindProperty("hitEventName");
            _falldownEventNameProp = serializedObject.FindProperty("falldownEventName");
            _idleAnimationNameProp = serializedObject.FindProperty("idleAnimationName");
            _stepsProp = serializedObject.FindProperty("steps");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Skill View Sequence drives battle visuals only. Use a preset for the common patterns, then tweak the step list below.",
                MessageType.Info);

            DrawPresetButtons();
            EditorGUILayout.Space(8f);

            if (_sequenceIdProp != null)
            {
                EditorGUILayout.PropertyField(_sequenceIdProp);
            }

            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("Spine Mapping", EditorStyles.boldLabel);
            DrawField(_animationNameProp, "Animation Name");
            DrawField(_fallbackAnimationNameProp, "Fallback Animation");
            DrawField(_hitEventNameProp, "Hit Event");
            DrawField(_falldownEventNameProp, "Falldown Event");
            DrawField(_idleAnimationNameProp, "Idle Animation");

            EditorGUILayout.Space(4f);

            if (_stepsProp != null)
            {
                EditorGUILayout.PropertyField(_stepsProp, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPresetButtons()
        {
            var sequence = target as SkillViewSequence;
            if (sequence == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Basic Strike"))
            {
                ApplyPreset(sequence, "Apply Basic Strike", () => sequence.ApplyBasicStrikePreset());
            }

            if (GUILayout.Button("Dash Through"))
            {
                ApplyPreset(sequence, "Apply Dash Through", () => sequence.ApplyDashThroughStrikePreset());
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Stationary Cast"))
            {
                ApplyPreset(sequence, "Apply Stationary Cast", () => sequence.ApplyStationaryCastPreset());
            }

            if (GUILayout.Button("Area Burst"))
            {
                ApplyPreset(sequence, "Apply Area Burst", () => sequence.ApplyAreaBurstPreset());
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Jump Behind"))
            {
                ApplyPreset(sequence, "Apply Jump Behind Strike", () => sequence.ApplyJumpBehindStrikePreset());
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Steps"))
            {
                Undo.RecordObject(sequence, "Clear Skill View Sequence");
                sequence.SetRuntimeSteps(null);
                EditorUtility.SetDirty(sequence);
            }

            if (GUILayout.Button("Ping Asset"))
            {
                EditorGUIUtility.PingObject(sequence);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawField(SerializedProperty property, string label)
        {
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));
            }
        }

        private void ApplyPreset(SkillViewSequence sequence, string undoName, System.Action applyAction)
        {
            if (sequence == null || applyAction == null)
            {
                return;
            }

            Undo.RecordObject(sequence, undoName);
            applyAction.Invoke();
            EditorUtility.SetDirty(sequence);
            serializedObject.Update();
        }
    }
}

#endif
