using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MythydRpg
{
    // [System.Serializable]
    // public class CombatResult
    // {
    //     public List<CombatDataResult> combatDataResults;
    // }
    [System.Serializable]
    public class CombatDataRemote
    {
        public List<TeamInfo> teamDef;
        public List<TeamInfo> teamAtk;
        public CombatResult combatResult;
        public string result;
        public int dmgAtk;
        public int dmgDef;
    }
    [System.Serializable]
    public class TeamInfo
    {
        public int id;
        public string name;
        public int hp;
    }
}