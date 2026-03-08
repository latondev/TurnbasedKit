# Scripts Architecture Documentation

## Overview
TurnbasedKit là Unity game project với các hệ thống game mechanics được xây dựng theo modular design patterns.

---

## Folder Structure

```
Assets/Scripts/
├── Battle/          # Battle system (turn-based combat)
├── Common/          # Shared utilities (PlayerStats, Calculator)
├── Demo/            # Demo scripts
├── DesignPatterns/  # (Reserved for patterns)
├── Equipment/       # Equipment system (items, sets, bonuses)
├── Formation/       # Formation/team positioning system
├── Inventory/       # Item inventory management
├── Pet/             # Pet companion system
├── Skill/           # Skill system (active, passive, ultimate)
├── Time/            # Time management (cooldowns, timers, events)
└── Tool/            # Editor tools (Spine setup)
```

---

## Data Flow

```
PlayerStatsCalculator (Common)
    ├── EquipmentController (Equipment)
    ├── FormationController (Formation)
    └── PetManager (Pet)
            ↓
    PlayerStats (Base + Multipliers)
            ↓
    BattleUnit.ApplyPlayerStats()
            ↓
    AutoBattleController (Battle)
```

---

## Core Systems

### 1. Battle System (`Battle/`)

| File | Class | Description |
|------|-------|-------------|
| AutoBattleController.cs | `AutoBattleController` | Main battle loop, turn management, AI decisions |
| BattleUnit.cs | `BattleUnit` | Individual unit: HP, ATK, DEF, SPD, skills, mana |
| BattleAction.cs | `BattleAction`, `BattleTurn` | Action representation, turn history |
| CharacterManager.cs | `CharacterManager` | Character database, power config |
| CharacterDataSO.cs | `CharacterDataSO` | ScriptableObject character data |
| CharacterTurnbase.cs | `CharacterTurnbase` | Turn-based character behavior |
| CharacterTurnbaseData.cs | `CharacterTurnbaseData` | Character turn data |
| CombatHelper.cs | `CombatHelper` | Combat calculation utilities |
| BaseCombat.cs | `BaseCombat` | Base combat logic |
| PveCombat.cs | `PveCombat` | PvE combat implementation |
| AbilityController.cs | `AbilityController` | Ability management |
| HealthController.cs | `HealthController` | Health management |
| StatusController.cs | `StatusController` | Status effects (buffs/debuffs) |
| View/ | | (Battle view components) |

**Key Enums:**
- `BattleState`: Idle → InProgress → Paused → Ended
- `BattleOutcome`: Victory, Defeat, Draw
- `UnitType`: Player, Enemy, Boss, Ally
- `AttackRange`: Melee, Ranged
- `ActionType`: Attack, Skill, Heal, Defend

**Battle Flow:**
```
InitializeBattle() → BattleLoop() (per turn)
    ↓
For each unit: DecideAction() → ExecuteAction()
    ↓
CheckBattleEnd() → EndBattle()
```

---

### 2. Player Stats System (`Common/`)

| File | Class | Description |
|------|-------|-------------|
| PlayerStats.cs | `PlayerStats` | Unified stats: Base + Multipliers |
| PlayerStatsCalculator.cs | `PlayerStatsCalculator` | Aggregates stats from Equipment + Formation + Pet |
| GameStateManager.cs | `GameStateManager` | Game state management |
| GenericIterator.cs | `GenericIterator` | Generic iterator pattern |

**PlayerStats Structure:**
```csharp
// Base stats (flat values from Equipment)
int BaseAttack, BaseDefense, BaseHealth, BaseMana, BaseSpeed
float BaseCritRate, BaseCritDamage

// Multipliers (% from Formation + Pet)
float AttackMultiplier = 1f      // 1.0 = 100%
float DefenseMultiplier = 1f
float HealthMultiplier = 1f
float SpeedMultiplier = 1f
float CritRateMultiplier = 1f

// Final = Base * Multiplier
```

---

### 3. Equipment System (`Equipment/`)

| File | Class | Description |
|------|-------|-------------|
| EquipmentController.cs | `EquipmentController` | Manages equipment, sets, enhancement |
| EquipmentItem.cs | `EquipmentItem` | Individual item with stats, rarity, enhancement |
| EquipmentSet.cs | `EquipmentSet` | Set bonuses (2/4/6 pieces) |
| EquipmentIteratorData.cs | `EquipmentIteratorData` | Collection data with iterator pattern |
| EquipmentSystemExample.cs | `EquipmentSystemExample` | Demo/Example |

**Equipment Enums:**
- `EquipmentSlot`: Weapon, Helmet, Armor, Ring, Necklace
- `EquipmentRarity`: Common, Uncommon, Rare, Epic, Legendary

**Features:**
- Iterator pattern for navigation (Next/Previous/First/Last)
- Set bonuses: 2-piece, 4-piece, 6-piece effects
- Enhancement system (+1 to +10)
- Power score calculation

---

### 4. Formation System (`Formation/`)

| File | Class | Description |
|------|-------|-------------|
| FormationController.cs | `FormationController` | Formation management, unit placement |
| FormationType.cs | `FormationType` | Formation definition with slots |
| FormationSlot.cs | `FormationSlot` | Individual slot with position bonuses |
| FormationExample.cs | - | Demo |
| FormationVisualDemo.cs | - | Visual demo |
| FormationDemoSceneBuilder.cs | - | Scene builder demo |

