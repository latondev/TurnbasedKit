using System.Collections;
using System.Collections.Generic;
using Akaal.PvCustomizer.Scripts;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine.Serialization;

namespace MythydRpg
{
    public enum Rarity
    {
        SSR,
        SR,
        R,
        UR,
        N
    }

    public enum TypeCharacter
    {
        Hero,
        Enemy,
        Boss
    }


    [CreateAssetMenu(fileName = "CharacterData", menuName = "ScriptableObjects/CharacterDataSO", order = 1)]
    public class CharacterDataSO : ScriptableObject
    {
        public int id;
        public string nameHero;

        [JsonConverter(typeof(StringEnumConverter))]
        public TypeCharacter type;

        [JsonIgnore]    public Sprite bgCardAvatar;

        [FormerlySerializedAs("cardAvatar")] [JsonIgnore]   public Sprite cardRewardAvatar;
        [JsonIgnore]   public Sprite cardFullAvatar;
        [JsonConverter(typeof(StringEnumConverter))] public Rarity rarity;
        [JsonIgnore]  [PreviewField, PvIcon] public Sprite iconAvatar;
        [JsonIgnore]   public Sprite fullAvatar;
        [JsonIgnore]    public CharacterView prefabCharacterView;
        [JsonIgnore]     public GameObject prefabUI;
        [JsonIgnore]    public GameObject prefabFormationUI;
        public int level = 1;

        [JsonIgnore]    public bool isUnlock;
        public CharacterDataStats data;
        public SkillData skillBasic;
        public SkillData skillUltimate;
        public SkillData skillPassive;
        public SkillData skillAwaken;

        private void OnValidate()
        {
            // Example: change the asset name when you validate
            ChangeAssetName(id + "_" + nameHero);
        }

        [Button]
        public void SaveJson()
        {
            string json = JsonConvert.SerializeObject(this);
            Debug.Log(json);
        }

        private void ChangeAssetName(string newName)
        {
            // Only works in the editor
#if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(this);
            AssetDatabase.RenameAsset(assetPath, newName);
            AssetDatabase.SaveAssets();
#endif
        }
    }

    [System.Serializable]
    public class SkillData
    {
        [HorizontalGroup("Row", 150, MarginRight = 0.1f)] // Nhóm hàng, cột đầu tiên có chiều rộng cố định 50
        [HideLabel]
        [PreviewField]
        [JsonIgnore]   public Sprite iconSkill;

        [HorizontalGroup("Row")] // Cột thứ hai
        [VerticalGroup("Row/Details")]
        [HideLabel]
        public string nameSkill;

        [VerticalGroup("Row/Details")] [MultiLineProperty(4)] [HideLabel]
        public string descriptionSkill;

        public SkillConfig skillConfig;
    }

    public enum LocationType
    {
        All,
        Only,
        Row,
        Column,
        Font,
        Back
    }

    public enum TypeSkill
    {
        Basic,
        Ultimate,
        Passive,
        Awaken
    }

    public enum ActionTypeSkill
    {
        Attack,
        Debuff,
        Buff
    }

    public enum DmgType
    {
        NORMAL = 0,
        Physical = 1,
        Magic = 2
    }

    [System.Serializable]
    public class BonusDmg
    {
        [JsonConverter(typeof(StringEnumConverter))]   public DmgType dmgType;
        public float percentBonusDamage;
    }

    public enum EffectType
    {
        None,
        Heal,
        Quicksand,
        ReduceDmg,
        Burn,
    }

    public enum TypeUnit
    {
        Ally,
        Enemy,
        Self
    }

    public enum HealthState
    {
        None,
        Low,
        High
    }

    [System.Serializable]
    public class HitEffect
    {
        public float percentChanceEffect = 1;
        [JsonConverter(typeof(StringEnumConverter))]   public EffectType effectType;
        [JsonConverter(typeof(StringEnumConverter))]       public TypeUnit typeUnit;
        public PercentEffect percentEffect;
        public PercentEffect percentEffectLimit;

        [JsonConverter(typeof(StringEnumConverter))]    public HealthState healthState;
    }

    [System.Serializable]
    public class PercentEffect
    {
        public float percentBonus;
        [JsonConverter(typeof(StringEnumConverter))]    public TypeUnit typeUnit;
        public float percentdmgAffectedEffect;

        public int duration;
    }


    [System.Serializable]
    public class SkillConfig
    {
        public int numberTarget;
        public BonusDmg bonusDmg;

        public float percentBonusCrit;
        public float percentBonusAccuracy;
        [JsonConverter(typeof(StringEnumConverter))]    public LocationType location;
        [JsonConverter(typeof(StringEnumConverter))]  public TypeSkill typeSkill;
        [JsonConverter(typeof(StringEnumConverter))]  public ActionTypeSkill actionTypeSkill;
        public List<HitEffect> hitEffect;
    }
}