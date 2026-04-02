using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using GameSystems;
using GameSystems.Battle;

namespace GameSystems.Skills
{
    /// <summary>
    /// Complete skill data with cooldown, cost, damage, effects
    /// </summary>
    [Serializable]
    public class SkillData : ISerializationCallbackReceiver
    {
        [SerializeField] private string skillId;
        [SerializeField] private string skillName;
        [SerializeField] private string description;
        [SerializeField] private SkillCategory category;
        [FormerlySerializedAs("element")]
        [SerializeField] private SkillDamageType damageType;
        [SerializeField] private int currentLevel;
        [SerializeField] private int maxLevel;
        
        // Requirements
        [SerializeField] private int requiredLevel;
        [SerializeField] private int manaCost;
        [SerializeField] private bool isUnlocked;
        
        // Cooldown
        [SerializeField] private float baseCooldown;
        [SerializeField] private float currentCooldown;
        [SerializeField] private bool isOnCooldown;
        
        // Power
        [SerializeField] private float baseDamage;
        [SerializeField] private float damagePerLevel;
        [SerializeField] private float range;
        [SerializeField] private int maxTargets;
        
        // Effects
        [SerializeField] private SkillEffectType effectType;
        [SerializeField] private float effectDuration;
        [SerializeField] private float effectValue;
        
        // Cast info
        [SerializeField] private float castTime;
        [SerializeField] private int totalCasts;
        
        [SerializeField] private Sprite icon;
        [FormerlySerializedAs("stepSelections")]
        [SerializeField, HideInInspector]
        private List<SkillViewStepSelection> legacyStepSelections = new List<SkillViewStepSelection>();
        [FormerlySerializedAs("stepSkills")]
        [FormerlySerializedAs("viewSequences")]
        [SerializeField, HideInInspector] private List<SkillViewSequence> legacyStepSequences = new List<SkillViewSequence>();
        [SerializeField, HideInInspector] private SkillViewSequence viewSequence; // Legacy single sequence fallback

        // Properties
        public string SkillId => skillId;
        public string SkillName => skillName;
        public string Description => description;
        public SkillCategory Category => category;
        public SkillDamageType DamageType => damageType;
        [Obsolete("Use DamageType instead.")]
        public SkillDamageType Element => damageType;
        public int CurrentLevel => currentLevel;
        public int MaxLevel => maxLevel;
        public int RequiredLevel => requiredLevel;
        public int ManaCost => manaCost;
        public bool IsUnlocked => isUnlocked;
        public float BaseCooldown => baseCooldown;
        public float CurrentCooldown => currentCooldown;
        public bool IsOnCooldown => isOnCooldown;
        public float BaseDamage => baseDamage;
        public float Range => range;
        public int MaxTargets => maxTargets;
        public SkillEffectType EffectType => effectType;
        public float EffectDuration => effectDuration;
        public float CastTime => castTime;
        public int TotalCasts => totalCasts;
        public Sprite Icon => icon;

        public SkillViewSequence ViewSequence => viewSequence;
        internal IReadOnlyList<SkillViewStepSelection> LegacyStepSelections => legacyStepSelections;
        internal bool HasLegacyStepSelections => legacyStepSelections != null && legacyStepSelections.Count > 0;

        public void SetViewSequence(SkillViewSequence sequence)
        {
            EnsureLegacyStepSelections();
            legacyStepSelections.Clear();
            AddAllStepsFromSequence(sequence, allowDuplicates: true);
            viewSequence = sequence;
            legacyStepSequences?.Clear();
        }

        public void InvalidateViewSequenceCache()
        {
            // SkillData no longer builds runtime step sequences.
        }

        public SkillData(string id, string name, string description, SkillCategory category, 
            SkillDamageType damageType, int manaCost, float cooldown, float damage)
        {
            this.skillId = id;
            this.skillName = name;
            this.description = description;
            this.category = category;
            this.damageType = damageType;
            this.manaCost = manaCost;
            this.baseCooldown = cooldown;
            this.baseDamage = damage;
            
            this.currentLevel = 0;
            this.maxLevel = 5;
            this.requiredLevel = 1;
            this.isUnlocked = false;
            this.currentCooldown = 0f;
            this.isOnCooldown = false;
            this.range = 5f;
            this.maxTargets = 1;
            this.damagePerLevel = damage * 0.2f;
            this.castTime = 0f;
            this.totalCasts = 0;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            EnsureLegacyStepSelections();
            MigrateLegacyStepSelections();
        }

        /// <summary>
        /// Unlocks the skill
        /// </summary>
        public void Unlock()
        {
            isUnlocked = true;
            currentLevel = 1;
            Debug.Log($"<color=green>🔓 Unlocked:</color> {skillName}");
        }

        private void EnsureLegacyStepSelections()
        {
            if (legacyStepSelections == null)
            {
                legacyStepSelections = new List<SkillViewStepSelection>();
            }
        }

