using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MythydRpg
{
    public class HealthController : MonoBehaviour
    {
        public Action OnDie;
        [SerializeField] float health;
        [SerializeField] float shield;
        [SerializeField] int mana;
        [SerializeField] float maxHealth;
        [SerializeField] int maxMana;
        [SerializeField] float maxShield;
        [SerializeField] bool isLive;

        public float Health => health;
        public float Shield => shield;
        public int Mana => mana;
        public bool IsDead => health <= 0;
        public float MaxHealth => maxHealth;
        public int MaxMana => maxMana;
        public float MaxShield => maxShield;


        private void Awake()
        {
            health = 0;
            //mana = 0;
        }

        void Start()
        {
        }

        public void AddMana(int value)
        {
            mana += value;
            if (mana >= maxMana)
            {
                mana = maxMana;
            }
        }

        public void ChangeHealth(float value)
        {
            health += value;
            if (health <= 0)
            {
                health = 0;
                isLive = false;
                OnDie?.Invoke();
            }
        }

        public void ResetMana()
        {
            mana = 0;
        }

        public bool CanSkill()
        {
            return mana >= maxMana;
        }

        public void Init(int statHp, int statMp)
        {
            isLive = true;
            maxHealth = statHp;
            maxMana = statMp;
            health = maxHealth;
            mana = 0;
        }
    }
}