using System;
using System.Collections.Generic;
using UnityEngine;
using GameSystems.Stats;

namespace GameSystems.Battle
{
    public enum TeamType
    {
        ATK,
        DEF
    }

    public enum PosType
    {
        FRONT,
        BACK
    }

    /// <summary>
    /// Character turnbase unit - represents a character in battle
    /// Uses HealthController with StatsSystem for stats management
    /// </summary>
    public class CharacterTurnbase : MonoBehaviour
    {
        [Header("Basic Info")]
        [SerializeField] private int _id;
        [SerializeField] private TeamType teamType;
        [SerializeField] private PosType posType;
        [SerializeField] private CharacterDataSO _characterDataSO;

        [Header("Components")]
        [SerializeField] private CharacterTurnbaseData _characterData;
        [SerializeField] private AbilityController _abilityController;
        [SerializeField] private StatusController _statusController;
        [SerializeField] private HealthController _healthController;
        [SerializeField] private CharacterView _characterView;

        [Header("Runtime")]
        [SerializeField] private List<CharacterTurnbase> _targets = new List<CharacterTurnbase>();
        [SerializeField] private List<HitInfo> hitInfos = new List<HitInfo>();
        [SerializeField] private bool isLoadData = false;
        public bool endAction = false;

        // Events
        public event Action OnEndAnimate;

        // Properties
        public bool IsDie => _healthController != null && _healthController.IsDead;
        public TeamType TeamType => teamType;
        public int IdLocation => _id;
        public int Level => _characterDataSO != null ? _characterDataSO.level : 1;
        public bool IsActive => isLoadData;
        public CharacterDataSO CharacterDataSO => _characterDataSO;

        public CharacterTurnbaseData CharacterData => _characterData;
        public HealthController HealCtr => _healthController;
        public CharacterView CharacterView => _characterView;

        public SkillConfig SkillBasic => _abilityController != null ? _abilityController.SkillBasic : null;
        public SkillConfig SkillUltimate => _abilityController != null ? _abilityController.SkillUltimate : null;
        public SkillConfig SkillPassive => _abilityController != null ? _abilityController.SkillPassive : null;

        public StatData Stat => _characterData != null ? _characterData.Stat : null;

        // Combat stats from StatsSystem via HealthController
        public float Speed => _healthController != null && _healthController.StatController != null
            ? _healthController.StatController.GetStatValue("speed")
            : 0;

        public float Atk => _healthController != null && _healthController.StatController != null
            ? _healthController.StatController.GetStatValue("attack")
            : 0;

        public float Def => _healthController != null && _healthController.StatController != null
            ? _healthController.StatController.GetStatValue("defense")
            : 0;

        private void OnValidate()
        {
            TryGetComponent(out _characterData);
            TryGetComponent(out _abilityController);
            TryGetComponent(out _statusController);
            TryGetComponent(out _healthController);
            _id = transform.GetSiblingIndex() + 1;
        }

        public void Initialized(CharacterDataSO data)
        {
            if (data == null)
            {
                Debug.LogWarning("CharacterTurnbase: data is null!");
                return;
            }

            hitInfos = new List<HitInfo>();
            _targets = new List<CharacterTurnbase>();
            isLoadData = true;
            _characterDataSO = data;

            if (_characterData != null)
            {
                _characterData.InitData(_characterDataSO);
            }

            if (_healthController != null && _characterData != null && _characterData.Stat != null)
            {
                // Use full stats initialization with combat stats
                _healthController.InitFull(
                    _characterData.Stat.hp,
                    _characterData.Stat.mp,
                    _characterData.Stat.atk,
                    _characterData.Stat.pdef,
                    _characterData.Stat.speed
                );
            }

            if (_abilityController != null)
            {
                _abilityController.Init(_characterDataSO);
            }

            if (_characterView != null && _healthController != null)
            {
                _characterView.Init(_healthController.MaxHealth, _healthController.MaxMana);
            }

            Debug.Log($"CharacterTurnbase initialized: {data.nameHero} (Team: {teamType}, ID: {_id})");
        }

        public void SetCharacterView(CharacterView view)
        {
            _characterView = view;
            if (_characterView != null && _healthController != null)
            {
                _characterView.Init(_healthController.MaxHealth, _healthController.MaxMana);
            }
        }

        public void SetTeamType(TeamType team)
        {
            teamType = team;
        }

        #region Combat Actions

        public void Attack(List<HitInfo> hits, List<CharacterTurnbase> targets)
        {
            if (targets == null || targets.Count <= 0)
            {
                Debug.LogWarning($"CharacterTurnbase.Attack: targets is null or empty!");
                return;
            }

            _targets.Clear();
            hitInfos.Clear();
            hitInfos.AddRange(hits);
            _targets.AddRange(targets);
            endAction = false;

            Debug.Log($"Attack: {targets.Count} targets");

            if (_characterView != null && _targets.Count > 0)
            {
                _characterView.Attack(_targets[0].transform.position);
            }
        }

        public void Skill(List<HitInfo> hits, List<CharacterTurnbase> targets, Vector3 posSkill)
        {
            if (targets == null || targets.Count <= 0)
            {
                Debug.LogWarning($"CharacterTurnbase.Skill: targets is null or empty!");
                return;
            }

            _targets.Clear();
            hitInfos.Clear();
            hitInfos.AddRange(hits);
            _targets.AddRange(targets);
            endAction = false;

            Debug.Log($"Skill: {targets.Count} targets");

            if (_characterView != null)
            {
                List<Vector3> listPos = new List<Vector3>();
                foreach (var target in targets)
                {
                    if (target != null)
                    {
                        listPos.Add(target.transform.position);
                    }
                }

                if (listPos.Count > 0)
                {
                    _characterView.Skill(listPos, posSkill);
                }
            }

            if (_healthController != null)
            {
                _healthController.ResetMana();
            }
        }

