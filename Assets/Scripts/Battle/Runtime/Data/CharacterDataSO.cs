using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using GameSystems.Skills;
using GameSystems.Stats;

namespace GameSystems.Battle
{
    [CreateAssetMenu(fileName = "CharacterData", menuName = "ScriptableObjects/CharacterDataSO", order = 1)]
    public class CharacterDataSO : ScriptableObject, ISerializationCallbackReceiver
    {
        private const int CurrentActionSchemaVersion = 2;

        public int id;
        public string nameHero;

        [JsonConverter(typeof(StringEnumConverter))]
        public CharacterType type;

        [JsonIgnore] public Sprite iconAvatar;
        [JsonIgnore] public Sprite fullAvatar;
        [JsonIgnore] public Sprite cardAvatar;

        [JsonConverter(typeof(StringEnumConverter))]
        public CharacterRarity rarity;

        public int level = 1;
        public bool isUnlock;

        // Stats - using List<StatData> for Unity serialization
        [SerializeField] private List<StatData> stats = new List<StatData>();

        // Skills
        public SkillData skillBasic;
        public SkillData skillUltimate;
        public SkillData skillPassive;
        public SkillData skillAwaken;
        [SerializeField] private List<CombatActionData> actions = new List<CombatActionData>();
        [SerializeField, HideInInspector] private int actionSchemaVersion;

        // Properties
        public IReadOnlyList<StatData> Stats => stats.AsReadOnly();
        public IReadOnlyList<CombatActionData> Actions => actions;

        private void OnValidate()
        {
            EnsureActionsData();

            // Delay rename to avoid calling AssetDatabase during asset importing
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => {
                ChangeAssetName(id + "_" + nameHero);
            };
#endif
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (actions == null)
            {
                actions = new List<CombatActionData>();
            }
        }

        public void EnsureActionsData()
        {
            if (actions == null)
            {
                actions = new List<CombatActionData>();
            }

            bool requiresSchemaUpgrade = actionSchemaVersion < CurrentActionSchemaVersion;

            for (int i = actions.Count - 1; i >= 0; i--)
            {
                if (actions[i] == null)
                {
                    actions.RemoveAt(i);
                }
            }

            if (actions.Count == 0 || requiresSchemaUpgrade)
            {
                AddActionFromLegacySkill(
                    skillBasic,
                    CombatActionKind.SkillBasic,
                    "skill_basic",
                    "Basic Skill"
                );
                AddActionFromLegacySkill(
                    skillUltimate,
                    CombatActionKind.SkillUltimate,
                    "skill_ultimate",
                    "Ultimate Skill"
                );
                AddActionFromLegacySkill(
                    skillPassive,
                    CombatActionKind.SkillPassive,
                    "skill_passive",
                    "Passive Skill"
                );
                AddActionFromLegacySkill(
                    skillAwaken,
                    CombatActionKind.SkillAwaken,
                    "skill_awaken",
                    "Awaken Skill"
                );
            }

            if (requiresSchemaUpgrade)
            {
                MigrateMissingActionStepsFromLegacySkill(skillBasic, CombatActionKind.SkillBasic);
                MigrateMissingActionStepsFromLegacySkill(skillUltimate, CombatActionKind.SkillUltimate);
                MigrateMissingActionStepsFromLegacySkill(skillPassive, CombatActionKind.SkillPassive);
                MigrateMissingActionStepsFromLegacySkill(skillAwaken, CombatActionKind.SkillAwaken);
            }

            if (FindActionByKindInternal(CombatActionKind.BasicAttack) == null)
            {
                var basicAttackAction = new CombatActionData();
                basicAttackAction.SetActionKind(CombatActionKind.BasicAttack);
                basicAttackAction.SetActionId("basic_attack");
                basicAttackAction.SetDisplayName("Basic Attack");
                basicAttackAction.SetMetadata("attack", "war_attack", "hit", "falldown", "idle");
                basicAttackAction.EnsureDefaults();
                actions.Insert(0, basicAttackAction);
            }

            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                if (action == null)
                {
                    continue;
                }

                action.EnsureDefaults();
            }

            if (actionSchemaVersion != CurrentActionSchemaVersion)
            {
                actionSchemaVersion = CurrentActionSchemaVersion;
            }

        }

        public CombatActionData GetAction(CombatActionKind kind)
        {
            EnsureActionsData();
            return FindActionByKindInternal(kind);
        }

        public CombatActionData GetActionBySkill(SkillData skill)
        {
            EnsureActionsData();
            if (skill == null || actions == null)
            {
                return null;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                if (action != null && action.SkillRef == skill)
                {
                    return action;
                }
            }

            return null;
        }

        public CombatActionData ResolveActionForBattle(
            GameSystems.AutoBattle.ActionType actionType,
            SkillData equippedSkill = null
        )
        {
            EnsureActionsData();

            if (actionType == GameSystems.AutoBattle.ActionType.Attack)
            {
                return FindActionByKindInternal(CombatActionKind.BasicAttack);
            }

            CombatActionData bySkill = GetActionBySkill(equippedSkill);
            if (bySkill != null)
            {
                return bySkill;
            }

            CombatActionData basicSkill = FindActionByKindInternal(CombatActionKind.SkillBasic);
            if (basicSkill != null)
            {
                return basicSkill;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                if (action == null)
                {
                    continue;
                }

                if (
                    action.ActionKind == CombatActionKind.SkillBasic
                    || action.ActionKind == CombatActionKind.SkillUltimate
                    || action.ActionKind == CombatActionKind.SkillPassive
                    || action.ActionKind == CombatActionKind.SkillAwaken
                    || action.ActionKind == CombatActionKind.Custom
                )
                {
                    return action;
                }
            }

            return FindActionByKindInternal(CombatActionKind.BasicAttack);
        }

