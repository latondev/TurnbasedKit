# CLAUDE.md - Project Standards

## Overview
TurnbasedKit là Unity game project với turn-based combat, sử dụng C# và Spine 2D animations.

---

## Code Conventions

### Naming
- **Classes/Types**: PascalCase (`AutoBattleController`, `BattleUnit`)
- **Methods**: PascalCase (`StartBattle`, `CalculateTotalStats`)
- **Properties**: PascalCase (`CurrentHP`, `FinalAttack`)
- **Variables/Fields**: camelCase (`playerUnits`, `currentTurn`)
- **Constants**: PascalCase (`MaxTurns`, `DefaultManaCost`)
- **Enums**: PascalCase (`BattleState`, `ActionType`)

### Scripts Naming
| Hậu tố | Ý nghĩa | Ví dụ |
|---------|----------|--------|
| `XxxPanel` | UI Panel | `MenuPanel`, `BattlePanel` |
| `XxxSlot` | UI Slot | `InventorySlot`, `FormationSlot` |
| `XxxButton` | UI Button | `StartGameButton`, `UpgradeButton` |
| `XxxManager` | Master script (DUY NHẤT 1 instance) | `DailyMissionManager`, `BattleManager` |
| `XxxController` | Script điều khiển object (nhiều instance) | `PlayerController`, `EnemyController` |
| `XxxDatabase` | Database (CSV/data rows) | `WeaponDatabase`, `SkillDatabase` |
| `XxxData` | Data row trong database | `WeaponData`, `SkillData` |
| `XxxItem` | In-game item instance | `EquipmentItem`, `CardItem` |
| `XxxGenerator` | Script instantiate GameObjects | `EnemyGenerator`, `ItemGenerator` |
| `XxxSettings` | ScriptableObject settings | `GameSettings`, `BattleSettings` |
| `XxxEditor` | Editor-only scripts | `BattleSceneSetup` |

### File Naming
- **Folders**: PascalCase (`FolderName/`)
- **Images**: hyphen naming (`image-name-64x64.jpg`)
- **Scripts**: PascalCase (`ScriptName.cs`)
- **Prefabs**: PascalCase (`PrefabName.prefab`)
- **Scenes**: PascalCase (`SceneName.unity`)

### Member Properties
```csharp
// Không có input → dùng property (getter only)
public bool IsAlive => currentHP > 0;
// hoặc
public bool IsRewarded { get { return someCondition; } }

// Set trong, get ngoài
public int Id { get; private set; }
public Sprite IconSprite { get; private set; }
```

### No Hardcoded Values
❌ KHÔNG ĐƯỢC:
```csharp
int a = 5;
if (damage > 100) { ... }
```

✅ PHẢI DÙNG:
```csharp
const int DefaultValue = 5;
const int MaxDamageThreshold = 100;

int a = DefaultValue;
if (damage > MaxDamageThreshold) { ... }
```

**Rules:**
- Tất cả magic numbers phải khai báo là `const`
- Dùng tên có ý nghĩa thay vì số
- Nếu dùng nhiều lần → khai báo constant

### Namespaces
```csharp
namespace GameSystems.AutoBattle     // Battle system
namespace GameSystems.Common          // Shared utilities
namespace GameSystems.Equipment      // Equipment system
namespace GameSystems.Formation      // Formation system
namespace GameSystems.Pet            // Pet system
namespace GameSystems.Skills         // Skill system
```

### File Structure
```csharp
using System;
using UnityEngine;
using GameSystems.Skills;

namespace GameSystems.X
{
    public class MyClass : MonoBehaviour
    {
        // 1. SerializeFields
        [SerializeField] private int myField;

        // 2. Public properties
        public int MyProperty { get; private set; }

        // 3. Events
        public event Action<int> OnSomethingChanged;

        // 4. Unity methods
        void Start() { }
        void Update() { }

        // 5. Public methods
        public void DoSomething() { }

        // 6. Private methods
        private void HelperMethod() { }
    }
}
```

---

## Architecture Patterns

### 1. Iterator Pattern
Dùng cho collection navigation:
```csharp
public Item Next();
public Item Previous();
public Item First();
public Item Last();
```

### 2. Singleton Pattern
```csharp
public static MyClass Instance { get; private set; }

void Awake()
{
    if (Instance == null) Instance = this;
}
```

### 3. Event System
```csharp
public event Action<SomeType> OnEventName;
OnEventName?.Invoke(data);
```

### 4. Data-Driven
- Items, Skills, Pets từ database/config
- Không hardcode game data trong code

---

## Project Systems

### Battle System
- `AutoBattleController`: Main battle loop, turn management
- `BattleUnit`: Unit data (HP, ATK, DEF, SPD, skills, mana)
- `BattleAction`: Action representation

### Stats System
- `PlayerStats`: Base stats + Multipliers
- `PlayerStatsCalculator`: Aggregate from Equipment + Formation + Pet

### Equipment
- `EquipmentController`: Manages items, sets, enhancement
- `EquipmentItem`: Individual item with stats, rarity
- `EquipmentSet`: Set bonuses (2/4/6 pieces)

### Formation
- `FormationController`: Formation management
- `FormationSlot`: Position bonuses (ATK%, DEF%, SPD%)

### Pet
- `PetManager`: Summon, evolve, active pet
- `PetStats`: Buffs to player (atkBonus%, hpBonus%)

### Skill
- `SkillData`: Skill definition (damage, cooldown, mana cost)
- `SkillCategory`: Active, Passive, Ultimate, Buff, Debuff, Healing

### Time
- `TimeManager`: Singleton - cooldowns, timers, scheduled events

---

## Key Enums

```csharp
// Battle
BattleState { Idle, InProgress, Paused, Ended }
BattleOutcome { Victory, Defeat, Draw }
UnitType { Player, Enemy, Boss, Ally }
AttackRange { Melee, Ranged }
ActionType { Attack, Skill, Heal, Defend }

// Equipment
EquipmentSlot { Weapon, Helmet, Armor, Ring, Necklace }
EquipmentRarity { Common, Uncommon, Rare, Epic, Legendary }

// Item
ItemType { Weapon, Armor, Consumable, Material, Quest }
ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

// Skill
SkillCategory { Active, Passive, Ultimate, Buff, Debuff, Healing }
SkillElement { Physical, Fire, Ice, Lightning, Earth, Wind, Holy, Dark }
```

---

## Tips

1. **Luôn dùng `var` khi type rõ ràng**: `var unit = new BattleUnit(...)`
2. **Dùng `System.Action` thay vì delegate tự tạo**
3. **Debug với màu**: `Debug.Log($"<color=green>Message</color>")`
4. **SerializeField cho private fields cần show trong Inspector**
5. **Dùng `?.Invoke()` cho events để tránh null reference**

---

## Related Docs
- [docs/SCRIPTS_ARCHITECTURE.md](docs/SCRIPTS_ARCHITECTURE.md) - Detailed system docs
- [docs/PROJECT_OVERVIEW.md](docs/PROJECT_OVERVIEW.md) - Project overview
