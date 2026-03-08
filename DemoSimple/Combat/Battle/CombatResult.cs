using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine.Serialization;

namespace MythydRpg
{
    public enum AttackType
    {
        NORMAL,
        SKILL,
        BUFF,
        DEBUFF
    }

    [System.Serializable]
    public class CombatResult
    {
        public List<CombatDataResult> combatDataResults;
    }

    [System.Serializable]
    public class CombatDataResult
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public TeamType idTeamSelf;

        public int idLocationSelf;
        public List<HitDataResult> hitDataResults;
    }

    [Serializable]
    public class HitDataResult
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public AttackType attackType;

        public int mana;
        public List<HitInfo> hitInfos;
    }

    [Serializable]
    public class HitInfo
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public TeamType idTeamTarget;

        public int idLocationTarget;
        public float value;

        [JsonConverter(typeof(StringEnumConverter))]
        public DmgType dmgType;

        [JsonConverter(typeof(StringEnumConverter))]
        public TypeHit typeHit;

        public AbilityStatus abilityStatus;
    }

    [Serializable]
    public class AbilityStatus
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public BaseAbilityStatus type;

        public float percent;
        public int duration; // theo round . 1 round l� 1 duration
        public int stackable;

       [JsonConverter(typeof(StringEnumConverter))]
        public DamageType dmgType;
    }

    public enum DamageType
    {
        NORMAL, //atk
        TRUE, //atk
        MAGIC, //atk
        HEAL,
        SHIELD,
        BUFF,
        DEBUFF
    }

    public enum BaseAbilityStatus
    {
    }


    public enum TypeHit
    {
        Self,
        Ally,
        Enemy
    }
}