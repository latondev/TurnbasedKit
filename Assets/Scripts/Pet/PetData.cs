using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Pet
{
    public enum PetRarity { Common, Rare, Epic, Legendary, Mythic }
    public enum PetType { Beast, Dragon, Spirit, Elemental }

    [Serializable]
    public class PetStats
    {
        public float atkBonus = 10f;      // % bonus cho player
        public float hpBonus = 5f;
        public float expBonus = 0f;       // % bonus exp gain
        public float goldBonus = 0f;      // % bonus gold gain
    }

    [Serializable]
    public class PetSkill
    {
        public string skillId;
        public string name;
        public float procChance = 0.1f;   // 10% proc khi player attack
        public int damage = 0;            // nếu skill gây sát thương
        public int heal = 0;              // nếu skill hồi máu
        public float cooldown = 5f;
        [TextArea] public string description;
    }

    [CreateAssetMenu(fileName = "PetData", menuName = "Game/Pet/Pet Data")]
    public class PetData : ScriptableObject
    {
        [Header("Identity")]
        public string petId = "pet_001";
        public string petName = "Fluffy";
        public PetRarity rarity = PetRarity.Common;
        public PetType type = PetType.Beast;
        public Sprite icon;
        public GameObject prefab;  // 3D model or 2D sprite prefab

        [Header("Growth")]
        public int maxLevel = 50;
        public AnimationCurve expCurve;  // exp needed per level
        public int maxAffection = 100;

        [Header("Base Stats")]
        public PetStats baseStats = new PetStats();

        [Header("Skills")]
        public List<PetSkill> skills = new List<PetSkill>();

        [Header("Evolution")]
        public PetData evolvesTo;
        public int evolveRequiredLevel = 30;
        public int evolveRequiredAffection = 80;
        public string evolveMaterialId;
        public int evolveMaterialQty = 10;

        [Header("Flavor")]
        [TextArea] public string description;

        public long GetExpForLevel(int level)
        {
            if (expCurve == null || expCurve.length == 0)
                return 100 * level * level; // fallback exponential
            float t = Mathf.Clamp01((float)level / maxLevel);
            return (long)(expCurve.Evaluate(t) * 1000);
        }
    }
}
