using System.Collections;
using System.Collections.Generic;
using System.Linq;
// using MythydRpg.Remote;
using UnityEngine;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using TapDBMiniJSON;

namespace MythydRpg
{
    public class PvpCombat : BaseCombat
    {
        [SerializeField] private CombatDataRemote data;

        public override void StartCombat()
        {
            string url = ServerGateWay.GetApi().urlPvp;

            var userData = new Dictionary<string, object>();

            userData.Add("userAtk", "6748654215ff11c65e9227c3");
            userData.Add("userDef", "675afc802d872adbe96ced49");

            // Chuyển đổi sang JSON
            string dataSend = JsonHelper.Serialize(userData);
            Debug.Log("data =" + dataSend);

            ServerGateWay.PostAPI(url, dataSend, OnRespon);
        }

        private void OnRespon(string json)
        {
            targets = new List<CharaterTurnbase>();

            data = JsonConvert.DeserializeObject<CombatDataRemote>(json);
            Initialized();

            this.Wait(3f/speed, () => { StartCoroutine(Combat()); });
        }

        [Button]
        void ResetA()
        {
            data = null;
        }

        protected override void Initialized()
        {
            var lineUpAtk = data.teamAtk;
            for (int i = 0; i < lineUpAtk.Count; i++)
            {
                var item = lineUpAtk[i];
                if (item.id != 0)
                {
                    var data = CharacterManager.Instance.GetDataHero(item.id);
                    Debug.Log("logg =" + data.id);
                    _teamAtk[i].Initialized(data);
                }
            }

            var lineUpTeamDef = data.teamDef;


            for (int i = 0; i < lineUpTeamDef.Count; i++)
            {
                var item = lineUpTeamDef[i];
                if (item.id != 0)
                {
                    var data = CharacterManager.Instance.GetDataHero(item.id);
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
            combatResult = data.combatResult;
            return combatResult;
        }
    }
}