using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace MythydRpg
{
    public class PveCombat : BaseCombat
    {
        [SerializeField] protected CombatHelper _combatHelper;

        public override void StartCombat()
        {
            Initialized();
            targets = new List<CharaterTurnbase>();
            this.Wait(3f/speed, () => { StartCoroutine(Combat()); });
        }

        protected override void Initialized()
        {
            var lineUp = FormationManager.Instance.GetFormation(LineUpMode.PVE);
            for (int i = 0; i < lineUp.Count; i++)
            {
                var item = lineUp[i];
                if (item != 0)
                {
                    var data = CharacterManager.Instance.GetDataHero(item);
                    Debug.Log("logg =" + data.id);
                    _teamAtk[i].Initialized(data);
                }
            }

            var lineUpStage = new List<int> { 1, 1, 1, 1, 1, 1 };

            for (int i = 0; i < lineUpStage.Count; i++)
            {
                var item = lineUpStage[i];
                if (item != 0)
                {
                    var data = CharacterManager.Instance.GetDataEnemy(item);
                    Debug.Log("logg =" + data.id);
                    _teamDef[i].Initialized(data);
                }
            }


            totalHpTeamAtk = 0;
            totalDefTeamAtk = 0;

            foreach (var item in _teamAtk)
            {
                item.OnEndAnimate += ResetFadeScreen;
                totalHpTeamAtk += (long)item.HealCtr.Health;
            }

            foreach (var item in _teamDef)
            {
                item.OnEndAnimate += ResetFadeScreen;
                totalDefTeamAtk += (long)item.HealCtr.Health;
            }
        }

        protected override void CombatEnd()
        {
        }

        protected override CombatResult GetCombatResult()
        {
            return _combatHelper.GetCombatResult(_teamAtk, _teamDef);
        }
    }
}