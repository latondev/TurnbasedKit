using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// Character Turnbase Data - stores stats for battle
    /// </summary>
    public class CharacterTurnbaseData : MonoBehaviour
    {
        [SerializeField] public bool _hasData;
        [SerializeField] public StatData _statBase;
        [SerializeField] public StatData _statData;

        public StatData Stat => _statData;

        private void Awake()
        {
            _statBase = new StatData();
            _statData = new StatData();
        }

        public void InitData(CharacterDataSO characterDataSO)
        {
            if (characterDataSO == null) return;

            _statBase.id = characterDataSO.id;
            _statBase.hp = characterDataSO.stats.hp;
            _statBase.mp = characterDataSO.stats.mp;
            _statBase.atk = characterDataSO.stats.atk;
            _statBase.pdef = characterDataSO.stats.pdef;
            _statBase.mdef = characterDataSO.stats.mdef;
            _statBase.speed = characterDataSO.stats.speed;
            _statBase.acc = characterDataSO.stats.acc;
            _statBase.eva = characterDataSO.stats.eva;

            _statData = new StatData
            {
                id = _statBase.id,
                hp = _statBase.hp,
                mp = _statBase.mp,
                atk = _statBase.atk,
                pdef = _statBase.pdef,
                mdef = _statBase.mdef,
                speed = _statBase.speed,
                acc = _statBase.acc,
                eva = _statBase.eva
            };

            _hasData = true;
        }
    }
}
