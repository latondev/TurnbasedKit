# CLAUDE.md - Project Standards

## Overview
Luôn trả lời bằng tiếng việt
TurnbasedKit là Unity game project với turn-based combat, sử dụng C# và Spine 2D animations.

---

## Code Conventions

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

Xem chi tiết tại: [.claude/rules/csharp-style.md](.claude/rules/csharp-style.md)

---

## Architecture Patterns

Xem chi tiết tại: [.claude/rules/unity-architecture.md](.claude/rules/unity-architecture.md)

---

## Project Systems

Xem chi tiết tại: [.claude/rules/unity-architecture.md](.claude/rules/unity-architecture.md)

---

## Key Enums

Xem chi tiết tại: [.claude/rules/enums.md](.claude/rules/enums.md)

---


## Related Docs
- [docs/SCRIPTS_ARCHITECTURE.md](docs/SCRIPTS_ARCHITECTURE.md) - Detailed system docs
- [docs/PROJECT_OVERVIEW.md](docs/PROJECT_OVERVIEW.md) - Project overview
- [.claude/rules/csharp-style.md](.claude/rules/csharp-style.md) - C# style guide
- [.claude/rules/ui-naming.md](.claude/rules/ui-naming.md) - UI naming conventions
- [.claude/rules/unity-architecture.md](.claude/rules/unity-architecture.md) - Unity architecture
- [.claude/rules/enums.md](.claude/rules/enums.md) - Standard enums
