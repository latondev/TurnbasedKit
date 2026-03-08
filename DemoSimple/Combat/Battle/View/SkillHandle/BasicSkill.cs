using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MythydRpg;

namespace MythydRpg
{
    public class BasicSkill : SkillHandle
    {
        [SerializeField] private ParticleSystem fxSkill;
        public override void Excute(float speed,Action callback= null)
        {
            fxSkill.SpeedUpFx(speed);
            fxSkill.Play();
        }
    }
}
