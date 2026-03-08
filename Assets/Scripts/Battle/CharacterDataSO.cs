using System;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using GameSystems.Skills;

namespace GameSystems.Battle
{
    [CreateAssetMenu(fileName = "CharacterData", menuName = "ScriptableObjects/CharacterDataSO", order = 1)]
    public class CharacterDataSO : ScriptableObject
    {
        public int id;
        public string nameHero;

        [JsonConverter(typeof(StringEnumConverter))]
        public CharacterType type;

        [JsonIgnore] public Sprite iconAvatar;
        [JsonIgnore] public Sprite fullAvatar;
        [JsonIgnore] public Sprite cardAvatar;

        [JsonConverter(typeof(StringEnumConverter))]
        public CharacterRarity rarity;

        public int level = 1;
        public bool isUnlock;

        // Stats
        public CharacterStats stats = new CharacterStats();

        // Skills
        public SkillData skillBasic;
        public SkillData skillUltimate;
        public SkillData skillPassive;
        public SkillData skillAwaken;

        private void OnValidate()
        {
            ChangeAssetName(id + "_" + nameHero);
        }

        [ContextMenu("SaveJson")]
        public void SaveJson()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            Debug.Log(json);
        }

        private void ChangeAssetName(string newName)
        {
#if UNITY_EDITOR
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                UnityEditor.AssetDatabase.RenameAsset(assetPath, newName);
                UnityEditor.AssetDatabase.SaveAssets();
            }
#endif
        }

        public CharacterStats GetStats()
        {
            return stats;
        }

        public void CopyStats(CharacterStats target)
        {
            if (target != null)
            {
                target.hp = stats.hp;
                target.mp = stats.mp;
                target.atk = stats.atk;
                target.pdef = stats.pdef;
                target.mdef = stats.mdef;
                target.speed = stats.speed;
                target.crit = stats.crit;
                target.critRes = stats.critRes;
                target.hitRate = stats.hitRate;
                target.dodgeRate = stats.dodgeRate;
                target.dmgIncr = stats.dmgIncr;
                target.dmgRes = stats.dmgRes;
                target.critDmg = stats.critDmg;
            }
        }
    }

    [System.Serializable]
    public class CharacterStats
    {
        public int hp = 1000;
        public int mp = 100;
        public int atk = 100;
        public float pdef = 10;
        public float mdef = 10;
        public float speed = 50;

        // Advanced stats
        public float crit = 0.05f;
        public float critRes = 0f;
        public float hitRate = 0.95f;
        public float dodgeRate = 0.05f;
        public float dmgIncr = 0f;
        public float dmgRes = 0f;
        public float critDmg = 1.5f;

        // Elemental stats
        public float antiDivine = 0f;
        public float resDivine = 0f;
        public float antiBuddha = 0f;
        public float resBuddha = 0f;
        public float antiSorcerer = 0f;
        public float resSorcerer = 0f;
        public float antiDemon = 0f;
        public float resDemon = 0f;

        public float acc = 1f;
        public float eva = 1f;

        public override string ToString()
        {
            return $"HP:{hp} MP:{mp} ATK:{atk} PDEF:{pdef} MDEF:{mdef} SPD:{speed}";
        }

        public CharacterStats Clone()
        {
            return new CharacterStats
            {
                hp = hp,
                mp = mp,
                atk = atk,
                pdef = pdef,
                mdef = mdef,
                speed = speed,
                crit = crit,
                critRes = critRes,
                hitRate = hitRate,
                dodgeRate = dodgeRate,
                dmgIncr = dmgIncr,
                dmgRes = dmgRes,
                critDmg = critDmg,
                antiDivine = antiDivine,
                resDivine = resDivine,
                antiBuddha = antiBuddha,
                resBuddha = resBuddha,
                antiSorcerer = antiSorcerer,
                resSorcerer = resSorcerer,
                antiDemon = antiDemon,
                resDemon = resDemon,
                acc = acc,
                eva = eva
            };
        }
    }

    public enum CharacterType
    {
        Hero,
        Enemy,
        Boss
    }

    public enum CharacterRarity
    {
        N,
        R,
        SR,
        SSR,
        UR
    }
}
