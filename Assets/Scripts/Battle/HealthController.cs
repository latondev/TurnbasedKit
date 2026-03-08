using System;
using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// Health Controller - manages HP, MP, Shield
    /// </summary>
    public class HealthController : MonoBehaviour
    {
        public Action OnDie;

        [SerializeField] private float health;
        [SerializeField] private float shield;
        [SerializeField] private int mana;
        [SerializeField] private float maxHealth;
        [SerializeField] private int maxMana;
        [SerializeField] private float maxShield;
        [SerializeField] private bool isLive;

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

            // Shield absorbs damage first
            if (value < 0 && shield > 0)
            {
                float remainingDamage = Mathf.Abs(value);
                if (shield >= remainingDamage)
                {
                    shield -= remainingDamage;
                    health += remainingDamage; // Cancel the health reduction
                }
                else
                {
                    shield = 0;
                    // health already reduced by remaining damage
                }
            }

            if (health <= 0)
            {
                health = 0;
                isLive = false;
                OnDie?.Invoke();
            }
            else
            {
                isLive = true;
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
            shield = 0;
            maxShield = 0;
        }

        public void SetMaxShield(float shield)
        {
            maxShield = shield;
            this.shield = maxShield;
        }

        public void AddShield(float amount)
        {
            shield += amount;
            shield = Mathf.Min(shield, maxShield);
        }

        public void FullHeal()
        {
            health = maxHealth;
            mana = maxMana;
            shield = maxShield;
            isLive = true;
        }

        public float GetHealthPercentage()
        {
            if (maxHealth <= 0) return 0;
            return health / maxHealth;
        }

        public float GetManaPercentage()
        {
            if (maxMana <= 0) return 0;
            return (float)mana / maxMana;
        }
    }
}
