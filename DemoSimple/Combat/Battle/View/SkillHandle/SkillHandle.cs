using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MythydRpg
{
    public abstract class SkillHandle : MonoBehaviour
    {
        public abstract void Excute(float speed,Action callback = null);

    }
}
