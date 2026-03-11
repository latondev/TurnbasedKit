using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using GameSystems.Skills;
using GameSystems.Stats;

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

        // Stats - using List<StatData> for Unity serialization
        [SerializeField] private List<StatData> stats = new List<StatData>();

        // Skills
        public SkillData skillBasic;
        public SkillData skillUltimate;
        public SkillData skillPassive;
        public SkillData skillAwaken;

        // Properties
        public IReadOnlyList<StatData> Stats => stats.AsReadOnly();

        private void OnValidate()
        {
            // Delay rename to avoid calling AssetDatabase during asset importing
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => {
                ChangeAssetName(id + "_" + nameHero);
            };
#endif
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
            if (string.IsNullOrEmpty(newName)) return;

            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string currentName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (currentName != newName)
                {
                    UnityEditor.AssetDatabase.RenameAsset(assetPath, newName);
                    UnityEditor.AssetDatabase.SaveAssets();
                }
            }
#endif
        }

        /// <summary>
        /// Get stat value by statId
        /// </summary>
        public float GetStatValue(string statId)
        {
            foreach (var stat in stats)
            {
                if (stat.StatId == statId)
                    return stat.BaseValue;
            }
            return 0;
        }

        /// <summary>
        /// Get stat by statId
        /// </summary>
        public StatData GetStat(string statId)
        {
            foreach (var stat in stats)
            {
                if (stat.StatId == statId)
                    return stat;
            }
            return null;
        }

        /// <summary>
        /// Convert to List<Stat> for runtime use (StatsSystem)
        /// </summary>
        public List<Stat> ToStatsList()
        {
            var result = new List<Stat>();
            foreach (var statData in stats)
            {
                result.Add(statData.ToStat());
            }
            return result;
        }

        /// <summary>
        /// Add a new stat to the list
        /// </summary>
        public void AddStat(StatData stat)
        {
            if (stat != null && !stats.Exists(s => s.StatId == stat.StatId))
            {
                stats.Add(stat);
            }
        }

        /// <summary>
        /// Remove a stat by statId
        /// </summary>
        public void RemoveStat(string statId)
        {
            stats.RemoveAll(s => s.StatId == statId);
        }

        /// <summary>
        /// Clear all stats
        /// </summary>
        public void ClearStats()
        {
            stats.Clear();
        }

        /// <summary>
        /// Initialize with default stats if empty
        /// </summary>
        [ContextMenu("InitializeDefaultStats")]
        public void InitializeDefaultStats()
        {
            if (stats.Count == 0)
            {
                stats = StatData.CreateDefaultStats();
            }
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
