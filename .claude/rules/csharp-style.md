# C# Style Guide

## Naming Conventions

| Loại | Quy tắc | Ví dụ |
|------|---------|-------|
| Classes/Types | PascalCase | `AutoBattleController`, `BattleUnit` |
| Methods | PascalCase | `StartBattle`, `CalculateTotalStats` |
| Properties | PascalCase | `CurrentHP`, `FinalAttack` |
| Variables/Fields (private) | _camelCase | `_playerUnits`, `_currentTurn` |
| Variables/Fields (public) | camelCase | `playerUnits`, `currentTurn` |
| Constants | PascalCase | `MaxTurns`, `DefaultManaCost` |
| Enums | PascalCase | `BattleState`, `ActionType` |

## Member Properties

```csharp
// Không có input → dùng property (getter only)
public bool IsAlive => currentHP > 0;
// hoặc
public bool IsRewarded { get { return someCondition; } }

// Set trong, get ngoài
public int Id { get; private set; }
public Sprite IconSprite { get; private set; }
```

## No Hardcoded Values

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

## Var Usage

- Luôn dùng `var` khi type rõ ràng: `var unit = new BattleUnit(...)`

## Namespaces

```csharp
namespace GameSystems.AutoBattle     // Battle system
namespace GameSystems.Common          // Shared utilities
namespace GameSystems.Equipment      // Equipment system
namespace GameSystems.Formation      // Formation system
namespace GameSystems.Pet            // Pet system
namespace GameSystems.Skills         // Skill system
```

## File Structure

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

## Tips

- Dùng `System.Action` thay vì delegate tự tạo
- Debug với màu: `Debug.Log($"<color=green>Message</color>")`
- SerializeField cho private fields cần show trong Inspector
- Dùng `?.Invoke()` cho events để tránh null reference