        [ContextMenu("SaveJson")]
        public void SaveJson()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            Debug.Log(json);
        }

        private void ChangeAssetName(string newName)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(newName)) return;

            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string currentName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (currentName != newName)
                {
                    UnityEditor.AssetDatabase.RenameAsset(assetPath, newName);
                    UnityEditor.AssetDatabase.SaveAssets();
                }
            }
#endif
        }

        private CombatActionData FindActionByKindInternal(CombatActionKind kind)
        {
            if (actions == null)
            {
                return null;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                if (action != null && action.ActionKind == kind)
                {
                    return action;
                }
            }

            return null;
        }

        private bool AddActionFromLegacySkill(
            SkillData legacySkill,
            CombatActionKind kind,
            string actionId,
            string displayName
        )
        {
            if (legacySkill == null)
            {
                return false;
            }

            if (FindActionByKindInternal(kind) != null)
            {
                return false;
            }

            var action = new CombatActionData();
            action.SetActionKind(kind);
            action.SetActionId(actionId);
            action.SetDisplayName(displayName);
            action.SetSkillRef(legacySkill);
            ApplyLegacySkillVisualData(action, legacySkill, overwriteExistingSteps: true);

            actions.Add(action);
            return true;
        }

        private void MigrateMissingActionStepsFromLegacySkill(SkillData legacySkill, CombatActionKind kind)
        {
            if (legacySkill == null)
            {
                return;
            }

            CombatActionData action = FindActionByKindInternal(kind);
            if (action == null || HasActionSteps(action))
            {
                return;
            }

            if (action.SkillRef == null)
            {
                action.SetSkillRef(legacySkill);
            }

            ApplyLegacySkillVisualData(action, legacySkill, overwriteExistingSteps: false);
        }

        private static bool HasActionSteps(CombatActionData action)
        {
            return action != null && action.StepSelections != null && action.StepSelections.Count > 0;
        }

        private static void ApplyLegacySkillVisualData(
            CombatActionData action,
            SkillData legacySkill,
            bool overwriteExistingSteps
        )
        {
            if (action == null || legacySkill == null)
            {
                return;
            }

            bool actionAlreadyHasSteps = HasActionSteps(action);
            if (overwriteExistingSteps || !actionAlreadyHasSteps)
            {
                action.SetStepSelectionsFrom(legacySkill.LegacyStepSelections);
                actionAlreadyHasSteps = HasActionSteps(action);
            }

            if (actionAlreadyHasSteps)
            {
                return;
            }

            SkillViewSequence sequence = legacySkill.ViewSequence;
            bool canStoreLegacySequence = sequence != null
                && (sequence.hideFlags & HideFlags.HideAndDontSave) == 0;

            if (canStoreLegacySequence)
            {
                action.SetLegacyViewSequence(sequence);
                action.SetMetadata(
                    sequence.AnimationName,
                    sequence.FallbackAnimationName,
                    sequence.HitEventName,
                    sequence.FalldownEventName,
                    sequence.IdleAnimationName
                );
            }
            else
            {
                action.EnsureDefaults();
            }
        }

        /// <summary>
        /// Get stat value by statId
        /// </summary>
        public float GetStatValue(string statId)
        {
            foreach (var stat in stats)
            {
                if (stat.StatId == statId)
                    return stat.BaseValue;
            }
            return 0;
        }

        /// <summary>
        /// Get stat by statId
        /// </summary>
        public StatData GetStat(string statId)
        {
            foreach (var stat in stats)
            {
                if (stat.StatId == statId)
                    return stat;
            }
            return null;
        }

        /// <summary>
        /// Convert to List<Stat> for runtime use (StatsSystem)
        /// </summary>
        public List<Stat> ToStatsList()
        {
            var result = new List<Stat>();
            foreach (var statData in stats)
            {
                result.Add(statData.ToStat());
            }
            return result;
        }

        /// <summary>
        /// Add a new stat to the list
        /// </summary>
        public void AddStat(StatData stat)
        {
            if (stat != null && !stats.Exists(s => s.StatId == stat.StatId))
            {
                stats.Add(stat);
            }
        }

        /// <summary>
        /// Remove a stat by statId
        /// </summary>
        public void RemoveStat(string statId)
        {
            stats.RemoveAll(s => s.StatId == statId);
        }

        /// <summary>
        /// Clear all stats
        /// </summary>
        public void ClearStats()
        {
            stats.Clear();
        }

        /// <summary>
        /// Initialize with default stats if empty
        /// </summary>
        [ContextMenu("InitializeDefaultStats")]
        public void InitializeDefaultStats()
        {
            if (stats.Count == 0)
            {
                stats = StatData.CreateDefaultStats();
            }
        }
    }

    public enum CharacterType
    {
        Hero,
        Enemy,
        Boss
    }

    public enum CharacterRarity
    {
        N,
        R,
        SR,
        SSR,
        UR
    }
}