**Formation Layouts:**
- `Standard_3x3` - Balanced 3x3 grid
- `Offensive_2x4` - More backline
- `Defensive_4x2` - Strong frontline
- `Balanced_2x3` - Mix offense/defense
- `VFormation` - V-shaped tactical

**Slot Bonuses (per position):**
```csharp
float AttackBonus;      // e.g., 0.1f = +10%
float DefenseBonus;     // e.g., 0.2f = +20%
float SpeedBonus;
float CritBonus;
```

---

### 5. Inventory System (`Inventory/`)

| File | Class | Description |
|------|-------|-------------|
| InventoryManager.cs | `InventoryManager` | Main inventory controller |
| InventoryIteratorController.cs | `InventoryIteratorController` | Inventory CRUD, navigation |
| Item.cs | `Item` | Individual item with type, rarity, quantity |
| InventoryIteratorData.cs | `InventoryIteratorData` | Collection with iterator |
| InventoryExample.cs | - | Demo |

**Item Enums:**
- `ItemType`: Weapon, Armor, Consumable, Material, Quest
- `ItemRarity`: Common, Uncommon, Rare, Epic, Legendary

**Features:**
- Stacking (same item ID)
- Navigation: Next/Previous/First/Last
- Filter: NextOfType(), NextConsumable()
- Sort: ByName, ByType, ByRarity, ByValue

---

### 6. Pet System (`Pet/`)

| File | Class | Description |
|------|-------|-------------|
| PetManager.cs | `PetManager` | Pet lifecycle: summon, evolve, active |
| PetData.cs | `PetData` | Pet template from database |
| PetInstance.cs | `PetInstance` | Individual pet with level, exp |
| PetDatabase.cs | `PetDatabase` | Pet data collection |
| PetFollowAI.cs | `PetFollowAI` | Follow behavior |

**Pet Features:**
- Summon from database
- Level up with EXP
- Evolution system (requires materials + gold)
- Active pet buffs → PlayerStats
- Persistence via PlayerPrefs

**PetStats (buffs to player):**
```csharp
int atkBonus;    // % bonus
int hpBonus;     // % bonus
```

---

### 7. Skill System (`Skill/`)

| File | Class | Description |
|------|-------|-------------|
| SkillData.cs | `SkillData` | Skill definition: damage, cooldown, mana cost |
| SkillController.cs | `SkillController` | Skill management, casting |
| SkillIteratorData.cs | `SkillIteratorData` | Skill collection |
| AutoCastSkillController.cs | `AutoCastSkillController` | Auto-cast skill logic |
| SkillSystemExample.cs | - | Demo |

**Skill Enums:**
- `SkillCategory`: Active, Passive, Ultimate, Buff, Debuff, Healing
- `SkillElement`: Physical, Fire, Ice, Lightning, Earth, Wind, Holy, Dark
- `SkillEffectType`: Stun, Slow, Burn, Freeze, Poison, Heal, Shield...

**Skill Properties:**
```csharp
int ManaCost;
float BaseCooldown;
float BaseDamage;
float DamagePerLevel;  // scales with level
int MaxTargets;
SkillEffectType EffectType;
float EffectDuration;
```

---

### 8. Time System (`Time/`)

| File | Class | Description |
|------|-------|-------------|
| TimeManager.cs | `TimeManager` | Singleton: cooldowns, timers, schedules |
| TimerData.cs | `TimerData` | Timer with progress, callbacks |
| CooldownData.cs | `CooldownData` | Cooldown tracking |
| ScheduledEvent.cs | `ScheduledEvent` | Daily/weekly/hourly events |

**TimeManager APIs:**
```csharp
// Cooldowns
StartCooldown(string id, float duration)
CooldownReady(string id) → bool
CooldownRemaining(string id) → float

// Timers
CreateTimer(id, duration, repeat, countUp, onComplete, onTick)
StartTimer(id)
TimerProgress01(id) → float

// Scheduled Events
ScheduleDaily(id, name, hour, minute, callback)
ScheduleWeekly(id, name, day, hour, minute, callback)
ScheduleHourly(id, name, minute, callback)

// Control
SetPaused(bool)
SetTimeScale(float)  // 0-10x speed

// Offline
GetOfflineSpan() → TimeSpan
```

---

### 9. Demo (`Demo/`)

| File | Class | Description |
|------|-------|-------------|
| DemoAutoSetup.cs | `DemoAutoSetup` | Auto setup demo scene |

---

### 10. Tool (`Tool/`)

| File | Class | Description |
|------|-------|-------------|
| SpineFindAndSetup.cs | `SpineFindAndSetup` | Editor tool: auto-find Spine assets and setup |

---

## Integration Example

```csharp
// 1. Setup PlayerStats from all sources
var calculator = PlayerStatsCalculator.Instance;
var stats = calculator.CalculateTotalStats();

// 2. Apply to BattleUnit
foreach (var unit in playerUnits)
{
    unit.ApplyPlayerStats(stats);
}

// 3. Start battle
var battle = FindObjectOfType<AutoBattleController>();
battle.StartBattle();
```

---

## Key Patterns

1. **Iterator Pattern**: Equipment, Inventory, Skill dùng iterator để navigate collection
2. **Singleton**: TimeManager, PlayerStatsCalculator, CharacterManager
3. **Event System**: Actions for UI callbacks (OnItemEquipped, OnTurnEnded...)
4. **Data-Driven**: Items, Skills, Pets từ database/config (ScriptableObject)

---

## Related Documentation

- [PROJECT_OVERVIEW.md](./PROJECT_OVERVIEW.md) - Project overview