        private void MigrateLegacyStepSelections()
        {
            if (legacyStepSequences == null)
            {
                legacyStepSequences = new List<SkillViewSequence>();
            }

            if (legacyStepSelections.Count == 0)
            {
                if (legacyStepSequences.Count > 0)
                {
                    for (int i = 0; i < legacyStepSequences.Count; i++)
                    {
                        AddAllStepsFromSequence(legacyStepSequences[i], allowDuplicates: true);
                    }
                }
                else if (viewSequence != null)
                {
                    AddAllStepsFromSequence(viewSequence, allowDuplicates: true);
                }
            }

            if (legacyStepSequences.Count > 0)
            {
                legacyStepSequences.Clear();
            }

        }

        private void AddAllStepsFromSequence(SkillViewSequence sequence, bool allowDuplicates = false)
        {
            if (sequence == null || sequence.Steps == null)
            {
                return;
            }

            for (int i = 0; i < sequence.Steps.Count; i++)
            {
                AddSingleStepSelection(sequence, i, allowDuplicates);
            }
        }

        private void AddSingleStepSelection(SkillViewSequence sequence, int stepIndex, bool allowDuplicates = false)
        {
            if (sequence == null || sequence.Steps == null || stepIndex < 0 || stepIndex >= sequence.Steps.Count)
            {
                return;
            }

            if (!allowDuplicates && HasLegacyStepSelection(sequence, stepIndex))
            {
                return;
            }

            var selection = new SkillViewStepSelection();
            selection.SetSelection(sequence, stepIndex);
            legacyStepSelections.Add(selection);
        }

