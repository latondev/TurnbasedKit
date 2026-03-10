using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Skills
{
    public class AutoCastSkillController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private SkillController skillController;
        
        [Header("Settings")]
        [SerializeField] private bool enableAutoCast = true;
        [SerializeField] private float checkInterval = 0.1f;
        [SerializeField] private bool debugMode = true;
        
        [Header("Filter")]
        [SerializeField] private bool onlyActiveSkills = true;
        
        // Thay List bằng flags enum - chọn nhiều trong Inspector
        [SerializeField] private SkillCategory allowedCategories = 
            SkillCategory.Active | SkillCategory.Buff;
        
        [Header("Runtime Info")]
        [SerializeField] private List<SkillData> autoSkills = new List<SkillData>();
        [SerializeField] private int totalAutoCasts = 0;
        
        private float nextCheckTime;

        void Start()
        {
            if (skillController == null)
            {
                skillController = GetComponent<SkillController>();
            }
            
            if (skillController == null)
            {
                Debug.LogError("AutoCastSkillController: SkillController not found!");
                enabled = false;
                return;
            }
            
            skillController.OnSkillUnlocked += OnSkillUnlocked;
            RefreshAutoSkillList();
            
            LogDebug($"✅ AutoCast initialized with {autoSkills.Count} skills");
        }

        void Update()
        {
            if (!enableAutoCast) return;
            if (Time.time < nextCheckTime) return;
            nextCheckTime = Time.time + checkInterval;
            
            TryAutoCastSkills();
        }

        void OnDestroy()
        {
            if (skillController != null)
            {
                skillController.OnSkillUnlocked -= OnSkillUnlocked;
            }
        }

        #region Auto Cast Logic

        private void TryAutoCastSkills()
        {
            foreach (var skill in autoSkills)
            {
                if (skill == null) continue;
                
                if (skill.CanCast(skillController.CurrentMana))
                {
                    CastSkillAuto(skill);
                }
            }
        }

        private void CastSkillAuto(SkillData skill)
        {
            if (skill == null) return;
            
            skill.Cast();
            SetCurrentSkillAndCast(skill);
            
            totalAutoCasts++;
            LogDebug($"🤖 Auto-cast: {skill.SkillName} (Mana: {skill.GetScaledManaCost()})");
        }

        private void SetCurrentSkillAndCast(SkillData skill)
        {
            var collection = skillController.SkillData.Items;
            int index = collection.IndexOf(skill);

            if (index < 0) return;

            skillController.SkillData.CurrentIndex = index;
            skillController.CastCurrentSkill();
        }

        #endregion

        #region Skill Management

        private void OnSkillUnlocked(SkillData skill)
        {
            if (skill == null) return;
            
            if (ShouldAutocast(skill))
            {
                AddSkillToAutoList(skill);
                LogDebug($"➕ Added to auto-cast: {skill.SkillName}");
            }
        }

        private bool ShouldAutocast(SkillData skill)
        {
            if (!skill.IsUnlocked) return false;
            
            if (onlyActiveSkills && skill.Category == SkillCategory.Passive)
                return false;
            
            // Check flag thay vì Contains()
            if (!allowedCategories.HasFlag(skill.Category))
                return false;
            
            return true;
        }

        public void AddSkillToAutoList(SkillData skill)
        {
            if (skill == null) return;
            if (autoSkills.Contains(skill)) return;
            
            autoSkills.Add(skill);
        }

        public void RemoveSkillFromAutoList(SkillData skill)
        {
            if (skill == null) return;
            
            autoSkills.Remove(skill);
            LogDebug($"➖ Removed from auto-cast: {skill.SkillName}");
        }

        public void RefreshAutoSkillList()
        {
            autoSkills.Clear();
            
            if (skillController == null) return;
            
            foreach (var skill in skillController.SkillData.Items)
            {
                if (ShouldAutocast(skill))
                {
                    autoSkills.Add(skill);
                }
            }
            
            LogDebug($"🔄 Refreshed auto-cast list: {autoSkills.Count} skills");
        }

        public void ClearAutoSkillList()
        {
            autoSkills.Clear();
            LogDebug("🗑️ Cleared auto-cast list");
        }

        #endregion

        #region Public Controls

        public void SetAutoCastEnabled(bool enabled)
        {
            enableAutoCast = enabled;
            LogDebug($"AutoCast: {(enabled ? "ON" : "OFF")}");
        }

        public void ToggleAutoCast()
        {
            SetAutoCastEnabled(!enableAutoCast);
        }

        /// <summary>
        /// Set allowed categories bằng code
        /// </summary>
        public void SetAllowedCategories(SkillCategory categories)
        {
            allowedCategories = categories;
            RefreshAutoSkillList();
            LogDebug($"Updated allowed categories: {allowedCategories}");
        }

        #endregion

        #region Integration Helpers

        public void UnlockAllAndRefresh()
        {
            if (skillController == null) return;
            
            skillController.UnlockAll();
            RefreshAutoSkillList();
            
            LogDebug($"🔓 Unlocked all skills! Auto-cast list: {autoSkills.Count}");
        }

        #endregion

        #region Info & Debug

        public void ShowInfo()
        {
            Debug.Log("\n═══════════════════════════════════════");
            Debug.Log("🤖 AutoCast Skill Controller 🤖");
            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"Enabled: {enableAutoCast}");
            Debug.Log($"Allowed Categories: {allowedCategories}");
            Debug.Log($"Auto Skills: {autoSkills.Count}");
            Debug.Log($"Total Auto Casts: {totalAutoCasts}");
            Debug.Log($"Check Interval: {checkInterval}s");
            
            Debug.Log("\n--- Auto-Cast Skills ---");
            foreach (var skill in autoSkills)
            {
                string status = skill.CanCast(skillController.CurrentMana) ? "✓ Ready" : "⏳ Waiting";
                Debug.Log($"  {skill} - {status}");
            }
            Debug.Log("═══════════════════════════════════════\n");
        }

        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[AutoCast] {message}");
            }
        }

        #endregion
    }
}
