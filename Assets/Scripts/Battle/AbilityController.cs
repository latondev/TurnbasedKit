using System;
using UnityEngine;
using GameSystems.Skills;

namespace GameSystems.Battle
{
    /// <summary>
    /// Ability Controller - manages skills
    /// </summary>
    public class AbilityController : MonoBehaviour
    {
        [SerializeField] private SkillConfig skillBasic;
        [SerializeField] private SkillConfig skillUltimate;
        [SerializeField] private SkillConfig skillPassive;

        public SkillConfig SkillBasic => skillBasic;
        public SkillConfig SkillPassive => skillPassive;
        public SkillConfig SkillUltimate => skillUltimate;

        void Start()
        {
        }

        public void Init(CharacterDataSO characterDataSO)
        {
            if (characterDataSO == null) return;

            skillBasic = CreateSkillConfig(characterDataSO.skillBasic);
            skillUltimate = CreateSkillConfig(characterDataSO.skillUltimate);
            skillPassive = CreateSkillConfig(characterDataSO.skillPassive);
        }

        private SkillConfig CreateSkillConfig(SkillData skillData)
        {
            if (skillData == null) return null;

            return new SkillConfig
            {
                numberTarget = skillData.MaxTargets,
                bonusDmg = new BonusDmg
                {
                    dmgType = DmgType.NORMAL,
                    percentBonusDamage = skillData.BaseDamage
                },
                percentBonusCrit = 0,
                percentBonusAccuracy = 0,
                location = LocationType.All,
                typeSkill = TypeSkill.Basic,
                actionTypeSkill = ActionTypeSkill.Attack,
                hitEffect = new System.Collections.Generic.List<HitEffect>()
            };
        }

        public bool CanUseSkillUltimate()
        {
            return skillUltimate != null && skillUltimate.numberTarget > 0;
        }

        public bool CanUseSkillPassive()
        {
            return skillPassive != null && skillPassive.numberTarget > 0;
        }
    }

    #region Skill Config Classes

    [System.Serializable]
    public class SkillConfig
    {
        public int numberTarget;
        public BonusDmg bonusDmg;
        public float percentBonusCrit;
        public float percentBonusAccuracy;
        public LocationType location;
        public TypeSkill typeSkill;
        public ActionTypeSkill actionTypeSkill;
        public System.Collections.Generic.List<HitEffect> hitEffect;
    }

    [System.Serializable]
    public class BonusDmg
    {
        public DmgType dmgType;
        public float percentBonusDamage;
    }

    public enum LocationType
    {
        All,
        Only,
        Row,
        Column,
        Font,
        Back
    }

    public enum TypeSkill
    {
        Basic,
        Ultimate,
        Passive,
        Awaken
    }

    public enum ActionTypeSkill
    {
        Attack,
        Debuff,
        Buff
    }

    [System.Serializable]
    public class HitEffect
    {
        public float percentChanceEffect = 1f;
        public EffectType effectType;
        public TypeUnit typeUnit;
        public PercentEffect percentEffect;
        public PercentEffect percentEffectLimit;
        public HealthState healthState;
    }

    [System.Serializable]
    public class PercentEffect
    {
        public float percentBonus;
        public TypeUnit typeUnit;
        public float percentdmgAffectedEffect;
        public int duration;
    }

    public enum EffectType
    {
        None,
        Heal,
        Quicksand,
        ReduceDmg,
        Burn,
    }

    public enum TypeUnit
    {
        Ally,
        Enemy,
        Self
    }

    public enum HealthState
    {
        None,
        Low,
        High
    }

    #endregion
}
