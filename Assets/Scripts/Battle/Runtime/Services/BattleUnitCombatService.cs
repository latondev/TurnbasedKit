using UnityEngine;
using GameSystems.Stats;
using GameSystems.Skills;

namespace GameSystems.AutoBattle
{
    /// <summary>
    /// Owns the combat math for a BattleUnit:
    /// attack, skill, damage, heal, and mana changes.
    /// </summary>
    public sealed class BattleUnitCombatService
    {
        private readonly BattleUnit _owner;

        public BattleUnitCombatService(BattleUnit owner)
        {
            _owner = owner;
        }

        public int DamageDealt { get; private set; }
        public int DamageTaken { get; private set; }

        public int CurrentCooldown => _owner?.RuntimeService?.CurrentCooldown ?? 0;
        public bool IsSkillReady => _owner?.RuntimeService?.IsSkillReady ?? true;

        public int CastSkill(BattleUnit target)
        {
            if (!IsUsable() || target == null || target == _owner)
            {
                return 0;
            }

            var skill = _owner.EquippedSkill;
            if (skill == null)
            {
                _owner.LogBattleAction("<color=red>No skill equipped!</color>");
                return 0;
            }

            int manaCost = skill.GetScaledManaCost();
            if (_owner.CurrentMana < manaCost)
            {
                _owner.LogBattleAction($"<color=red>Not enough mana! Need {manaCost}, have {_owner.CurrentMana}</color>");
                return 0;
            }

            if (!IsSkillReady)
            {
                _owner.LogBattleAction($"<color=orange>Skill on cooldown: {CurrentCooldown} turns</color>");
                return 0;
            }

            _owner.StatController?.ModifyStat(StatType.Mana, -manaCost);
            _owner.RuntimeService?.SetCooldown(_owner.SkillCooldown);

            float skillDamage = skill.GetTotalDamage() * _owner.SkillDamageMultiplier;
            int baseDamage = Mathf.Max(1, Mathf.RoundToInt(skillDamage) - (target.FinalDefense / 2));
            bool isCrit = Random.value < (_owner.CritRate * 2f);
            int finalDamage = isCrit ? Mathf.RoundToInt(baseDamage * _owner.CritDamage) : baseDamage;

            int actualDamage = target.TakeDamage(finalDamage);
            DamageDealt += actualDamage;

            string critText = isCrit ? " [CRIT!]" : "";
            _owner.LogBattleAction($"💥 Used [{skill.SkillName}] on {target.UnitName} for {actualDamage} damage{critText}");
            target.LogBattleAction($"Hit by [{skill.SkillName}] from {_owner.UnitName} for {actualDamage}{critText}");

            return actualDamage;
        }

        public int Attack(BattleUnit target)
        {
            if (!IsUsable() || target == null || target == _owner)
            {
                return 0;
            }

            int baseDamage = Mathf.Max(1, _owner.FinalAttack - target.FinalDefense);
            bool isCrit = Random.value < _owner.CritRate;
            int finalDamage = isCrit ? Mathf.RoundToInt(baseDamage * _owner.CritDamage) : baseDamage;

            int actualDamage = target.TakeDamage(finalDamage);
            DamageDealt += actualDamage;

            string critText = isCrit ? " [CRIT!]" : "";
            string rangeText = _owner.Range == AttackRange.Ranged ? "🏹" : "⚔️";
            _owner.LogBattleAction($"{rangeText} Attacked {target.UnitName} for {actualDamage} damage{critText}");
            target.LogBattleAction($"Took {actualDamage} damage from {_owner.UnitName}{critText}");

            return actualDamage;
        }

        public int TakeDamage(int damage)
        {
            if (!IsUsable() || damage <= 0)
            {
                return 0;
            }

            int actualDamage = Mathf.RoundToInt(_owner.StatController.TakeDamage(damage));
            DamageTaken += actualDamage;
            return actualDamage;
        }

        public int Heal(int amount)
        {
            if (!IsUsable() || amount <= 0)
            {
                return 0;
            }

            int actualHeal = Mathf.RoundToInt(_owner.StatController.Heal(amount));
            _owner.LogBattleAction($"<color=green>Healed for {actualHeal} HP</color>");
            return actualHeal;
        }

        public void SetMana(int current, int max)
        {
            if (_owner?.StatController == null)
            {
                return;
            }

            var mpStat = _owner.StatController.GetStat(StatType.Mana);
            if (mpStat != null)
            {
                mpStat.IncreaseMax(max - _owner.MaxMana);
                mpStat.SetCurrent(Mathf.Clamp(current, 0, _owner.MaxMana));
            }
        }

        public void ResetCombat()
        {
            DamageDealt = 0;
            DamageTaken = 0;
        }

        private bool IsUsable()
        {
            return _owner != null && _owner.IsAlive && _owner.StatController != null;
        }
    }
}