        private bool HasLegacyStepSelection(SkillViewSequence sequence, int stepIndex)
        {
            if (legacyStepSelections == null)
            {
                return false;
            }

            for (int i = 0; i < legacyStepSelections.Count; i++)
            {
                var selection = legacyStepSelections[i];
                if (selection != null && selection.Sequence == sequence && selection.StepIndex == stepIndex)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Levels up the skill
        /// </summary>
        public bool LevelUp()
        {
            if (currentLevel >= maxLevel)
            {
                Debug.Log($"<color=orange>{skillName} is already max level!</color>");
                return false;
            }

            if (!isUnlocked)
            {
                Debug.Log($"<color=red>{skillName} is locked!</color>");
                return false;
            }

            currentLevel++;
            Debug.Log($"<color=yellow>⬆ Level Up!</color> {skillName} → Level {currentLevel}");
            return true;
        }

        /// <summary>
        /// Checks if skill can be cast (uses TimeManager if available)
        /// </summary>
        public bool CanCast(int currentMana)
        {
            if (!isUnlocked) return false;

            // Check mana
            if (currentMana < manaCost) return false;

            // Use TimeManager for cooldown check if available
            if (TimeManager.Instance != null)
            {
                return TimeManager.Instance.CooldownReady(skillId);
            }

            // Fallback to manual cooldown
            return !isOnCooldown;
        }

        /// <summary>
        /// Casts the skill (uses TimeManager if available)
        /// </summary>
        public void Cast()
        {
            if (!isUnlocked)
            {
                Debug.Log($"<color=red>Cannot cast {skillName} - not unlocked!</color>");
                return;
            }

            // Check cooldown using TimeManager if available
            if (TimeManager.Instance != null)
            {
                if (!TimeManager.Instance.CooldownReady(skillId))
                {
                    float remaining = TimeManager.Instance.CooldownRemaining(skillId);
                    Debug.Log($"<color=orange>Skill on cooldown: {remaining:F1}s remaining</color>");
                    return;
                }

                // Start cooldown in TimeManager
                TimeManager.Instance.StartCooldown(skillId, baseCooldown);
            }
            else
            {
                // Fallback to manual cooldown
                if (isOnCooldown)
                {
                    Debug.Log($"<color=orange>Skill on cooldown: {currentCooldown:F1}s remaining</color>");
                    return;
                }
                isOnCooldown = true;
                currentCooldown = baseCooldown;
            }

            totalCasts++;
            Debug.Log($"<color=cyan>✨ CAST:</color> {skillName} (Damage: {GetTotalDamage():F0})");
        }

        /// <summary>
        /// Updates cooldown (uses TimeManager if available)
        /// </summary>
        public void UpdateCooldown(float deltaTime)
        {
            // If using TimeManager, sync local state with TimeManager
            if (TimeManager.Instance != null)
            {
                float remaining = TimeManager.Instance.CooldownRemaining(skillId);
                currentCooldown = remaining;
                isOnCooldown = remaining > 0f;
                return;
            }

            // Fallback: manual cooldown
            if (isOnCooldown)
            {
                currentCooldown -= deltaTime;

                if (currentCooldown <= 0f)
                {
                    currentCooldown = 0f;
                    isOnCooldown = false;
                    Debug.Log($"<color=green>✓ Ready:</color> {skillName}");
                }
            }
        }

        /// <summary>
        /// Resets cooldown immediately (uses TimeManager if available)
        /// </summary>
        public void ResetCooldown()
        {
            if (TimeManager.Instance != null)
            {
                // Force cooldown to ready in TimeManager by starting with 0 duration
                TimeManager.Instance.StartCooldown(skillId, 0f);
            }

            currentCooldown = 0f;
            isOnCooldown = false;
            Debug.Log($"<color=cyan>Cooldown reset:</color> {skillName}");
        }

        /// <summary>
        /// Gets remaining cooldown time
        /// </summary>
        public float GetRemainingCooldown()
        {
            if (TimeManager.Instance != null)
            {
                return TimeManager.Instance.CooldownRemaining(skillId);
            }
            return currentCooldown;
        }

        /// <summary>
        /// Gets cooldown progress (0 = just started, 1 = ready)
        /// </summary>
        public float GetCooldownProgress()
        {
            if (TimeManager.Instance != null)
            {
                return TimeManager.Instance.CooldownProgress01(skillId);
            }
            return GetCooldownPercentage();
        }

        /// <summary>
        /// Gets total damage with level scaling
        /// </summary>
        public float GetTotalDamage()
        {
            return baseDamage + (damagePerLevel * (currentLevel - 1));
        }

        /// <summary>
        /// Gets cooldown percentage (0-1)
        /// </summary>
        public float GetCooldownPercentage()
        {
            if (baseCooldown <= 0f) return 0f;
            return 1f - (currentCooldown / baseCooldown);
        }

        /// <summary>
        /// Gets mana cost with level scaling
        /// </summary>
        public int GetScaledManaCost()
        {
            return manaCost + (currentLevel - 1);
        }

        public override string ToString()
        {
            string levelText = isUnlocked ? $"Lv.{currentLevel}" : "🔒";
            string cdText = isOnCooldown ? $" [CD: {currentCooldown:F1}s]" : "";
            return $"{GetCategoryIcon()} {skillName} {levelText}{cdText}";
        }

        public string GetCategoryIcon()
        {
            return category switch
            {
                SkillCategory.Active => "⚡",
                SkillCategory.Passive => "🛡️",
                SkillCategory.Ultimate => "🔥",
                SkillCategory.Buff => "✨",
                SkillCategory.Debuff => "💀",
                SkillCategory.Healing => "💚",
                _ => "•"
            };
        }

        public string GetDamageTypeIcon()
        {
            return damageType switch
            {
                SkillDamageType.Physical => "⚔️",
                SkillDamageType.Magic => "✨",
                SkillDamageType.TrueDamage => "💥",
                SkillDamageType.PercentHP => "❤️",
                SkillDamageType.LowestHPEnemy => "⬇️",
                SkillDamageType.HighestHPEnemy => "⬆️",
                SkillDamageType.AttackWithEffect => "🪄",
                _ => "•"
            };
        }

        [Obsolete("Use GetDamageTypeIcon instead.")]
        public string GetElementIcon() => GetDamageTypeIcon();

        public Color GetDamageTypeColor()
        {
            return damageType switch
            {
                SkillDamageType.Physical => new Color(0.8f, 0.8f, 0.8f),
                SkillDamageType.Magic => new Color(0.35f, 0.55f, 1f),
                SkillDamageType.TrueDamage => new Color(1f, 0.35f, 0.35f),
                SkillDamageType.PercentHP => new Color(1f, 0.55f, 0.65f),
                SkillDamageType.LowestHPEnemy => new Color(1f, 0.75f, 0.3f),
                SkillDamageType.HighestHPEnemy => new Color(0.3f, 0.85f, 0.8f),
                SkillDamageType.AttackWithEffect => new Color(0.7f, 0.55f, 1f),
                _ => Color.white
            };
        }

        [Obsolete("Use GetDamageTypeColor instead.")]
        public Color GetElementColor() => GetDamageTypeColor();

        public Color GetCategoryColor()
        {
            return category switch
            {
                SkillCategory.Active => new Color(1f, 0.7f, 0.3f),
                SkillCategory.Passive => new Color(0.5f, 0.8f, 1f),
                SkillCategory.Ultimate => new Color(1f, 0.3f, 0.3f),
                SkillCategory.Buff => new Color(0.5f, 1f, 0.5f),
                SkillCategory.Debuff => new Color(0.8f, 0.3f, 0.8f),
                SkillCategory.Healing => new Color(0.3f, 1f, 0.7f),
                _ => Color.white
            };
        }
    }
    [Flags]
    public enum SkillCategory
    {
        None = 0,
        Active = 1 << 0,     // 1
        Passive = 1 << 1,    // 2
        Ultimate = 1 << 2,   // 4
        Buff = 1 << 3,       // 8
        Debuff = 1 << 4,     // 16
        Healing = 1 << 5     // 32
    }

    public enum SkillDamageType
    {
        Physical,
        Magic,
        TrueDamage,
        PercentHP,
        LowestHPEnemy,
        HighestHPEnemy,
        AttackWithEffect
    }

    public enum SkillEffectType
    {
        None,
        Stun,
        Slow,
        Burn,
        Freeze,
        Poison,
        Heal,
        Shield,
        AttackBuff,
        DefenseBuff
    }
}
