using UnityEngine;

namespace GameSystems.Skills
{
    /// <summary>
    /// Complete Skill Controller with cooldown management
    /// </summary>
    public class SkillController : MonoBehaviour
    {
        [SerializeField] private string controllerName = "Skill System";
        [SerializeField] private bool debugMode = true;
        [SerializeField] private int playerLevel = 1;
        
        [SerializeField] private SkillIteratorData skillData = new SkillIteratorData();

        [Header("Resources")]
        [SerializeField] private int currentMana = 100;
        [SerializeField] private int maxMana = 100;
        [SerializeField] private float manaRegenRate = 2f;

        [Header("Runtime Info")]
        [SerializeField] private int currentIndex = -1;
        [SerializeField] private int totalIterations = 0;
        [SerializeField] private int totalCasts = 0;

        public string ControllerName 
        { 
            get => controllerName; 
            set => controllerName = value; 
        }

        public SkillIteratorData SkillData => skillData;
        public SkillData CurrentSkill => skillData.Current;
        public int CurrentMana => currentMana;
        public int MaxMana => maxMana;
        public int PlayerLevel => playerLevel;

        // Events
        public event System.Action<SkillData> OnSkillCast;
        public event System.Action<SkillData> OnSkillUnlocked;
        public event System.Action<SkillData> OnSkillLevelUp;
        public event System.Action<SkillData> OnCooldownComplete;

        void Start()
        {
            if (skillData.IsEmpty)
            {
                SetupDefaultSkills();
            }

            skillData.Initialize();
            UpdateRuntimeInfo();
            LogDebug("✅ Skill system ready!");
        }

        void Update()
        {
            UpdateAllCooldowns();
            RegenerateMana();
            HandleInput();
        }

        #region Setup

        private void SetupDefaultSkills()
        {
            // Fire Skills
            skillData.Add(new SkillData(
                "fire_fireball", "Fireball", "Launch a blazing fireball",
                SkillCategory.Active, SkillElement.Fire, 20, 3f, 50f
            ));

            skillData.Add(new SkillData(
                "fire_inferno", "Inferno", "Massive fire explosion",
                SkillCategory.Ultimate, SkillElement.Fire, 80, 30f, 200f
            ));

            // Ice Skills
            skillData.Add(new SkillData(
                "ice_frost", "Frost Bolt", "Freeze enemies with ice",
                SkillCategory.Active, SkillElement.Ice, 15, 2.5f, 40f
            ));

            skillData.Add(new SkillData(
                "ice_blizzard", "Blizzard", "Devastating ice storm",
                SkillCategory.Ultimate, SkillElement.Ice, 90, 35f, 250f
            ));

            // Lightning Skills
            skillData.Add(new SkillData(
                "lightning_bolt", "Lightning Bolt", "Strike with lightning",
                SkillCategory.Active, SkillElement.Lightning, 25, 4f, 60f
            ));

            skillData.Add(new SkillData(
                "lightning_storm", "Thunder Storm", "Call down lightning",
                SkillCategory.Ultimate, SkillElement.Lightning, 100, 40f, 300f
            ));

            // Healing Skills
            skillData.Add(new SkillData(
                "heal_light", "Healing Light", "Restore health",
                SkillCategory.Healing, SkillElement.Holy, 30, 5f, 80f
            ));

            // Buff Skills
            skillData.Add(new SkillData(
                "buff_haste", "Haste", "Increase speed",
                SkillCategory.Buff, SkillElement.Wind, 10, 1f, 0f
            ));

            skillData.Add(new SkillData(
                "buff_power", "Power Up", "Increase attack",
                SkillCategory.Buff, SkillElement.Physical, 15, 1.5f, 0f
            ));

            // Passive Skills
            skillData.Add(new SkillData(
                "passive_mana", "Mana Mastery", "Increase mana regen",
                SkillCategory.Passive, SkillElement.Holy, 0, 0f, 0f
            ));

            // Unlock starter skills
            skillData.Items[0].Unlock(); // Fireball
            skillData.Items[7].Unlock(); // Haste
        }

        #endregion

        #region Navigation

        public void Next()
        {
            SkillData skill = skillData.Next();
            UpdateRuntimeInfo();
            LogDebug($"→ {skill}");
        }

        public void Previous()
        {
            SkillData skill = skillData.Previous();
            UpdateRuntimeInfo();
            LogDebug($"← {skill}");
        }

        public void First()
        {
            SkillData skill = skillData.First();
            UpdateRuntimeInfo();
            LogDebug($"⏮ {skill}");
        }

        public void Last()
        {
            SkillData skill = skillData.Last();
            UpdateRuntimeInfo();
            LogDebug($"⏭ {skill}");
        }

        public void NextUnlocked()
        {
            SkillData skill = skillData.NextUnlocked();
            UpdateRuntimeInfo();
            if (skill != null)
                LogDebug($"→ Next Unlocked: {skill}");
        }

        public void NextReady()
        {
            SkillData skill = skillData.NextReady(currentMana);
            UpdateRuntimeInfo();
            if (skill != null)
                LogDebug($"→ Next Ready: {skill}");
        }

        #endregion

        #region Skill Actions

