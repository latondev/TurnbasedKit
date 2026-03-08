using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MythydRpg
{
    public class IndexSlot : MonoBehaviour
    {
        public Action OnChangeID;
        [SerializeField] int id;
        [SerializeField] int _idHero;

        [SerializeField] HeroLineup _hero;



        public int ID { get { return id; } }
        public int IDHero { get { return _idHero; } }

        public HeroLineup Hero { get { return _hero; } }
        private void OnValidate()
        {
            id = transform.GetSiblingIndex();
        }


        void Start()
        {

        }

        public void SetChild(HeroLineup heroLineup, int idHero)
        {
            _idHero = idHero;
            heroLineup.transform.SetParent(transform);
            heroLineup.ResetPosition();
            _hero = heroLineup;
            heroLineup.SetCallback(OnSwap);

        }

        public void ChangeHero(int id, HeroLineup hero)
        {
            this._idHero = id;
            if (hero == null)
            {
                _hero = null;
                _idHero = 0;
                return;
            }
            _hero = hero;
            _hero.SetCallback(OnSwap);
            _hero.transform.SetParent(transform);
            _hero.ResetPosition();

        }


        private void OnSwap(IndexSlot slot)
        {
            if (slot == null) return;
            int idTarget = slot.IDHero;
            int idSelf = _idHero;


            HeroLineup heroTarget = slot.Hero;
            HeroLineup heroSelf = _hero;

            slot.ChangeHero(idSelf, heroSelf);
            this.ChangeHero(idTarget, heroTarget);
            OnChangeID?.Invoke();

        }
    }
}
