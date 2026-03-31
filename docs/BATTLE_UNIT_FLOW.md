# Battle Unit Flow

## Runtime Flow

`AutoBattleController`
-> `BattleUnit.BeginTurn()`
-> `BattleUnitRuntimeService.BeginTurn()`
-> `BattleUnitCombatService.Attack()/CastSkill()/TakeDamage()/Heal()`
-> `BattleUnitRuntimeService.EndTurn()`
-> `StatusController.TickTurn()`
-> `BattleUnitRuntimeService` events
-> `BattleSceneSetup` / `BattleUIView` / `BattleVisualManager`

## Responsibilities

`BattleUnit`
- facade cho unit battle
- expose stat, skill, status, log, events
- forward API sang service đúng lớp

`BattleUnitRuntimeService`
- turn tracking
- cooldown
- status apply/remove/tick
- temporary modifier lifetime
- defeat/reset events

`BattleUnitCombatService`
- attack / skill damage
- crit
- heal / damage
- mana set / consumption
- damage counters

`BattleUnitEventBridgeService`
- subscribe runtime events
- forward sang `BattleUnit` public events
- giữ `BattleUnit` khỏi phải tự bind/unbind handler

`BattleUnitLogService`
- battle log/history
- append, clear, dispose
- giữ list log cũ để UI/editor đọc như trước

`StatusController`
- lưu status effect active
- tick turn cho poison/burn/regeneration
- remove status khi hết lượt

## New Class Schema

`BattleUnit`
- `RuntimeService`
- `combatService`
- `eventBridgeService`
- `logService`
- `StatController`
- `ActionsLog`
- `DamageDealt`
- `DamageTaken`

`BattleUnitRuntimeService`
- `OnStatChanged`
- `OnStatusApplied`
- `OnStatusRemoved`
- `OnTurnStarted`
- `OnTurnEnded`
- `OnDefeated`
- `OnReset`
- `OnCooldownChanged`

`BattleUnitCombatService`
- `Attack(target)`
- `CastSkill(target)`
- `TakeDamage(amount)`
- `Heal(amount)`
- `SetMana(current, max)`
- `ResetCombat()`
