using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MythydRpg
{
    public class AbilityController : MonoBehaviour
    {
        [SerializeField] SkillConfig skillBasic;
        [SerializeField] SkillConfig skillUltimate;
        [SerializeField] SkillConfig skillPassive;
        public SkillConfig SkillBasic => skillBasic;
        public SkillConfig SkillPassive => skillPassive;
        public SkillConfig SkillUltimate => skillUltimate;


        void Start()
        {

        }

        public void Init(CharacterDataSO characterDataSO)
        {
           
            skillBasic = characterDataSO.skillBasic.skillConfig;
            skillUltimate = characterDataSO.skillUltimate.skillConfig;
            skillPassive = characterDataSO.skillPassive.skillConfig;
        }

        public bool CanUseSkillUltimate()
        {
            return skillUltimate.numberTarget > 0;
        }

        public bool CanUseSkillPassive()
        {
            return skillPassive.numberTarget > 0;

        }




    }
}
