using System;
using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// Skill Handle - handles skill effects.
    /// </summary>
    public abstract class SkillHandle : MonoBehaviour
    {
        public abstract void Excute(float speed, Action callback = null);
    }
}
