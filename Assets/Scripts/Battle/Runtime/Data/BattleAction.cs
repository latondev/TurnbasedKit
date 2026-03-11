using System;
using UnityEngine;

namespace GameSystems.AutoBattle
{
    /// <summary>
    /// Represents a single action in battle
    /// </summary>
    [Serializable]
    public class BattleAction
    {
        public BattleUnit actor;
        public BattleUnit target;
        public ActionType type;
        public int value;
        public bool isCritical;
        public string description;
        public float timestamp;

        public BattleAction(BattleUnit actor, BattleUnit target, ActionType type, int value = 0)
        {
            this.actor = actor;
            this.target = target;
            this.type = type;
            this.value = value;
            this.isCritical = false;
            this.timestamp = Time.time;
            
            GenerateDescription();
        }

        private void GenerateDescription()
        {
            description = type switch
            {
                ActionType.Attack => $"{actor.UnitName} attacks {target.UnitName} for {value} damage",
                ActionType.Skill => $"{actor.UnitName} uses skill on {target.UnitName}",
                ActionType.Heal => $"{actor.UnitName} heals {target.UnitName} for {value} HP",
                ActionType.Defend => $"{actor.UnitName} defends",
                _ => "Unknown action"
            };
            
            if (isCritical)
                description += " [CRIT!]";
        }

        public override string ToString()
        {
            return description;
        }
    }

    public enum ActionType
    {
        Attack,
        Skill,
        Heal,
        Defend
    }

    /// <summary>
    /// Represents a turn in battle
    /// </summary>
    [Serializable]
    public class BattleTurn
    {
        public int turnNumber;
        public BattleUnit activeUnit;
        public BattleAction action;
        public float turnStartTime;
        public float turnEndTime;

        public BattleTurn(int turnNumber, BattleUnit activeUnit)
        {
            this.turnNumber = turnNumber;
            this.activeUnit = activeUnit;
            this.turnStartTime = Time.time;
        }

        public void SetAction(BattleAction action)
        {
            this.action = action;
            this.turnEndTime = Time.time;
        }

        public float GetTurnDuration()
        {
            return turnEndTime - turnStartTime;
        }

        public override string ToString()
        {
            return $"Turn {turnNumber}: {action?.description ?? "No action"}";
        }
    }
}
