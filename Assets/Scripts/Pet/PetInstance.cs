using System;
using UnityEngine;

namespace GameSystems.Pet
{
    [Serializable]
    public class PetInstance
    {
        public string instanceId = Guid.NewGuid().ToString();
        public string petId;
        
        [Header("Progression")]
        public int level = 1;
        public long exp = 0;
        public int affection = 0;
        
        [Header("Customization")]
        public string nickname = "";
        public string skinId = "";

        [Header("Meta")]
        public DateTime acquiredUtc;
        public long totalBattles = 0;

        public PetInstance(string petId)
        {
            this.petId = petId;
            this.acquiredUtc = DateTime.UtcNow;
        }

        public void AddExp(long amount, PetData data)
        {
            exp += amount;
            while (level < data.maxLevel && exp >= data.GetExpForLevel(level))
            {
                exp -= data.GetExpForLevel(level);
                level++;
            }
        }

        public void AddAffection(int amount, PetData data)
        {
            affection = Mathf.Clamp(affection + amount, 0, data.maxAffection);
        }

        public float ExpProgress01(PetData data)
        {
            long needed = data.GetExpForLevel(level);
            return needed > 0 ? Mathf.Clamp01((float)exp / needed) : 1f;
        }

        public PetStats GetCurrentStats(PetData data)
        {
            // Scale stats theo level
            float levelMult = 1f + (level - 1) * 0.05f; // +5% per level
            return new PetStats
            {
                atkBonus = data.baseStats.atkBonus * levelMult,
                hpBonus = data.baseStats.hpBonus * levelMult,
                expBonus = data.baseStats.expBonus * levelMult,
                goldBonus = data.baseStats.goldBonus * levelMult
            };
        }

        public bool CanEvolve(PetData data, Func<string, int, bool> hasMaterial)
        {
            if (data.evolvesTo == null) return false;
            if (level < data.evolveRequiredLevel) return false;
            if (affection < data.evolveRequiredAffection) return false;
            if (!string.IsNullOrEmpty(data.evolveMaterialId) && data.evolveMaterialQty > 0)
                return hasMaterial?.Invoke(data.evolveMaterialId, data.evolveMaterialQty) ?? false;
            return true;
        }
    }
}
