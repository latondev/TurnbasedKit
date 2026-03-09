using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameSystems.Common;

namespace GameSystems.Skills
{
    /// <summary>
    /// Skill collection with query methods - uses ItemCollection base
    /// </summary>
    [Serializable]
    public class SkillIteratorData : ItemCollection<SkillData>
    {
        /// <summary>
        /// Gets next unlocked skill
        /// </summary>
        public SkillData NextUnlocked()
        {
            if (IsEmpty) return null;

            int startIndex = CurrentIndex;
            int loopCount = 0;
            int maxLoops = Items.Count;

            while (loopCount < maxLoops)
            {
                if (MoveNext())
                {
                    SkillData skill = Current;
                    if (skill != null && skill.IsUnlocked)
                        return skill;

                    if (CurrentIndex == startIndex)
                        break;
                }
                loopCount++;
            }

            return null;
        }

        /// <summary>
        /// Gets next skill ready to cast
        /// </summary>
        public SkillData NextReady(int currentMana)
        {
            if (IsEmpty) return null;

            int startIndex = CurrentIndex;
            int loopCount = 0;
            int maxLoops = Items.Count;

            while (loopCount < maxLoops)
            {
                if (MoveNext())
                {
                    SkillData skill = Current;
                    if (skill != null && skill.CanCast(currentMana))
                        return skill;

                    if (CurrentIndex == startIndex)
                        break;
                }
                loopCount++;
            }

            return null;
        }

        /// <summary>
        /// Gets next skill of category
        /// </summary>
        public SkillData NextOfCategory(SkillCategory category)
        {
            if (IsEmpty) return null;

            int startIndex = CurrentIndex;
            int loopCount = 0;
            int maxLoops = Items.Count;

            while (loopCount < maxLoops)
            {
                if (MoveNext())
                {
                    SkillData skill = Current;
                    if (skill != null && skill.Category == category)
                        return skill;

                    if (CurrentIndex == startIndex)
                        break;
                }
                loopCount++;
            }

            return null;
        }

        /// <summary>
        /// Gets skills by category
        /// </summary>
        public List<SkillData> GetSkillsByCategory(SkillCategory category)
        {
            return Items.Where(s => s.Category == category).ToList();
        }

        /// <summary>
        /// Gets skills by element
        /// </summary>
        public List<SkillData> GetSkillsByElement(SkillElement element)
        {
            return Items.Where(s => s.Element == element).ToList();
        }

        /// <summary>
        /// Gets all unlocked skills
        /// </summary>
        public List<SkillData> GetUnlockedSkills()
        {
            return Items.Where(s => s.IsUnlocked).ToList();
        }

        /// <summary>
        /// Gets all skills ready to cast
        /// </summary>
        public List<SkillData> GetReadySkills(int currentMana)
        {
            return Items.Where(s => s.CanCast(currentMana)).ToList();
        }

        /// <summary>
        /// Gets skills on cooldown
        /// </summary>
        public List<SkillData> GetSkillsOnCooldown()
        {
            return Items.Where(s => s.IsOnCooldown).ToList();
        }

        /// <summary>
        /// Sorts by level
        /// </summary>
        public void SortByLevel()
        {
            Items.Sort((a, b) => b.CurrentLevel.CompareTo(a.CurrentLevel));
            Initialize();
        }

        /// <summary>
        /// Sorts by damage
        /// </summary>
        public void SortByDamage()
        {
            Items.Sort((a, b) => b.GetTotalDamage().CompareTo(a.GetTotalDamage()));
            Initialize();
        }

        /// <summary>
        /// Sorts by cooldown
        /// </summary>
        public void SortByCooldown()
        {
            Items.Sort((a, b) => a.BaseCooldown.CompareTo(b.BaseCooldown));
            Initialize();
        }
    }
}