        private int tempCountHit = 0;

        public void OnStepAction(int obj, bool isHitEffect)
        {
            tempCountHit++;
            if (tempCountHit > obj) return;

            foreach (var character in _targets)
            {
                if (character == null) continue;

                Debug.Log($"OnStepAction: target {character.IdLocation}");
                HitInfo hit = GetHitInfo(character.IdLocation);
                int dmgFinal = hit != null ? (int)(hit.value / obj) : 0;
                character.Behit(dmgFinal, isHitEffect);
            }
        }

        HitInfo GetHitInfo(int id)
        {
            return hitInfos.Find(x => x.idLocationTarget == id);
        }

        public void Behit(int dmg, bool isHitEffect = false)
        {
            if (_healthController != null)
            {
                _healthController.AddMana(15);
            }

            if (_characterView != null)
            {
                _characterView.AddMana(15);
            }

            if (_healthController != null)
            {
                _healthController.ChangeHealth(-dmg);
            }

            if (_characterView != null)
            {
                _characterView.Behit(dmg, isHitEffect);
            }
        }

        private void OnEndAction()
        {
            tempCountHit = 0;

            if (_healthController != null)
            {
                _healthController.AddMana(25);
            }

            if (_characterView != null)
            {
                _characterView.AddMana(25);
            }

            Debug.Log($"OnEndAction: {transform.name}");
            endAction = true;
            OnEndAnimate?.Invoke();
        }

        public void ResetView()
        {
            if (_characterView != null)
            {
                _characterView.ResetView();
            }
        }

        public void ActiveFadeView()
        {
            if (_characterView != null)
            {
                _characterView.ActiveFadeView();
            }
        }

        public bool CanSkillUltimate()
        {
            return _healthController != null &&
                   _healthController.CanSkill() &&
                   _abilityController != null &&
                   _abilityController.CanUseSkillUltimate();
        }

        public bool IsSkillUltimate()
        {
            return _abilityController != null && _abilityController.CanUseSkillUltimate();
        }

        public bool IsSkillPassive()
        {
            return _abilityController != null && _abilityController.CanUseSkillPassive();
        }

        public void ShowWin()
        {
            if (_characterView != null)
            {
                _characterView.AnimateWin();
            }
        }

        public void ChangeSpeed(float speed)
        {
            if (_characterView != null)
            {
                _characterView.ChangeSpeed(speed);
            }
        }

        public void OnDie()
        {
            if (_characterView != null)
            {
                _characterView.Die();
            }
        }

        #region Buff/Debuff Methods (using StatsSystem)

        /// <summary>
        /// Apply attack buff to this unit
        /// </summary>
        public void ApplyAttackBuff(float percentageBonus, int durationTurns)
        {
            _healthController?.ApplyBuff("attack", percentageBonus, durationTurns);
        }

        /// <summary>
        /// Apply defense buff to this unit
        /// </summary>
        public void ApplyDefenseBuff(float percentageBonus, int durationTurns)
        {
            _healthController?.ApplyBuff("defense", percentageBonus, durationTurns);
        }

        /// <summary>
        /// Apply speed buff to this unit
        /// </summary>
        public void ApplySpeedBuff(float percentageBonus, int durationTurns)
        {
            _healthController?.ApplyBuff("speed", percentageBonus, durationTurns);
        }

        /// <summary>
        /// Apply attack debuff to this unit
        /// </summary>
        public void ApplyAttackDebuff(float percentagePenalty, int durationTurns)
        {
            _healthController?.ApplyDebuff("attack", percentagePenalty, durationTurns);
        }

        /// <summary>
        /// Apply defense debuff to this unit
        /// </summary>
        public void ApplyDefenseDebuff(float percentagePenalty, int durationTurns)
        {
            _healthController?.ApplyDebuff("defense", percentagePenalty, durationTurns);
        }

        /// <summary>
        /// Clear all buffs/debuffs from this unit
        /// </summary>
        public void ClearAllBuffs()
        {
            _healthController?.ClearAllBuffs();
        }

        #endregion

        #endregion
    }

    #region Data Classes

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
        public float acc = 1f;
        public float eva = 1f;

        public StatData() { }

        public StatData(int id, int hp, int mp, float atk, float def, float mdef, float speed, float acc, float eva)
        {
            this.id = id;
            this.hp = hp;
            this.mp = mp;
            this.atk = atk;
            this.pdef = def;
            this.mdef = mdef;
            this.speed = speed;
            this.acc = acc;
            this.eva = eva;
        }
    }

    [System.Serializable]
    public class HitInfo
    {
        public TeamType idTeamTarget;
        public int idLocationTarget;
        public float value;
        public DmgType dmgType;
        public TypeHit typeHit;
        public AbilityStatus abilityStatus;
    }

    [System.Serializable]
    public class AbilityStatus
    {
        public BaseAbilityStatus type;
        public float percent;
        public int duration;
        public int stackable;
        public DamageType dmgType;
    }

    public enum DmgType
    {
        NORMAL,
        Physical,
        Magic,
        TRUE
    }

    public enum TypeHit
    {
        Self,
        Ally,
        Enemy
    }

    public enum BaseAbilityStatus
    {
    }

    public enum DamageType
    {
        NORMAL,
        TRUE,
        MAGIC,
        HEAL,
        SHIELD,
        BUFF,
        DEBUFF
    }

    #endregion
}
