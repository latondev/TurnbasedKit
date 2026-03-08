using System;
using UnityEngine;
using GameSystems;

namespace GameSystems.Skills
{
    /// <summary>
    /// Complete skill data with cooldown, cost, damage, effects
    /// </summary>
    [Serializable]
    public class SkillData
    {
        [SerializeField] private string skillId;
        [SerializeField] private string skillName;
        [SerializeField] private string description;
        [SerializeField] private SkillCategory category;
        [SerializeField] private SkillElement element;
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

        // Properties
        public string SkillId => skillId;
        public string SkillName => skillName;
        public string Description => description;
        public SkillCategory Category => category;
        public SkillElement Element => element;
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

        public SkillData(string id, string name, string description, SkillCategory category, 
            SkillElement element, int manaCost, float cooldown, float damage)
        {
            this.skillId = id;
            this.skillName = name;
            this.description = description;
            this.category = category;
            this.element = element;
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

        /// <summary>
        /// Unlocks the skill
        /// </summary>
        public void Unlock()
        {
            isUnlocked = true;
            currentLevel = 1;
            Debug.Log($"<color=green>🔓 Unlocked:</color> {skillName}");
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

        public string GetElementIcon()
        {
            return element switch
            {
                SkillElement.Fire => "🔥",
                SkillElement.Ice => "❄️",
                SkillElement.Lightning => "⚡",
                SkillElement.Earth => "🌍",
                SkillElement.Wind => "💨",
                SkillElement.Holy => "✨",
                SkillElement.Dark => "🌑",
                SkillElement.Physical => "⚔️",
                _ => "•"
            };
        }

        public Color GetElementColor()
        {
            return element switch
            {
                SkillElement.Fire => new Color(1f, 0.3f, 0.2f),
                SkillElement.Ice => new Color(0.3f, 0.8f, 1f),
                SkillElement.Lightning => new Color(1f, 1f, 0.3f),
                SkillElement.Earth => new Color(0.6f, 0.5f, 0.3f),
                SkillElement.Wind => new Color(0.8f, 1f, 0.8f),
                SkillElement.Holy => new Color(1f, 1f, 0.8f),
                SkillElement.Dark => new Color(0.5f, 0.3f, 0.6f),
                SkillElement.Physical => new Color(0.8f, 0.8f, 0.8f),
                _ => Color.white
            };
        }

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

    public enum SkillElement
    {
        Physical,
        Fire,
        Ice,
        Lightning,
        Earth,
        Wind,
        Holy,
        Dark
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
