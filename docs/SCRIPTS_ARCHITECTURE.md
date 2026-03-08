# Scripts Architecture Documentation

## Overview
TurnbasedKit là một Unity game project với các hệ thống game mechanics được xây dựng theo modular design patterns.

---

## 📁 Folder Structure

```
Assets/Scripts/
├── Battle/          # Battle system (turn-based combat)
├── Common/          # Shared utilities (PlayerStats, Calculator)
├── Equipment/       # Equipment system (items, sets, bonuses)
├── Formation/       # Formation/team positioning system
├── Inventory/       # Item inventory management
├── Pet/             # Pet companion system
├── Skill/           # Skill system (active, passive, ultimate)
├── Time/            # Time management (cooldowns, timers, events)
└── Tool/            # Editor tools (Spine setup)
```

---

## 🔄 Data Flow

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
    Battle - AutoBattleController
```

---

## 📦 Core Systems

### 1. Battle System (`Battle/`)

| File | Class | Description |
|------|-------|-------------|
| [AutoBattleController.cs](Battle/AutoBattleController.cs) | `AutoBattleController` | Main battle loop, turn management, AI decisions |
| [BattleUnit.cs](Battle/BattleUnit.cs) | `BattleUnit` | Individual unit data: HP, ATK, DEF, SPD, skills, mana |
| [BattleAction.cs](Battle/BattleAction.cs) | `BattleAction`, `BattleTurn` | Action representation, action history |

**Key Enums:**
- `BattleState`: Idle → InProgress → Ended
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
| [PlayerStats.cs](Common/PlayerStats.cs) | `PlayerStats` | Unified stats: Base + Multipliers |
| [PlayerStatsCalculator.cs](Common/PlayerStatsCalculator.cs) | `PlayerStatsCalculator` | Aggregates stats from Equipment + Formation + Pet |

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
| [EquipmentController.cs](Equipment/EquipmentController.cs) | `EquipmentController` | Manages equipment, sets, enhancement |
| [EquipmentItem.cs](Equipment/EquipmentItem.cs) | `EquipmentItem` | Individual item with stats, rarity, enhancement |
| [EquipmentSet.cs](Equipment/EquipmentSet.cs) | `EquipmentSet` | Set bonuses (2/4/6 pieces) |
| [EquipmentIteratorData.cs](Equipment/EquipmentIteratorData.cs) | `EquipmentIteratorData` | Collection data with iterator pattern |
| [EquipmentSystemExample.cs](Equipment/EquipmentSystemExample.cs) | `EquipmentSystemExample` | Demo/Example |

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
| [FormationController.cs](Formation/FormationController.cs) | `FormationController` | Formation management, unit placement |
| [FormationType.cs](Formation/FormationType.cs) | `FormationType` | Formation definition with slots |
| [FormationSlot.cs](Formation/FormationSlot.cs) | `FormationSlot` | Individual slot with position bonuses |
| [FormationExample.cs](Formation/FormationExample.cs) | - | Demo |

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
| [InventoryIteratorController.cs](Inventory/InventoryIteratorController.cs) | `InventoryIteratorController` | Inventory CRUD, navigation |
| [Item.cs](Inventory/Item.cs) | `Item` | Individual item with type, rarity, quantity |
| [InventoryIteratorData.cs](Inventory/InventoryIteratorData.cs) | `InventoryIteratorData` | Collection with iterator |
| [InventoryExample.cs](Inventory/InventoryExample.cs) | - | Demo |

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
| [PetManager.cs](Pet/PetManager.cs) | `PetManager` | Pet lifecycle: summon, evolve, active |
| [PetData.cs](Pet/PetData.cs) | `PetData` | Pet template from database |
| [PetInstance.cs](Pet/PetInstance.cs) | `PetInstance` | Individual pet with level, exp |
| [PetDatabase.cs](Pet/PetDatabase.cs) | `PetDatabase` | Pet data collection |
| [PetFollowAI.cs](Pet/PetFollowAI.cs) | `PetFollowAI` | Follow behavior |

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
| [SkillData.cs](Skill/SkillData.cs) | `SkillData` | Skill definition: damage, cooldown, mana cost |
| [SkillController.cs](Skill/SkillController.cs) | `SkillController` | Skill management, casting |
| [SkillIteratorData.cs](Skill/SkillIteratorData.cs) | `SkillIteratorData` | Skill collection |
| [SkillSystemExample.cs](Skill/SkillSystemExample.cs) | - | Demo |

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
| [TimeManager.cs](Time/TimeManager.cs) | `TimeManager` | Singleton: cooldowns, timers, schedules |
| [TimerData.cs](Time/TimerData.cs) | `TimerData` | Timer with progress, callbacks |
| [CooldownData.cs](Time/CooldownData.cs) | `CooldownData` | Cooldown tracking |
| [ScheduledEvent.cs](Time/ScheduledEvent.cs) | `ScheduledEvent` | Daily/weekly/hourly events |

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

### 9. Tool (`Tool/`)

| File | Class | Description |
|------|-------|-------------|
| [SpineFindAndSetup.cs](Tool/SpineFindAndSetup.cs) | `SpineFindAndSetup` | Editor tool: auto-find Spine assets and setup |

---

## 🎯 Integration Example

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

## 📝 Key Patterns

1. **Iterator Pattern**: Equipment, Inventory, Skill dùng iterator để navigate collection
2. **Singleton**: TimeManager, PlayerStatsCalculator
3. **Event System**: Actions for UI callbacks (OnItemEquipped, OnTurnEnded...)
4. **Data-Driven**: Items, Skills, Pets từ database/config

---

## 🔗 Related Documentation

- [PROJECT_OVERVIEW.md](../docs/PROJECT_OVERVIEW.md) - Project overview
- [PROJECT_STRUCTURE.md](../memory/PROJECT_STRUCTURE.md) - Full project structure
