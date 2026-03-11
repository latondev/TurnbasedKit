using UnityEngine;
using GameSystems.Stats;

namespace GameSystems.Battle
{
    /// <summary>
    /// Character Turnbase Data - stores stats for battle
    /// Uses StatData from StatsSystem
    /// </summary>
    public class CharacterTurnbaseData : MonoBehaviour
    {
        [SerializeField] private bool hasData;
        [SerializeField] private StatData statBase;
        [SerializeField] private StatData statRuntime;

        public StatData Stat => statRuntime;
        public bool HasData => hasData;

        private void Awake()
        {
            statBase = new StatData();
            statRuntime = new StatData();
        }

        /// <summary>
        /// Initialize from CharacterDataSO using List<StatData>
        /// </summary>
        public void InitData(CharacterDataSO characterDataSO)
        {
            if (characterDataSO == null) return;

            // Copy stats from CharacterDataSO
            var sourceStats = characterDataSO.Stats;
            if (sourceStats == null || sourceStats.Count == 0)
            {
                Debug.LogWarning($"CharacterTurnbaseData: {characterDataSO.nameHero} has no stats!");
                return;
            }

            // Clone stats for base (original values)
            statBase = new StatData(
                "base",
                "Base Stats",
                StatType.Health,
                characterDataSO.GetStatValue("hp"),
                characterDataSO.GetStatValue("hp"),
                true,
                0f
            );

            // Clone stats for runtime (can be modified during battle)
            statRuntime = new StatData(
                "runtime",
                "Runtime Stats",
                StatType.Health,
                characterDataSO.GetStatValue("hp"),
                characterDataSO.GetStatValue("hp"),
                true,
                0f
            );

            hasData = true;
        }
    }
}
