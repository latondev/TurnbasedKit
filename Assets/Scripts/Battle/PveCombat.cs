using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// PVE Combat - player vs enemy combat
    /// </summary>
    public class PveCombat : BaseCombat
    {
        [Header("PVE Settings")]
        [SerializeField] protected CombatHelper _combatHelper;
        [SerializeField] private List<int> defaultEnemyLineup = new List<int> { 1, 1, 1, 1, 1, 1 };

        public override void StartCombat()
        {
            Initialized();
            targets = new List<CharacterTurnbase>();
            StartCoroutine(Combat());
        }

        protected override void Initialized()
        {
            // Get formation from FormationController if available
            var formationController = FindObjectOfType<Formation.FormationController>();
            List<int> lineup = new List<int>();
            bool hasFormation = false;

            if (formationController != null)
            {
                // Try to get formation from controller
                var formation = formationController.CurrentFormation;
                if (formation != null)
                {
                    foreach (var slot in formation.Slots)
                    {
                        if (slot.IsOccupied)
                        {
                            lineup.Add(slot.OccupiedUnitId.GetHashCode()); // Use hash as temp ID
                        }
                        else
                        {
                            lineup.Add(0);
                        }
                    }
                    hasFormation = true;
                }
            }

            // If no formation, use defaults
            if (!hasFormation)
            {
                lineup = new List<int> { 1, 2, 3, 0, 0, 0 };
            }

            // Initialize player team
            for (int i = 0; i < lineup.Count; i++)
            {
                var item = lineup[i];
                if (item != 0)
                {
                    var data = GetCharacterData(item);
                    if (data != null && i < _teamAtk.Count)
                    {
                        _teamAtk[i].Initialized(data);
                        _teamAtk[i].SetTeamType(TeamType.ATK);
                        Debug.Log($"PveCombat: Player unit {i} = {data.nameHero}");
                    }
                }
            }

            // Initialize enemy team
            var lineUpStage = defaultEnemyLineup;
            for (int i = 0; i < lineUpStage.Count; i++)
            {
                var item = lineUpStage[i];
                if (item != 0)
                {
                    var enemyData = GetEnemyData(item);
                    if (enemyData != null && i < _teamDef.Count)
                    {
                        _teamDef[i].Initialized(enemyData);
                        _teamDef[i].SetTeamType(TeamType.DEF);
                        Debug.Log($"PveCombat: Enemy unit {i} = {enemyData.nameHero}");
                    }
                }
            }

            // Calculate total HP
            totalHpTeamAtk = 0;
            totalDefTeamAtk = 0;

            foreach (var item in _teamAtk)
            {
                if (item.HealCtr != null)
                {
                    item.HealCtr.OnDie += OnUnitDie;
                    totalHpTeamAtk += (long)item.HealCtr.MaxHealth;
                }
            }

            foreach (var item in _teamDef)
            {
                if (item.HealCtr != null)
                {
                    item.HealCtr.OnDie += OnUnitDie;
                    totalDefTeamAtk += (long)item.HealCtr.MaxHealth;
                }
            }

            Debug.Log($"PveCombat initialized: Player HP={totalHpTeamAtk}, Enemy HP={totalDefTeamAtk}");
        }

        private CharacterDataSO GetCharacterData(int id)
        {
            var manager = CharacterManager.Instance;
            if (manager != null)
            {
                return manager.GetDataHero(id);
            }
            return null;
        }

        private CharacterDataSO GetEnemyData(int id)
        {
            var manager = CharacterManager.Instance;
            if (manager != null)
            {
                return manager.GetDataEnemy(id);
            }
            return null;
        }

        private void OnUnitDie()
        {
            // Handle unit death if needed
        }

        protected override void CombatEnd()
        {
            Debug.Log("PVE Combat ended");
        }

        protected override CombatResult GetCombatResult()
        {
            if (_combatHelper == null)
            {
                _combatHelper = GetComponent<CombatHelper>();
            }

            if (_combatHelper != null)
            {
                return _combatHelper.GetCombatResult(_teamAtk, _teamDef);
            }

            // Fallback: return empty result
            return new CombatResult();
        }

        public void SetupTeams(List<int> playerLineup, List<int> enemyLineup)
        {
            if (playerLineup != null && playerLineup.Count > 0)
            {
                for (int i = 0; i < Mathf.Min(playerLineup.Count, _teamAtk.Count); i++)
                {
                    if (playerLineup[i] != 0)
                    {
                        var data = GetCharacterData(playerLineup[i]);
                        if (data != null)
                        {
                            _teamAtk[i].Initialized(data);
                            _teamAtk[i].SetTeamType(TeamType.ATK);
                        }
                    }
                }
            }

            if (enemyLineup != null && enemyLineup.Count > 0)
            {
                for (int i = 0; i < Mathf.Min(enemyLineup.Count, _teamDef.Count); i++)
                {
                    if (enemyLineup[i] != 0)
                    {
                        var data = GetEnemyData(enemyLineup[i]);
                        if (data != null)
                        {
                            _teamDef[i].Initialized(data);
                            _teamDef[i].SetTeamType(TeamType.DEF);
                        }
                    }
                }
            }
        }
    }
}
