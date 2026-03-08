using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using UnityEngine;

namespace MythydRpg
{
    public class CombatHelper : MonoBehaviour
    {
        [SerializeField] CombatResult _combatCurrentResult;

        void Start()
        {
        }

        public CombatResult GetCombatResult(List<CharaterTurnbase> ally, List<CharaterTurnbase> enemy)
        {
            _combatCurrentResult.combatDataResults.Clear();
            _combatCurrentResult.combatDataResults = GetCombatDataResult(ally, enemy);
            return _combatCurrentResult;
        }

        List<CharaterTurnbase> GetCharactersLive(List<CharaterTurnbase> characters)
        {
            return characters.Where(ch => !ch.IsDie && ch.IsActive).ToList();
        }


        List<CombatDataResult> GetCombatDataResult(List<CharaterTurnbase> ally, List<CharaterTurnbase> enemy)
        {
            List<CombatDataResult> combatDataResults = new List<CombatDataResult>();
            List<CharaterTurnbase> allyLive = GetCharactersLive(ally);
            List<CharaterTurnbase> enemyLive = GetCharactersLive(enemy);
            Debug.Log(" ally =" + allyLive.Count + " --- enemy " + enemyLive.Count);

            List<CharaterTurnbase> characters = GetCharactersBySpeed(allyLive, enemyLive);
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


        List<CharaterTurnbase> GetCharactersBySpeed(List<CharaterTurnbase> ally, List<CharaterTurnbase> enemy)
        {
            return ally
                .Concat(enemy) // Gộp cả ally và enemy
                .OrderByDescending(ch => ch.Speed) // Sắp xếp theo tốc độ giảm dần
                .ToList(); // Trả về danh sách
        }

        List<HitDataResult> GetHitDataResult(CharaterTurnbase main, List<CharaterTurnbase> ally, List<CharaterTurnbase> enemy)
        {
            List<HitDataResult> hitDataResults = new List<HitDataResult>();
            var targetTeam = main.TeamType == TeamType.ATK ? enemy : ally;

            var validTargets = targetTeam.Where(t => !t.IsDie && t.IsActive).ToList();

            if (validTargets.Count == 0)
            {
                Debug.LogWarning($"No valid targets for {main.TeamType}");
                return hitDataResults;
            }

            //CharaterTurnbase randomTarget = validTargets.GetRandomItem();


            bool isSkill = main.CanSkillUltimate();
            AttackType attackType = isSkill ? AttackType.SKILL : AttackType.NORMAL;
            if (isSkill)
            {
                Debug.Log($" value => <b><color=blue> atk type = {attackType}</color></b>  - >iskill = {isSkill}");
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

        void HandleAttackNormal(List<HitDataResult> hitDataResults, AttackType attackType, CharaterTurnbase main, List<CharaterTurnbase> validTargets)
        {
            int countTarget = main.SkillBasic.numberTarget;
            List<CharaterTurnbase> randomTargets = validTargets.GetRandomItems(countTarget);
            HitDataResult hitData = new HitDataResult();
            hitData.hitInfos = new List<HitInfo>();
            hitData.attackType = attackType;
            float dmgBonus = main.SkillBasic.bonusDmg.percentBonusDamage * main.Stat.atk;

            foreach (var item in randomTargets)
            {
                HitInfo hit = new HitInfo
                {
                    idTeamTarget = item.TeamType,
                    idLocationTarget = item.IdLocation,
                    dmgType = main.SkillBasic.bonusDmg.dmgType,
                    value = GetDmg(main, item,dmgBonus),
                    typeHit = TypeHit.Enemy
                };
                hitData.hitInfos.Add(hit);
            }

            hitDataResults.Add(hitData);
        }

        void HandleAttackSkill(List<HitDataResult> hitDataResults, AttackType attackType, CharaterTurnbase main, List<CharaterTurnbase> validTargets)
        {
            
            int countTarget = main.SkillUltimate.numberTarget;
            List<CharaterTurnbase> randomTargets = validTargets.GetRandomItems(countTarget);
            HitDataResult hitData = new HitDataResult();
            hitData.hitInfos = new List<HitInfo>();
            hitData.attackType = attackType;
            float dmgBonus = main.SkillUltimate.bonusDmg.percentBonusDamage * main.Stat.atk;

            foreach (var item in randomTargets)
            {
                HitInfo hit = new HitInfo
                {
                    idTeamTarget = item.TeamType,
                    idLocationTarget = item.IdLocation,
                    dmgType = main.SkillUltimate.bonusDmg.dmgType,
                    value = GetDmg(main, item,dmgBonus),
                    typeHit = TypeHit.Enemy
                };
                hitData.hitInfos.Add(hit);
            }

            hitDataResults.Add(hitData);
        }

        void HandleAttackBuff(List<HitDataResult> hitDataResults, AttackType attackType, CharaterTurnbase main, List<CharaterTurnbase> validTargets)
        {
        }

        void HandleAttackDebuff(List<HitDataResult> hitDataResults, AttackType attackType, CharaterTurnbase main, List<CharaterTurnbase> validTargets)
        {
        }


        float GetDmg(CharaterTurnbase main, CharaterTurnbase target,float dmgBonus =0)
        {
            return Mathf.Abs(target.Stat.pdef - (main.Stat.atk+dmgBonus)) ;
        }
    }
}