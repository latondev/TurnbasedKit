using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MythydRpg
{
    public class GameCombat : MonoBehaviour
    {
        [SerializeField] private PveCombat _pve;
        [SerializeField] PvpCombat _pvp;
        [SerializeField] private GamePlayScreen combatPanel;

        [SerializeField] BaseCombat combatExcute;

        void Start()
        {
            combatPanel.OnSpeedChange += SetSpeed;
        }

        void SetSpeed(float speed)
        {
            combatExcute.ChangeSpeed(speed);
        }

        public void PlayPvpCombat()
        {
            _pvp.StartCombat();
            combatExcute = _pvp;
            combatPanel.Show();
        }

        public void PlayPveCombat()
        {
            _pve.StartCombat();
            combatExcute = _pve;
            combatPanel.Show();
        }
    }
}