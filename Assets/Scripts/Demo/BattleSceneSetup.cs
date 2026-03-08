using UnityEngine;
using GameSystems.Battle;
using GameSystems.Skills;
using GameSystems.AutoBattle;
using System.Collections.Generic;

namespace GameSystems.Demo
{
    /// <summary>
    /// Battle Scene Setup - Tự động load prefab và setup battle
    /// </summary>
    public class BattleSceneSetup : MonoBehaviour
    {
        [Header("=== SETTINGS ===")]
        public bool setupOnStart = true;
        public bool createCharacterData = true;

        // Folder chứa prefab Role
        private const string RolePrefabFolder = "Assets/AssetGame/ArtWork/Prefab/Role/";

        // Player prefabs (sẽ tự động load)
        private string[] playerPrefabNames = { "zhujue_fei_yu", "zhujue_jian_ling", "zhujue_wu_sheng", "fei_yu", "jian_ling" };
        private string[] enemyPrefabNames = { "bing_yi", "chu_chu", "tao_tie", "gou_mang", "jiang_hu_ke" };

        // Team positions - 5 units như trong hình
        private Vector3[] playerPositions = new Vector3[]
        {
            new Vector3(-5, 0, -3),  // Hàng trước (front)
            new Vector3(-5, 0, -1),  // Hàng trước
            new Vector3(-5, 0, 1),   // Hàng trước
            new Vector3(-7, 0, -2),  // Hàng sao (back)
            new Vector3(-7, 0, 2),   // Hàng sao
        };

        private Vector3[] enemyPositions = new Vector3[]
        {
            new Vector3(5, 0, -3),   // Hàng trước (front)
            new Vector3(5, 0, -1),   // Hàng trước
            new Vector3(5, 0, 1),    // Hàng trước
            new Vector3(7, 0, -2),   // Hàng sao (back)
            new Vector3(7, 0, 2),    // Hàng sao
        };

        void Start()
        {
            if (setupOnStart)
            {
                Setup();
            }
        }

        public void Setup()
        {
            Debug.Log("<color=cyan>=== Battle Scene Setup ===</color>");

            // 1. Setup Character Manager with data
            SetupCharacterManager();

            // 2. Load và setup Player Team - 5 units
            var playerPrefabs = LoadPrefabs(playerPrefabNames);
            var playerUnits = SetupTeam("PlayerTeam", playerPrefabs, playerPositions, TeamType.ATK);

            // 3. Load và setup Enemy Team - 5 units
            var enemyPrefabs = LoadPrefabs(enemyPrefabNames);
            var enemyUnits = SetupTeam("EnemyTeam", enemyPrefabs, enemyPositions, TeamType.DEF);

            // 4. Setup PveCombat
            SetupCombat(playerUnits, enemyUnits);

            Debug.Log("<color=green>✅ Setup Complete!</color>");
            Debug.Log("<color=yellow>Press F2 = AutoBattle | F5 = TurnBased Battle</color>");
        }

        /// <summary>
        /// Load prefabs từ Role folder
        /// </summary>
        GameObject[] LoadPrefabs(string[] names)
        {
            var prefabs = new List<GameObject>();

#if UNITY_EDITOR
            foreach (var name in names)
            {
                string path = RolePrefabFolder + name + ".prefab";
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                    Debug.Log($"Loaded prefab: {name}");
                }
                else
                {
                    Debug.LogWarning($"Not found: {path}");
                    // Tạo fallback cube nếu không load được
                    prefabs.Add(CreateFallbackPrefab(name));
                }
            }
#else
            // Runtime - thử load từ Resources
            foreach (var name in names)
            {
                var prefab = Resources.Load<GameObject>("Role/" + name);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
                else
                {
                    prefabs.Add(CreateFallbackPrefab(name));
                }
            }
#endif
            return prefabs.ToArray();
        }

