using PathologicalGames;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MythydRpg
{
    public class FormationPanel : ViewBase
    {
        public Action OnBack;

        public Action<List<int>> OnChangeFormation;
        [SerializeField] RectTransform root;
        [SerializeField] Button _btnBack;
        [SerializeField] List<IndexSlot> _slots;
        [SerializeField] List<HeroLineup> _heros;

        List<int> formation;//= new List<int>();
        public void ShowPanel(List<int> lineUps, List<CharacterDataSO> ownerSO)
        {
            Show();
            for (int i = 0; i < lineUps.Count; i++)
            {
                if (lineUps[i] != 0)
                {
                    int idHero = lineUps[i];

                    var prefabHero = ownerSO.Find(x => x.id == idHero).prefabFormationUI;
                    var heroUI = PoolManager.Pools[PoolName.poolUI].Spawn(prefabHero).GetComponent<HeroLineup>();

                    var heroLineup = heroUI.GetComponent<HeroLineup>();
                    _slots[i].SetChild(heroUI, idHero);

                    if (!_heros.Contains(heroUI))
                        _heros.Add(heroUI);
                }
            }
        }

        void Start()
        {
            formation = new List<int>();
            _btnBack.onClick.AddListener(() =>
            {
                foreach (var item in _heros)
                {
                    PoolManager.Pools[PoolName.poolUI].Despawn(item.transform);
                }
                OnBack?.Invoke();
                gameObject.Hide();
                Hide();
            });

            foreach (var item in _slots)
            {
                item.OnChangeID += OnChangeLineUp;
            }

        }

        private void OnChangeLineUp()
        {
            string dg = "[";
            formation.Clear();

            foreach (var item in _slots)
            {
                dg += item.IDHero + ",";
                formation.Add(item.IDHero);
            }
            dg += "]";

            Debug.Log("=>>> " + dg);
            OnChangeFormation?.Invoke(formation);

        }
    }
}
