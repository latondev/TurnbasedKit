using PathologicalGames;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace MythydRpg
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

    [RequireComponent(typeof(CharacterTurnbaseData))]
    [RequireComponent(typeof(AbilityController))]
    [RequireComponent(typeof(StatusController))]
    [RequireComponent(typeof(HealthController))]
    [RequireComponent(typeof(CharacterInventoryData))]
    public class CharaterTurnbase : MonoBehaviour
    {
        public Action OnEndAnimate;
        [SerializeField] private int _id;

        [FormerlySerializedAs("_team")] [SerializeField]
        private TeamType teamType;

        [SerializeField] private PosType posType;

        [SerializeField] CharacterTurnbaseData _characterData;
        [SerializeField] AbilityController _abilityController;
        [SerializeField] StatusController _statusController;
        [SerializeField] HealthController _healthController;
        [SerializeField] CharacterInventoryData _characterInventoryData;
        [SerializeField] CharacterView _characterView;

        [SerializeField] List<CharaterTurnbase> _targets;
        [SerializeField] CharacterDataSO _characterDataSO;

        public bool endAction;
        [SerializeField] bool isLoadData = false;

        [SerializeField] private List<HitInfo> hitInfos;


        public bool IsDie => _healthController.IsDead;

        public StatData Stat => _characterData.Stat;
        public float Speed => _characterData.Stat.speed;
        public TeamType TeamType => teamType;
        public int IdLocation => _id;

        public HealthController HealCtr => _healthController;

        public CharacterDataSO CharacterDataSO => _characterDataSO;

        public SkillConfig SkillBasic => _abilityController.SkillBasic;
        public SkillConfig SkillUltimate => _abilityController.SkillUltimate;

        public SkillConfig SkillPassive => _abilityController.SkillPassive;

        public int Level => _characterDataSO.level;
        public bool IsActive => isLoadData;


        private void OnValidate()
        {
            TryGetComponent(out _characterData);
            TryGetComponent(out _abilityController);
            TryGetComponent(out _statusController);
            TryGetComponent(out _healthController);
            TryGetComponent(out _characterInventoryData);
            // transform.TryGetComponentInChildren(out _characterView);
            _id = transform.GetSiblingIndex() + 1;
        }

        public void Initialized(CharacterDataSO data)
        {
            if (data == null) return;
            hitInfos = new List<HitInfo>();
            _targets = new List<CharaterTurnbase>();
            isLoadData = true;
            _characterDataSO = data;
            _characterData.InitData(_characterDataSO);
            _characterView = PoolManager.Pools[PoolName.poolCharacter].Spawn(_characterDataSO.prefabCharacterView.gameObject, transform).GetComponent<CharacterView>();
            _characterView.transform.localPosition = Vector3.zero;
            _characterView.SetFlipY(teamType);
            _healthController.OnDie += OnDie;
            InitComponet();
        }

        void Start()
        {
        }

        void InitComponet()
        {
            _characterView.OnStepAtk += OnStepAction;
            _characterView.OnEndAtk += OnEndAction;
            _healthController.Init(_characterData.Stat.hp, _characterData.Stat.mp);
            _abilityController.Init(_characterDataSO);
            _characterView.Init(_healthController.MaxHealth, _healthController.MaxMana);
        }

        private void OnDie()
        {
            _characterView.Die();
        }

        private int tempCountHit = 0;

        private void OnStepAction(int obj, bool isHitEffect)
        {
            tempCountHit++;
            if (tempCountHit > obj) return;

            foreach (var character in _targets)
            {
                Debug.Log("IdLocation =>> " + character.IdLocation);
                HitInfo hit = GetHitInfo(character.IdLocation);
                int dmgFinal = (int)hit.value / obj;
                character.Behit(dmgFinal, isHitEffect);
            }

            Debug.Log("OnStepAction " + obj);
        }

        HitInfo GetHitInfo(int id)
        {
            return hitInfos.Find(x => x.idLocationTarget == id);
        }

        public void Behit(int dmg, bool isHitEffect = false)
        {
            _healthController.AddMana(15);
            _characterView.AddMana(15);
            _healthController.ChangeHealth(-dmg);
            _characterView.Behit(dmg, isHitEffect);
        }

        private void OnEndAction()
        {
            tempCountHit = 0;
            _healthController.AddMana(25);
            _characterView.AddMana(25);
            Debug.Log("OnEndAction " + transform.name);
            endAction = true;
            OnEndAnimate?.Invoke();

            // if (_healthController.IsDead)
            // {
            //     OnDie();
            // }
        }

        public void Attack(List<HitInfo> hits, List<CharaterTurnbase> targets)
        {
            if (targets == null || targets.Count <= 0)
            {
                if (targets == null) Debug.Log($"<b><color=red>_Log </color></b> : targets == NULL");
                if (targets.Count <= 0) Debug.Log($"<b><color=red>_Log </color></b> : targets <= 0");


                //  endAction = true;
                return;
            }

            _targets.Clear();
            hitInfos.Clear();
            hitInfos.AddRange(hits);
            _targets.AddRange(targets);
            endAction = false;
            Debug.Log("count = " + targets.Count + " -- " + _characterView.name);


            _characterView.Attack(_targets[0].transform.position);
            // _characterView.Attack(_targets.transform.position);
        }

        public void Skill(List<HitInfo> hits, List<CharaterTurnbase> targets, Vector3 posSkill)
        {
            if (targets == null || targets.Count <= 0)
            {
                if (targets == null) Debug.Log($"<b><color=red>_Log </color></b> : targets == NULL");
                if (targets.Count <= 0) Debug.Log($"<b><color=red>_Log </color></b> : targets <= 0");


                //  endAction = true;
                return;
            }

            _targets.Clear();
            hitInfos.Clear();
            hitInfos.AddRange(hits);
            _targets.AddRange(targets);
            endAction = false;
            Debug.Log("count = " + targets.Count + " -- " + _characterView.name);
            List<Vector3> listPos = targets.FindAll(x => x != null).Select(x => x.transform.position).ToList();

            _characterView.Skill(listPos, posSkill);
            _healthController.ResetMana();
        }

        public void ResetView()
        {
            _characterView.ResetView();
        }

        public void ActiveFadeView()
        {
            _characterView.ActiveFadeView();
        }

        public bool CanSkillUltimate()
        {
            return _healthController.CanSkill() && _abilityController.CanUseSkillUltimate();
        }

        public bool IsSkillUltimate()
        {
            return _abilityController.CanUseSkillUltimate();
        }

        public bool IsSkillPassive()
        {
            return _abilityController.CanUseSkillPassive();
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
            _characterView.ChangeSpeed(speed);
        }
    }
}