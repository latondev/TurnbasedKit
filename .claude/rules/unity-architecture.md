# Unity Architecture

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
