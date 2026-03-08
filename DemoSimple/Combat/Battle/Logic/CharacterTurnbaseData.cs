using System;
using System.Collections;
using System.Collections.Generic;
using Kryz.CharacterStats;
using UnityEngine;

namespace MythydRpg
{
    public class CharacterTurnbaseData : MonoBehaviour
    {
        [SerializeField] public bool _hasData;
        [SerializeField] public StatData _statBase;

        [SerializeField] public StatData _statData;
        public StatData Stat => _statData;

        public void InitData(CharacterDataSO characterDataSO)
        {
            _statBase.id = characterDataSO.id;
            _statBase.hp = characterDataSO.data.hp;
            _statBase.mp = characterDataSO.data.mp;
            _statBase.atk = characterDataSO.data.atk;
            _statBase.pdef = characterDataSO.data.pdef;
            _statBase.mdef = characterDataSO.data.mdef;
            _statBase.speed = characterDataSO.data.speed;
            _statBase.acc = characterDataSO.data.acc;
            _statBase.eva = characterDataSO.data.eva;


            _statData = _statBase;
        }
    }

    [System.Serializable]
    public class StatData
    {
        public int id;
        public int hp;
        public int mp;
        public float atk;
        public float pdef;
        public float mdef;

        public float speed;
        public float acc;
        public float eva;


        public StatData(int id, int hp, int mp, float atk, float def, float speed, float acc, float eva)
        {
            this.id = id;
            this.hp = hp;
            this.mp = mp;
            this.atk = atk;
            this.pdef = def;
            this.speed = speed;
            this.acc = acc;
            this.eva = eva;
        }
    }
}