using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// Base Combat - abstract base class for PVE/PVP combat
    /// </summary>
    public abstract class BaseCombat : MonoBehaviour
    {
        [Header("Teams")]
        [SerializeField] protected List<CharacterTurnbase> _teamAtk = new List<CharacterTurnbase>();
        [SerializeField] protected List<CharacterTurnbase> _teamDef = new List<CharacterTurnbase>();

        [Header("Combat Settings")]
        [SerializeField] protected Transform posSkill;
        [SerializeField] protected CharacterTurnbase character;
        [SerializeField] protected float speed = 1f;

        [Header("Runtime")]
        [SerializeField] protected long totalHpTeamAtk = 0;
        [SerializeField] protected long totalDefTeamAtk = 0;

        protected List<CharacterTurnbase> targets = new List<CharacterTurnbase>();
        protected CombatResult combatResult;
        protected bool isTeamAtkLose;
        protected bool isTeamDefLose;

        // Events
        public event Action OnBattleStarted;
        public event Action<BattleOutcome> OnBattleEnded;

        // Properties
        public List<CharacterTurnbase> TeamAtk => _teamAtk;
        public List<CharacterTurnbase> TeamDef => _teamDef;
        public bool IsBattleOver => CheckCombatOver();

        public abstract void StartCombat();
        protected abstract void Initialized();
        protected abstract void CombatEnd();

        public void StopCombat()
        {
            StopCoroutine(Combat());
        }

        public void ChangeSpeed(float speed)
        {
            this.speed = Mathf.Clamp(speed, 0.5f, 5f);
            foreach (var item in _teamAtk)
            {
                item.ChangeSpeed(this.speed);
            }

            foreach (var item in _teamDef)
            {
                item.ChangeSpeed(this.speed);
            }
        }

        protected IEnumerator Combat()
        {
            Debug.Log("Start Combat");

            // Check if battle should end immediately
            if (HandleResult())
            {
                yield break;
            }

            var combatResult = GetCombatResult();

            foreach (var combatInfo in combatResult.combatDataResults)
            {
                character = GetCharacterById(combatInfo.idLocationSelf, combatInfo.idTeamSelf);

                if (character == null || character.IsDie)
                {
                    continue;
                }

                foreach (var hit in combatInfo.hitDataResults)
                {
                    targets.Clear();
                    Debug.Log($"Hit count: {hit.hitInfos.Count}");

                    foreach (var info in hit.hitInfos)
                    {
                        Debug.Log($"Attack: {character.IdLocation} (Team: {character.TeamType}) -> Target: {info.idLocationTarget} (Team: {info.idTeamTarget})");

                        CharacterTurnbase target = GetCharacterById(info.idLocationTarget, info.idTeamTarget);

                        if (target != null)
                        {
                            targets.Add(target);
                        }
                    }

                    // Create temp target list
                    var tempTarget = new List<CharacterTurnbase>();
                    tempTarget.AddRange(targets);

                    // Handle attack
                    HandleAttack(hit, character, tempTarget);

                    // Wait for animation to finish
                    yield return new WaitUntil(() => character.endAction);

                    if (HandleResult())
                    {
                        yield break;
                    }
                }
            }

            // Loop combat
            StartCoroutine(Combat());
        }

        protected bool HandleResult()
        {
            bool endCombat = CheckCombatOver();
            if (endCombat)
            {
                Debug.Log("Combat ended");

                if (isTeamAtkLose)
                {
                    Debug.Log("Team ATK lost");
                    foreach (var item in _teamDef)
                    {
                        item.ShowWin();
                    }
                    OnBattleEnded?.Invoke(BattleOutcome.Defeat);
                }
                else if (isTeamDefLose)
                {
                    Debug.Log("Team DEF lost");
                    foreach (var item in _teamAtk)
                    {
                        item.ShowWin();
                    }
                    OnBattleEnded?.Invoke(BattleOutcome.Victory);
                }

                CombatEnd();
                return true;
            }

            return false;
        }

        protected void ResetFadeScreen()
        {
            // Reset character view
            character?.ResetView();
            foreach (var item in targets)
            {
                item?.ResetView();
            }
        }

        protected void HandleAttack(HitDataResult hit, CharacterTurnbase attacker, List<CharacterTurnbase> targets)
        {
            switch (hit.attackType)
            {
                case AttackType.NORMAL:
                    Debug.Log($"Normal Attack: {attacker.CharacterDataSO?.nameHero}");
                    attacker.Attack(hit.hitInfos, targets);
                    break;
                case AttackType.SKILL:
                    attacker.ActiveFadeView();
                    foreach (var item in targets)
                    {
                        item.ActiveFadeView();
                    }

                    Vector3 posTemp = posSkill != null ? posSkill.position : targets[0].transform.position;
                    attacker.Skill(hit.hitInfos, targets, posTemp);
                    break;
                case AttackType.BUFF:
                    break;
                case AttackType.DEBUFF:
                    break;
            }
        }

        protected bool CheckCombatOver()
        {
            isTeamAtkLose = _teamAtk.TrueForAll(c => c.IsDie);
            isTeamDefLose = _teamDef.TrueForAll(c => c.IsDie);
            bool isCombatOver = isTeamAtkLose || isTeamDefLose;
            return isCombatOver;
        }

        protected CharacterTurnbase GetCharacterById(int id, TeamType teamType)
        {
            List<CharacterTurnbase> team = teamType == TeamType.ATK ? _teamAtk : _teamDef;

            if (team == null || team.Count == 0)
            {
                return null;
            }

            CharacterTurnbase character = team
                .Where(c => c != null)
                .FirstOrDefault(c =>
                    c.IdLocation == id &&
                    !c.IsDie &&
                    c.IsActive);

            return character;
        }

        protected abstract CombatResult GetCombatResult();
    }

    public enum BattleOutcome
    {
        Victory,
        Defeat,
        Draw
    }
}
