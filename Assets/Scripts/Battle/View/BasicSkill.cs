using System;
using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// Basic Skill - simple skill implementation.
    /// </summary>
    public class BasicSkill : SkillHandle
    {
        [SerializeField] private ParticleSystem fxSkill;

        public override void Excute(float speed, Action callback = null)
        {
            if (fxSkill != null)
            {
                fxSkill.Play();
            }

            callback?.Invoke();
        }
    }
}