        public void CastCurrentSkill()
        {
            SkillData skill = CurrentSkill;
            if (skill == null)
            {
                LogDebug("<color=red>No skill selected!</color>");
                return;
            }

            if (!skill.CanCast(currentMana))
            {
                if (!skill.IsUnlocked)
                    LogDebug("<color=red>Skill is locked!</color>");
                else if (skill.IsOnCooldown)
                    LogDebug($"<color=orange>On cooldown: {skill.CurrentCooldown:F1}s</color>");
                else
                    LogDebug($"<color=orange>Not enough mana! Need {skill.ManaCost}, have {currentMana}</color>");
                return;
            }

            // Cast skill
            currentMana -= skill.GetScaledManaCost();
            skill.Cast();
            totalCasts++;

            OnSkillCast?.Invoke(skill);
            UpdateRuntimeInfo();
        }

        public void UnlockCurrentSkill()
        {
            SkillData skill = CurrentSkill;
            if (skill == null)
            {
                LogDebug("<color=red>No skill selected!</color>");
                return;
            }

            if (skill.IsUnlocked)
            {
                LogDebug($"<color=orange>{skill.SkillName} is already unlocked!</color>");
                return;
            }

            skill.Unlock();
            OnSkillUnlocked?.Invoke(skill);
        }

        public void LevelUpCurrentSkill()
        {
            SkillData skill = CurrentSkill;
            if (skill == null)
            {
                LogDebug("<color=red>No skill selected!</color>");
                return;
            }

            if (skill.LevelUp())
            {
                OnSkillLevelUp?.Invoke(skill);
            }
        }

        public void ResetCooldownCurrent()
        {
            SkillData skill = CurrentSkill;
            if (skill != null)
            {
                skill.ResetCooldown();
            }
        }

        public void ResetAllCooldowns()
        {
            foreach (var skill in skillData.Items)
            {
                skill.ResetCooldown();
            }
            LogDebug("<color=cyan>All cooldowns reset!</color>");
        }

        #endregion

        #region Cooldown Management

        private void UpdateAllCooldowns()
        {
            foreach (var skill in skillData.Items)
            {
                bool wasOnCooldown = skill.IsOnCooldown;
                skill.UpdateCooldown(Time.deltaTime);
                
                if (wasOnCooldown && !skill.IsOnCooldown)
                {
                    OnCooldownComplete?.Invoke(skill);
                }
            }
        }

        #endregion

        #region Mana Management

        private void RegenerateMana()
        {
            if (currentMana < maxMana)
            {
                currentMana = Mathf.Min(currentMana + (int)(manaRegenRate * Time.deltaTime), maxMana);
            }
        }

        public void RestoreMana(int amount)
        {
            currentMana = Mathf.Min(currentMana + amount, maxMana);
            LogDebug($"<color=cyan>+{amount} Mana → {currentMana}/{maxMana}</color>");
        }

        #endregion

        #region Level & Unlock

        public void LevelUpPlayer()
        {
            playerLevel++;
            LogDebug($"<color=yellow>🎉 Player Level Up! → {playerLevel}</color>");
        }

        public void UnlockAll()
        {
            foreach (var skill in skillData.Items)
            {
                if (!skill.IsUnlocked)
                {
                    skill.Unlock();
                    OnSkillUnlocked?.Invoke(skill);
                }
            }
            LogDebug("<color=green>🔓 Unlocked all skills!</color>");
        }

        #endregion

        #region Sorting

        public void SortByLevel()
        {
            skillData.SortByLevel();
            UpdateRuntimeInfo();
            LogDebug("Sorted by Level");
        }

        public void SortByDamage()
        {
            skillData.SortByDamage();
            UpdateRuntimeInfo();
            LogDebug("Sorted by Damage");
        }

        public void SortByCooldown()
        {
            skillData.SortByCooldown();
            UpdateRuntimeInfo();
            LogDebug("Sorted by Cooldown");
        }

        #endregion

        #region Input

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                Next();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Previous();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                CastCurrentSkill();
            }
            else if (Input.GetKeyDown(KeyCode.U))
            {
                UnlockCurrentSkill();
            }
            else if (Input.GetKeyDown(KeyCode.L))
            {
                LevelUpCurrentSkill();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                ResetCooldownCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                RestoreMana(50);
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                NextReady();
            }
        }

        #endregion

        #region Info

        private void UpdateRuntimeInfo()
        {
            currentIndex = skillData.CurrentIndex;
            totalIterations = skillData.Items.Count;
        }

        public void ShowSkillInfo()
        {
            Debug.Log("\n<color=cyan>═══════════════════════════════════════</color>");
            Debug.Log($"<color=yellow>⚡ {controllerName} ⚡</color>");
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>");
            Debug.Log($"Player Level: {playerLevel}");
            Debug.Log($"Mana: {currentMana}/{maxMana}");
            Debug.Log($"Total Skills: {skillData.Items.Count}");
            Debug.Log($"Unlocked: {skillData.GetUnlockedSkills().Count}");
            Debug.Log($"Total Casts: {totalCasts}");
            
            if (CurrentSkill != null)
            {
                Debug.Log($"\n<color=yellow>Current:</color> {CurrentSkill}");
            }
            
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>\n");
        }

        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"<color=magenta>[{controllerName}]</color> {message}");
            }
        }

        #endregion
    }
}
