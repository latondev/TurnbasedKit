using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Common
{
    /// <summary>
    /// Game State Manager - quản lý toàn bộ trạng thái game
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        [Header("Player Resources")]
        [SerializeField] private long gold = 1000;
        [SerializeField] private int gems = 0;
        [SerializeField] private int energy = 100;
        [SerializeField] private int maxEnergy = 100;
        [SerializeField] private float energyRegenRate = 1f; // per minute

        [Header("Player Progression")]
        [SerializeField] private int playerLevel = 1;
        [SerializeField] private long experience = 0;
        [SerializeField] private long experienceToNextLevel = 100;
        [SerializeField] private int statPointsPerLevel = 5;

        [Header("Battle Stats")]
        [SerializeField] private int totalBattles = 0;
        [SerializeField] private int victories = 0;
        [SerializeField] private int defeats = 0;
        [SerializeField] private int highestWave = 0;

        [Header("Settings")]
        [SerializeField] private bool debugMode = true;
        [SerializeField] private string saveKey = "GAME_STATE_V1";

        // Stats Points
        [SerializeField] private int availableStatPoints = 0;
        [SerializeField] private int bonusAttack = 0;
        [SerializeField] private int bonusDefense = 0;
        [SerializeField] private int bonusHealth = 0;
        [SerializeField] private int bonusSpeed = 0;

        // Singleton
        public static GameStateManager Instance { get; private set; }

        #region Properties

        public long Gold => gold;
        public int Gems => gems;
        public int Energy => energy;
        public int MaxEnergy => maxEnergy;
        public int PlayerLevel => playerLevel;
        public long Experience => experience;
        public long ExperienceToNextLevel => experienceToNextLevel;
        public int TotalBattles => totalBattles;
        public int Victories => victories;
        public int Defeats => defeats;
        public int HighestWave => highestWave;
        public int AvailableStatPoints => availableStatPoints;

        // Bonus stats from level ups
        public int BonusAttack => bonusAttack;
        public int BonusDefense => bonusDefense;
        public int BonusHealth => bonusHealth;
        public int BonusSpeed => bonusSpeed;

        #endregion

        #region Events

        public event Action<long> OnGoldChanged;
        public event Action<int> OnGemsChanged;
        public event Action<int> OnEnergyChanged;
        public event Action<int> OnLevelUp;
        public event Action<long> OnExperienceGained;
        public event Action<int> OnStatPointsChanged;
        public event Action OnGameStateLoaded;

        #endregion

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            Load();
        }

        void Update()
        {
            RegenerateEnergy();
        }

        void OnDestroy()
        {
            Save();
        }

        #region Resources

        /// <summary>
        /// Thêm gold
        /// </summary>
        public void AddGold(long amount)
        {
            gold += amount;
            OnGoldChanged?.Invoke(gold);
            LogDebug($"<color=yellow>+{amount} Gold</color> (Total: {gold})");
        }

        /// <summary>
        /// Trừ gold (true nếu thành công)
        /// </summary>
        public bool SpendGold(long amount)
        {
            if (gold >= amount)
            {
                gold -= amount;
                OnGoldChanged?.Invoke(gold);
                LogDebug($"<color=red>-{amount} Gold</color> (Remaining: {gold})");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Thêm gems
        /// </summary>
        public void AddGems(int amount)
        {
            gems += amount;
            OnGemsChanged?.Invoke(gems);
            LogDebug($"<color=magenta>+{amount} Gems</color> (Total: {gems})");
        }

        /// <summary>
        /// Trừ gems (true nếu thành công)
        /// </summary>
        public bool SpendGems(int amount)
        {
            if (gems >= amount)
            {
                gems -= amount;
                OnGemsChanged?.Invoke(gems);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sử dụng energy cho battle
        /// </summary>
        public bool SpendEnergy(int amount)
        {
            if (energy >= amount)
            {
                energy -= amount;
                OnEnergyChanged?.Invoke(energy);
                LogDebug($"<color=cyan>-{amount} Energy</color> (Remaining: {energy})");
                return true;
            }
            LogDebug("<color=red>Not enough energy!</color>");
            return false;
        }

        /// <summary>
        /// Thêm energy
        /// </summary>
        public void AddEnergy(int amount)
        {
            energy = Mathf.Min(energy + amount, maxEnergy);
            OnEnergyChanged?.Invoke(energy);
        }

        /// <summary>
        /// Thêm health (for demo - not stored, just for immediate use)
        /// </summary>
        public int AddHealth(int amount)
        {
            Debug.Log($"<color=green>Healed +{amount} HP</color>");
            return amount;
        }

        /// <summary>
        /// Thêm mana (for demo - not stored, just for immediate use)
        /// </summary>
        public int AddMana(int amount)
        {
            Debug.Log($"<color=blue>Restored +{amount} Mana</color>");
            return amount;
        }

        /// <summary>
        /// Hồi phục energy theo thời gian
        /// </summary>
        private void RegenerateEnergy()
        {
            if (energy < maxEnergy)
            {
                // Regen rate is per minute, so divide by 60 for per second
                energy = Mathf.Min(energy + Mathf.RoundToInt(energyRegenRate * Time.deltaTime / 60f), maxEnergy);
            }
        }

        /// <summary>
        /// Full energy
        /// </summary>
        public void RestoreFullEnergy()
        {
            energy = maxEnergy;
            OnEnergyChanged?.Invoke(energy);
        }

        #endregion

        #region Experience & Level

        /// <summary>
        /// Thêm experience
        /// </summary>
        public void AddExperience(long amount)
        {
            experience += amount;
            OnExperienceGained?.Invoke(amount);

            // Check for level up
            while (experience >= experienceToNextLevel)
            {
                LevelUp();
            }

            LogDebug($"<color=green>+{amount} EXP</color> ({experience}/{experienceToNextLevel})");
        }

        /// <summary>
        /// Level up player
        /// </summary>
        private void LevelUp()
        {
            experience -= experienceToNextLevel;
            playerLevel++;

            // Increase EXP requirement
            experienceToNextLevel = CalculateExpForLevel(playerLevel + 1);

            // Give stat points
            availableStatPoints += statPointsPerLevel;

            // Increase max energy
            maxEnergy += 10;

            OnLevelUp?.Invoke(playerLevel);
            OnStatPointsChanged?.Invoke(availableStatPoints);

            LogDebug($"<color=yellow>🎉 LEVEL UP! → {playerLevel}</color>");
            LogDebug($"  +{statPointsPerLevel} stat points");
            LogDebug($"  Max Energy: {maxEnergy}");
        }

        /// <summary>
        /// Tính EXP cần cho level
        /// </summary>
        private long CalculateExpForLevel(int level)
        {
            // Exponential curve: 100, 200, 350, 550, 800...
            return 50L * level * (level - 1) + 100;
        }

        #endregion

        #region Stat Points

        /// <summary>
        /// Cộng điểm stat
        /// </summary>
        public bool AllocateStatPoint(string statType)
        {
            if (availableStatPoints <= 0)
            {
                LogDebug("<color=red>No stat points available!</color>");
                return false;
            }

            switch (statType.ToLower())
            {
                case "attack":
                case "atk":
                    bonusAttack += 5;
                    break;
                case "defense":
                case "def":
                    bonusDefense += 5;
                    break;
                case "health":
                case "hp":
                    bonusHealth += 50;
                    break;
                case "speed":
                case "spd":
                    bonusSpeed += 3;
                    break;
                default:
                    LogDebug($"<color=red>Unknown stat: {statType}</color>");
                    return false;
            }

            availableStatPoints--;
            OnStatPointsChanged?.Invoke(availableStatPoints);
            LogDebug($"<color=green>+{statType} allocated</color> ({availableStatPoints} remaining)");
            return true;
        }

        /// <summary>
        /// Lấy tổng bonus stats
        /// </summary>
        public PlayerStats GetBonusStats()
        {
            var stats = new PlayerStats();
            stats.BaseAttack = bonusAttack;
            stats.BaseDefense = bonusDefense;
            stats.BaseHealth = bonusHealth;
            stats.BaseSpeed = bonusSpeed;
            return stats;
        }

        #endregion

        #region Battle Stats

        /// <summary>
        /// Ghi nhận chiến thắng
        /// </summary>
        public void RecordVictory(int wave = 1)
        {
            totalBattles++;
            victories++;
            if (wave > highestWave) highestWave = wave;

            // Reward
            AddGold(50 + wave * 10);
            AddExperience(20 + wave * 5);
        }

        /// <summary>
        /// Ghi nhận thất bại
        /// </summary>
        public void RecordDefeat(int wave = 1)
        {
            totalBattles++;
            defeats++;

            // Smaller reward for losing
            AddExperience(5 + wave);
        }

        #endregion

        #region Save/Load

        public void Save()
        {
            var saveData = new GameStateSaveData
            {
                gold = gold,
                gems = gems,
                energy = energy,
                maxEnergy = maxEnergy,
                playerLevel = playerLevel,
                experience = experience,
                experienceToNextLevel = experienceToNextLevel,
                totalBattles = totalBattles,
                victories = victories,
                defeats = defeats,
                highestWave = highestWave,
                availableStatPoints = availableStatPoints,
                bonusAttack = bonusAttack,
                bonusDefense = bonusDefense,
                bonusHealth = bonusHealth,
                bonusSpeed = bonusSpeed,
                lastSaveTime = DateTime.UtcNow
            };

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(saveKey, json);
            PlayerPrefs.Save();

            LogDebug("Game state saved!");
        }

        public void Load()
        {
            string json = PlayerPrefs.GetString(saveKey, "");
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var saveData = JsonUtility.FromJson<GameStateSaveData>(json);

                gold = saveData.gold;
                gems = saveData.gems;
                energy = saveData.energy;
                maxEnergy = saveData.maxEnergy;
                playerLevel = saveData.playerLevel;
                experience = saveData.experience;
                experienceToNextLevel = saveData.experienceToNextLevel;
                totalBattles = saveData.totalBattles;
                victories = saveData.victories;
                defeats = saveData.defeats;
                highestWave = saveData.highestWave;
                availableStatPoints = saveData.availableStatPoints;
                bonusAttack = saveData.bonusAttack;
                bonusDefense = saveData.bonusDefense;
                bonusHealth = saveData.bonusHealth;
                bonusSpeed = saveData.bonusSpeed;

                // Calculate offline energy regen
                if (saveData.lastSaveTime != default)
                {
                    var offlineTime = DateTime.UtcNow - saveData.lastSaveTime;
                    int energyRegained = Mathf.FloorToInt((float)offlineTime.TotalMinutes * energyRegenRate);
                    if (energyRegained > 0)
                    {
                        energy = Mathf.Min(energy + energyRegained, maxEnergy);
                        LogDebug($"<color=green>+{energyRegained} offline energy</color>");
                    }
                }

                OnGameStateLoaded?.Invoke();
                LogDebug("Game state loaded!");
            }
            catch (Exception e)
            {
                Debug.LogError($"<color=red>Failed to load game state: {e.Message}</color>");
            }
        }

        public void ResetProgress()
        {
            gold = 1000;
            gems = 0;
            energy = 100;
            maxEnergy = 100;
            playerLevel = 1;
            experience = 0;
            experienceToNextLevel = 100;
            totalBattles = 0;
            victories = 0;
            defeats = 0;
            highestWave = 0;
            availableStatPoints = 0;
            bonusAttack = 0;
            bonusDefense = 0;
            bonusHealth = 0;
            bonusSpeed = 0;

            Save();
            OnGameStateLoaded?.Invoke();
            LogDebug("<color=yellow>Progress reset!</color>");
        }

        #endregion

        #region Debug

        public void ShowGameState()
        {
            Debug.Log("\n<color=cyan>═══════════ Game State ═══════════</color>");
            Debug.Log($"<color=yellow>💰 Gold:</color> {gold:N0}");
            Debug.Log($"<color=magenta>💎 Gems:</color> {gems}");
            Debug.Log($"<color=cyan>⚡ Energy:</color> {energy}/{maxEnergy}");
            Debug.Log($"<color=green>⭐ Level:</color> {playerLevel} (EXP: {experience:N0}/{experienceToNextLevel:N0})");
            Debug.Log($"<color=yellow>📊 Stat Points:</color> {availableStatPoints}");
            Debug.Log($"\n<color=yellow>⚔️ Battle Stats:</color>");
            Debug.Log($"  Total: {totalBattles} | Wins: {victories} | Losses: {defeats}");
            Debug.Log($"  Win Rate: {(totalBattles > 0 ? (float)victories / totalBattles * 100f : 0f):F1}%");
            Debug.Log($"  Highest Wave: {highestWave}");
            Debug.Log($"\n<color=yellow>💪 Bonus Stats:</color>");
            Debug.Log($"  ATK: +{bonusAttack} | DEF: +{bonusDefense} | HP: +{bonusHealth} | SPD: +{bonusSpeed}");
            Debug.Log("<color=cyan>════════════════════════════════════════</color>\n");
        }

        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"<color=orange>[GameState]</color> {message}");
            }
        }

        #endregion

        #region Helper Classes

        [Serializable]
        private class GameStateSaveData
        {
            public long gold;
            public int gems;
            public int energy;
            public int maxEnergy;
            public int playerLevel;
            public long experience;
            public long experienceToNextLevel;
            public int totalBattles;
            public int victories;
            public int defeats;
            public int highestWave;
            public int availableStatPoints;
            public int bonusAttack;
            public int bonusDefense;
            public int bonusHealth;
            public int bonusSpeed;
            public DateTime lastSaveTime;
        }

        #endregion
    }
}
