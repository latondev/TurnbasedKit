#if UNITY_EDITOR

using GameSystems.Battle;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Battle.Editor
{
    public static class SkillViewSequenceMenu
    {
        private const string MenuRoot = "Assets/Create/Battle/Skill View Sequence/";

        [MenuItem(MenuRoot + "Basic Strike", false, 0)]
        private static void CreateBasicStrikeAsset()
        {
            CreatePresetAsset("BasicStrike", sequence => sequence.ApplyBasicStrikePreset());
        }

        [MenuItem(MenuRoot + "Dash Through Strike", false, 1)]
        private static void CreateDashThroughAsset()
        {
            CreatePresetAsset("DashThroughStrike", sequence => sequence.ApplyDashThroughStrikePreset());
        }

        [MenuItem(MenuRoot + "Stationary Cast", false, 2)]
        private static void CreateStationaryCastAsset()
        {
            CreatePresetAsset("StationaryCast", sequence => sequence.ApplyStationaryCastPreset());
        }

        [MenuItem(MenuRoot + "Area Burst", false, 3)]
        private static void CreateAreaBurstAsset()
        {
            CreatePresetAsset("AreaBurst", sequence => sequence.ApplyAreaBurstPreset());
        }

        [MenuItem(MenuRoot + "Jump Behind Strike", false, 4)]
        private static void CreateJumpBehindStrikeAsset()
        {
            CreatePresetAsset("JumpBehindStrike", sequence => sequence.ApplyJumpBehindStrikePreset());
        }

        private static void CreatePresetAsset(string defaultName, System.Action<SkillViewSequence> presetAction)
        {
            if (presetAction == null)
            {
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Create Skill View Sequence",
                defaultName,
                "asset",
                "Choose location for the new SkillViewSequence asset");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var sequence = ScriptableObject.CreateInstance<SkillViewSequence>();
            presetAction.Invoke(sequence);

            AssetDatabase.CreateAsset(sequence, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(sequence);
            Selection.activeObject = sequence;
        }
    }
}

#endif