        /// <summary>
        /// Tạo prefab đơn giản nếu không load được
        /// </summary>
        GameObject CreateFallbackPrefab(string name)
        {
            var go = new GameObject(name);
            // Thêm SpriteRenderer để nhìn thấy
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = name.Contains("enemy") ? Color.red : Color.blue;
            sr.size = new Vector2(1, 2);
            return go;
        }

        void SetupCharacterManager()
        {
            var cm = FindFirstObjectByType<CharacterManager>();
            if (cm == null)
            {
                var go = new GameObject("CharacterManager");
                cm = go.AddComponent<CharacterManager>();
            }

            if (createCharacterData && cm.Characters.Count == 0)
            {
                CreateCharacterData(cm);
            }
        }

        void CreateCharacterData(CharacterManager cm)
        {
            // Create 5 heroes - Player team
            CreateHero(cm, 1, "Feiyu", 2000, 150, 50, 80, CharacterRarity.SR);
            CreateHero(cm, 2, "Jianling", 1500, 180, 30, 90, CharacterRarity.SR);
            CreateHero(cm, 3, "Wusheng", 2500, 120, 70, 60, CharacterRarity.SR);
            CreateHero(cm, 4, "Knight", 3000, 100, 80, 40, CharacterRarity.R);
            CreateHero(cm, 5, "Healer", 1200, 80, 20, 100, CharacterRarity.R);

            // Create 5 enemies - Enemy team (như trong hình có 5 con rồng xanh)
            CreateEnemy(cm, 1, "Dragon1", 1500, 120, 30, 50);
            CreateEnemy(cm, 2, "Dragon2", 1500, 120, 30, 50);
            CreateEnemy(cm, 3, "Dragon3", 1500, 120, 30, 50);
            CreateEnemy(cm, 4, "Dragon4", 1500, 120, 30, 50);
            CreateEnemy(cm, 5, "Dragon5", 1500, 120, 30, 50);

            Debug.Log($"Created {cm.Characters.Count} heroes, {cm.Enemies.Count} enemies");
        }

        void CreateHero(CharacterManager cm, int id, string name, int hp, int atk, int def, int spd, CharacterRarity rarity)
        {
            var data = ScriptableObject.CreateInstance<CharacterDataSO>();
            data.name = $"Hero_{id}_{name}";
            data.id = id;
            data.nameHero = name;
            data.type = CharacterType.Hero;
            data.rarity = rarity;
            data.level = 1;
            data.isUnlock = true;

            data.stats = new CharacterStats { hp = hp, mp = 100, atk = atk, pdef = def, mdef = def, speed = spd, crit = 0.05f, critDmg = 1.5f };

            data.skillBasic = new SkillData("atk", "Attack", "Basic attack", SkillCategory.Active, SkillElement.Physical, 0, 0, atk);
            data.skillUltimate = new SkillData("ult", "Power Strike", "Powerful attack", SkillCategory.Ultimate, SkillElement.Physical, 30, 5, atk * 2);

            cm.AddCharacter(data);
        }

        void CreateEnemy(CharacterManager cm, int id, string name, int hp, int atk, int def, int spd)
        {
            var data = ScriptableObject.CreateInstance<CharacterDataSO>();
            data.name = $"Enemy_{id}_{name}";
            data.id = id;
            data.nameHero = name;
            data.type = CharacterType.Enemy;
            data.rarity = CharacterRarity.N;
            data.level = 1;
            data.isUnlock = true;

            data.stats = new CharacterStats { hp = hp, mp = 50, atk = atk, pdef = def, mdef = def, speed = spd, crit = 0.03f, critDmg = 1.3f };

            data.skillBasic = new SkillData("atk", "Attack", "Enemy attack", SkillCategory.Active, SkillElement.Physical, 0, 0, atk);

            cm.AddEnemy(data);
        }

