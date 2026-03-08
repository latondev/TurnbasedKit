using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// Combat Helper - calculates combat results
    /// </summary>
    public class CombatHelper : MonoBehaviour
    {
        [SerializeField] private CombatResult _combatCurrentResult;

        void Awake()
        {
            if (_combatCurrentResult == null)
            {
                _combatCurrentResult = new CombatResult();
            }
        }

        void Start()
        {
        }

        public CombatResult GetCombatResult(List<CharacterTurnbase> ally, List<CharacterTurnbase> enemy)
        {
            _combatCurrentResult.combatDataResults.Clear();
            _combatCurrentResult.combatDataResults = GetCombatDataResult(ally, enemy);
            return _combatCurrentResult;
        }

        List<CharacterTurnbase> GetCharactersLive(List<CharacterTurnbase> characters)
        {
            return characters.Where(ch => ch != null && !ch.IsDie && ch.IsActive).ToList();
        }

        List<CombatDataResult> GetCombatDataResult(List<CharacterTurnbase> ally, List<CharacterTurnbase> enemy)
        {
            List<CombatDataResult> combatDataResults = new List<CombatDataResult>();
            List<CharacterTurnbase> allyLive = GetCharactersLive(ally);
            List<CharacterTurnbase> enemyLive = GetCharactersLive(enemy);

            Debug.Log($"CombatHelper: ally = {allyLive.Count}, enemy = {enemyLive.Count}");

            if (allyLive.Count == 0 || enemyLive.Count == 0)
            {
                Debug.LogWarning("CombatHelper: No alive units in one team!");
                return combatDataResults;
            }

            List<CharacterTurnbase> characters = GetCharactersBySpeed(allyLive, enemyLive);
            foreach (var cha in characters)
            {
                CombatDataResult combatDataResult = new CombatDataResult();
                combatDataResult.idTeamSelf = cha.TeamType;
                combatDataResult.idLocationSelf = cha.IdLocation;

                combatDataResult.hitDataResults = GetHitDataResult(cha, allyLive, enemyLive);
                combatDataResults.Add(combatDataResult);
            }

            return combatDataResults;
        }

        List<CharacterTurnbase> GetCharactersBySpeed(List<CharacterTurnbase> ally, List<CharacterTurnbase> enemy)
        {
            return ally
                .Concat(enemy)
                .OrderByDescending(ch => ch.Speed)
                .ToList();
        }

        List<HitDataResult> GetHitDataResult(CharacterTurnbase main, List<CharacterTurnbase> ally, List<CharacterTurnbase> enemy)
        {
            List<HitDataResult> hitDataResults = new List<HitDataResult>();
            var targetTeam = main.TeamType == TeamType.ATK ? enemy : ally;

            var validTargets = targetTeam.Where(t => t != null && !t.IsDie && t.IsActive).ToList();

            if (validTargets.Count == 0)
            {
                Debug.LogWarning($"No valid targets for {main.CharacterDataSO?.nameHero}");
                return hitDataResults;
            }

            bool isSkill = main.CanSkillUltimate();
            AttackType attackType = isSkill ? AttackType.SKILL : AttackType.NORMAL;

            if (isSkill)
            {
                Debug.Log($"Using skill: {attackType}");
            }

            switch (attackType)
            {
                case AttackType.NORMAL:
                    HandleAttackNormal(hitDataResults, attackType, main, validTargets);
                    break;
                case AttackType.SKILL:
                    HandleAttackSkill(hitDataResults, attackType, main, validTargets);
                    break;
                case AttackType.BUFF:
                    HandleAttackBuff(hitDataResults, attackType, main, validTargets);
                    break;
                case AttackType.DEBUFF:
                    HandleAttackDebuff(hitDataResults, attackType, main, validTargets);
                    break;
            }

            return hitDataResults;
        }

        void HandleAttackNormal(List<HitDataResult> hitDataResults, AttackType attackType, CharacterTurnbase main, List<CharacterTurnbase> validTargets)
        {
            var skillBasic = main.SkillBasic;
            if (skillBasic == null)
            {
                Debug.LogWarning("No basic skill configured!");
                return;
            }

            int countTarget = Mathf.Min(skillBasic.numberTarget, validTargets.Count);
            List<CharacterTurnbase> randomTargets = GetRandomItems(validTargets, countTarget);

            HitDataResult hitData = new HitDataResult();
            hitData.hitInfos = new List<HitInfo>();
            hitData.attackType = attackType;
            hitData.mana = 0;

            float dmgBonus = 0;
            if (skillBasic.bonusDmg != null)
            {
                dmgBonus = skillBasic.bonusDmg.percentBonusDamage * (main.Stat != null ? main.Stat.atk : 0);
            }

            foreach (var item in randomTargets)
            {
                HitInfo hit = new HitInfo
                {
                    idTeamTarget = item.TeamType,
                    idLocationTarget = item.IdLocation,
                    dmgType = skillBasic.bonusDmg?.dmgType ?? DmgType.NORMAL,
                    value = GetDmg(main, item, dmgBonus),
                    typeHit = TypeHit.Enemy
                };
                hitData.hitInfos.Add(hit);
            }

            hitDataResults.Add(hitData);
        }

        void HandleAttackSkill(List<HitDataResult> hitDataResults, AttackType attackType, CharacterTurnbase main, List<CharacterTurnbase> validTargets)
        {
            var skillUltimate = main.SkillUltimate;
            if (skillUltimate == null)
            {
                Debug.LogWarning("No ultimate skill configured!");
                return;
            }

            int countTarget = Mathf.Min(skillUltimate.numberTarget, validTargets.Count);
            List<CharacterTurnbase> randomTargets = GetRandomItems(validTargets, countTarget);

            HitDataResult hitData = new HitDataResult();
            hitData.hitInfos = new List<HitInfo>();
            hitData.attackType = attackType;
            hitData.mana = 0;

            float dmgBonus = 0;
            if (skillUltimate.bonusDmg != null)
            {
                dmgBonus = skillUltimate.bonusDmg.percentBonusDamage * (main.Stat != null ? main.Stat.atk : 0);
            }

            foreach (var item in randomTargets)
            {
                HitInfo hit = new HitInfo
                {
                    idTeamTarget = item.TeamType,
                    idLocationTarget = item.IdLocation,
                    dmgType = skillUltimate.bonusDmg?.dmgType ?? DmgType.NORMAL,
                    value = GetDmg(main, item, dmgBonus),
                    typeHit = TypeHit.Enemy
                };
                hitData.hitInfos.Add(hit);
            }

            hitDataResults.Add(hitData);
        }

        void HandleAttackBuff(List<HitDataResult> hitDataResults, AttackType attackType, CharacterTurnbase main, List<CharacterTurnbase> validTargets)
        {
            // Buff logic - for now, skip
        }

        void HandleAttackDebuff(List<HitDataResult> hitDataResults, AttackType attackType, CharacterTurnbase main, List<CharacterTurnbase> validTargets)
        {
            // Debuff logic - for now, skip
        }

        float GetDmg(CharacterTurnbase main, CharacterTurnbase target, float dmgBonus = 0)
        {
            if (main == null || main.Stat == null || target == null || target.Stat == null)
            {
                return 0;
            }

            float atk = main.Stat.atk + dmgBonus;
            float def = target.Stat.pdef;

            // Simple damage formula
            float dmg = Mathf.Abs(def - atk);
            return Mathf.Max(1, dmg);
        }

        List<T> GetRandomItems<T>(List<T> list, int count)
        {
            if (list == null || list.Count == 0 || count <= 0)
                return new List<T>();

            int actualCount = Mathf.Min(count, list.Count);
            List<T> result = new List<T>();
            List<T> tempList = new List<T>(list);

            for (int i = 0; i < actualCount; i++)
            {
                if (tempList.Count == 0) break;
                int index = UnityEngine.Random.Range(0, tempList.Count);
                result.Add(tempList[index]);
                tempList.RemoveAt(index);
            }

            return result;
        }
    }

    #region Combat Result Classes

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
        public List<CombatDataResult> combatDataResults = new List<CombatDataResult>();
    }

    [System.Serializable]
    public class CombatDataResult
    {
        public TeamType idTeamSelf;
        public int idLocationSelf;
        public List<HitDataResult> hitDataResults = new List<HitDataResult>();
    }

    [System.Serializable]
    public class HitDataResult
    {
        public AttackType attackType;
        public int mana;
        public List<HitInfo> hitInfos = new List<HitInfo>();
    }

    #endregion
}
