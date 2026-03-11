# UI Naming Conventions

## Scripts Naming

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

## File Naming

| Loại | Quy tắc | Ví dụ |
|------|---------|-------|
| Folders | PascalCase | `FolderName/` |
| Images | hyphen naming | `image-name-64x64.jpg` |
| Scripts | PascalCase | `ScriptName.cs` |
| Prefabs | PascalCase | `PrefabName.prefab` |
| Scenes | PascalCase | `SceneName.unity` |