        CharacterTurnbase[] SetupTeam(string teamName, GameObject[] prefabs, Vector3[] positions, TeamType teamType)
        {
            var teamGo = new GameObject(teamName);
            var units = new System.Collections.Generic.List<CharacterTurnbase>();

            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] == null) continue;

                Vector3 pos = i < positions.Length ? positions[i] : Vector3.zero;
                var unitGo = Instantiate(prefabs[i], pos, Quaternion.identity, teamGo.transform);
                unitGo.name = $"{teamName}_{i}";

                // Add required components - Battle Logic
                var turnbase = unitGo.AddComponent<CharacterTurnbase>();
                unitGo.AddComponent<CharacterTurnbaseData>();
                unitGo.AddComponent<AbilityController>();
                unitGo.AddComponent<StatusController>();
                unitGo.AddComponent<HealthController>();

                // Add View components - Animation
                AddViewComponents(unitGo);

                turnbase.SetTeamType(teamType);
                units.Add(turnbase);

                // Setup CharacterView
                var view = unitGo.GetComponent<CharacterView>();
                if (view != null)
                {
                    // Set flip for enemy team
                    if (teamType == TeamType.DEF)
                    {
                        view.SetFlipY(TeamType.DEF);
                    }
                }
            }

            // Initialize with data
            var cm = FindFirstObjectByType<CharacterManager>();
            if (cm != null)
            {
                var charList = teamType == TeamType.ATK ? cm.Characters : cm.Enemies;
                for (int i = 0; i < System.Math.Min(units.Count, charList.Count); i++)
                {
                    units[i].Initialized(charList[i]);
                }
            }

            Debug.Log($"Setup {teamName}: {units.Count} units");
            return units.ToArray();
        }

        void AddViewComponents(GameObject go)
        {
            // Add AnimationController if not exists
            if (go.GetComponent<AnimationController>() == null)
            {
                go.AddComponent<AnimationController>();
            }

            // Add AttackBehavior if not exists
            if (go.GetComponent<AttackBehavior>() == null)
            {
                var attack = go.AddComponent<AttackBehavior>();
                // Use reflection to set private fields
                var attackType = typeof(AttackBehavior);
                attackType.GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(attack, GameSystems.Battle.AttackRange.Melee);
                attackType.GetField("moveGo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(attack, "run");
                attackType.GetField("moveBack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(attack, "run");
                attackType.GetField("attackAnimation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(attack, "attack");
                attackType.GetField("idleAnimation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(attack, "idle");
            }

            // Add BehitBehavior if not exists
            if (go.GetComponent<BehitBehavior>() == null)
            {
                go.AddComponent<BehitBehavior>();
            }

            // Add CharacterView if not exists
            if (go.GetComponent<CharacterView>() == null)
            {
                go.AddComponent<CharacterView>();
            }

            Debug.Log($"Added View components to {go.name}");
        }

        void SetupCombat(CharacterTurnbase[] playerUnits, CharacterTurnbase[] enemyUnits)
        {
            var combat = FindFirstObjectByType<PveCombat>();
            if (combat == null)
            {
                var go = new GameObject("PveCombat");
                combat = go.AddComponent<PveCombat>();
                combat.gameObject.AddComponent<CombatHelper>();
            }

            // Set teams via reflection
            var teamAtkField = typeof(PveCombat).GetField("_teamAtk", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var teamDefField = typeof(PveCombat).GetField("_teamDef", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            teamAtkField?.SetValue(combat, new System.Collections.Generic.List<CharacterTurnbase>(playerUnits));
            teamDefField?.SetValue(combat, new System.Collections.Generic.List<CharacterTurnbase>(enemyUnits));
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                var battle = FindFirstObjectByType<AutoBattleController>();
                battle?.StartBattle();
            }
            if (Input.GetKeyDown(KeyCode.F5))
            {
                var combat = FindFirstObjectByType<PveCombat>();
                combat?.StartCombat();
            }
        }
    }
}
