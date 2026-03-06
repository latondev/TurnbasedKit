using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameSystems.Common;

namespace GameSystems.Skills
{
    /// <summary>
    /// Base iterator data for Skills - extends GenericIteratorData
    /// </summary>
    [Serializable]
    public class SkillIteratorData : GenericIteratorData<SkillData>
    {
        /// <summary>
        /// Gets next unlocked skill
        /// </summary>
        public SkillData NextUnlocked()
        {
            if (CurrentIterator == null) return default;

            int startIndex = CurrentIterator.CurrentIndex;

            while (HasNext())
            {
                SkillData skill = Next();
                if (skill != null && skill.IsUnlocked)
                    return skill;

                if (CurrentIterator.CurrentIndex == startIndex)
                    break;
            }

            return default;
        }

        /// <summary>
        /// Gets next skill ready to cast
        /// </summary>
        public SkillData NextReady(int currentMana)
        {
            if (CurrentIterator == null) return default;

            int startIndex = CurrentIterator.CurrentIndex;

            while (HasNext())
            {
                SkillData skill = Next();
                if (skill != null && skill.CanCast(currentMana))
                    return skill;

                if (CurrentIterator.CurrentIndex == startIndex)
                    break;
            }

            return default;
        }

        /// <summary>
        /// Gets next skill of category
        /// </summary>
        public SkillData NextOfCategory(SkillCategory category)
        {
            if (CurrentIterator == null) return default;

            int startIndex = CurrentIterator.CurrentIndex;

            while (HasNext())
            {
                SkillData skill = Next();
                if (skill != null && skill.Category == category)
                    return skill;

                if (CurrentIterator.CurrentIndex == startIndex)
                    break;
            }

            return default;
        }

        /// <summary>
        /// Gets skills by category
        /// </summary>
        public List<SkillData> GetSkillsByCategory(SkillCategory category)
        {
            return Collection.Where(s => s.Category == category).ToList();
        }

        /// <summary>
        /// Gets skills by element
        /// </summary>
        public List<SkillData> GetSkillsByElement(SkillElement element)
        {
            return Collection.Where(s => s.Element == element).ToList();
        }

        /// <summary>
        /// Gets all unlocked skills
        /// </summary>
        public List<SkillData> GetUnlockedSkills()
        {
            return Collection.Where(s => s.IsUnlocked).ToList();
        }

        /// <summary>
        /// Gets all skills ready to cast
        /// </summary>
        public List<SkillData> GetReadySkills(int currentMana)
        {
            return Collection.Where(s => s.CanCast(currentMana)).ToList();
        }

        /// <summary>
        /// Gets skills on cooldown
        /// </summary>
        public List<SkillData> GetSkillsOnCooldown()
        {
            return Collection.Where(s => s.IsOnCooldown).ToList();
        }

        /// <summary>
        /// Sorts by level
        /// </summary>
        public void SortByLevel()
        {
            Collection.Sort((a, b) => b.CurrentLevel.CompareTo(a.CurrentLevel));
            Initialize();
        }

        /// <summary>
        /// Sorts by damage
        /// </summary>
        public void SortByDamage()
        {
            Collection.Sort((a, b) => b.GetTotalDamage().CompareTo(a.GetTotalDamage()));
            Initialize();
        }

        /// <summary>
        /// Sorts by cooldown
        /// </summary>
        public void SortByCooldown()
        {
            Collection.Sort((a, b) => a.BaseCooldown.CompareTo(b.BaseCooldown));
            Initialize();
        }
    }
}
