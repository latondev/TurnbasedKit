# Project Overview: TurnbasedKit

## Basic Information

| Property | Value |
|----------|-------|
| **Project Name** | TurnbasedKit |
| **Unity Version** | 6000.3.6f1 |
| **Platform** | Android |
| **Project Root** | F:/Latondev-git/TurnbasedKit |
| **Assets Path** | F:/Latondev-git/TurnbasedKit/Assets |

## Project Structure (Updated 2026-02)

```
Assets/
├── Scripts/                      # Game scripts (10 folders)
│   ├── Battle/                   # Battle system
│   ├── Common/                   # Shared utilities
│   ├── Demo/                     # Demo scripts
│   ├── DesignPatterns/          # (Reserved for patterns)
│   ├── Equipment/                # Equipment system
│   ├── Formation/                # Formation system
│   ├── Inventory/                # Inventory system
│   ├── Pet/                      # Pet system
│   ├── Skill/                    # Skill system
│   ├── Time/                     # Time management
│   └── Tool/                     # Editor tools
│
├── AssetGame/                    # Game assets
│   ├── AnimationClip/            # Animation clips
│   ├── AnimatorController/       # Animation controllers
│   ├── ArtWork/                  # Artwork assets
│   ├── Editor/                   # Editor scripts
│   ├── Emoji/                    # Emoji assets
│   ├── Font/                     # Font files
│   ├── Material/                 # Materials
│   ├── Mesh/                     # 3D models
│   ├── Resources/                # Runtime loadable resources
│   ├── Scenes/                   # Game scenes
│   ├── SerializedCollections/    # Dictionary serialization lib
│   ├── Shader/                   # Custom shaders
│   ├── TextAsset/                # Spine Atlas & Materials
│   └── Texture2D/               # 2D textures
│
├── Plugins/                      # Third-party plugins
│   ├── Sirenix/                  # Odin Inspector
│   ├── Spine/                    # Spine runtime
│   ├── Spine Examples/           # Spine examples
│   ├── Voxel Labs/              # Voxel graphics lib
│   └── Roslyn/                  # Roslyn compiler
│
├── SkeletonAsset/                # Spine skeleton assets
│   ├── feiyu/                   # Main character: Feiyu
│   ├── jianling/                # Main character: Jianling
│   ├── wusheng/                 # Main character: Wusheng
│   ├── yangjian_0/               # Yangjian variant
│   ├── zhiyinshou/              # Character: Zhiyinshou
│   ├── zhiyinshou_0/            # Zhiyinshou variant
│   ├── maigeleileizhanimen/     # Enemy: Maigeleileizhanimen
│   ├── map_tree_1/               # Environment asset
│   ├── map_tree_1_0/            # Environment variant
│   ├── map2_tree_2/             # Environment asset
│   ├── map2_tree_2_0/           # Environment variant
│   └── skeleton/                 # Base skeleton
│
├── SpineData/                    # Spine data (reference)
│   ├── Battle/                  # Battle characters (45+ characters)
│   │   ├── bingyi, caoyao, chuchu, daobatu...
│   │   ├── zhujue_feiyu, zhujue_jianling, zhujue_wusheng
│   │   └── ...
│   └── Other/                    # Other Spine data
│
├── Scenes/                       # Unity scenes
│   ├── SampleScene.unity
│   └── GameDemo.unity
│
├── Editor/                       # Editor scripts
│   ├── BingYiPrefabModifier.cs
│   ├── DumpSpineInfo.cs
│   ├── ReplaceModelWithSpine.cs
│   └── UpdateSpinePrefabs.cs
│
└── InputSystem_Actions.inputactions  # Unity Input System
```

## Key Changes (2026)

1. **Scripts folder** - 10 subfolders with modular game systems
2. **SkeletonAsset folder** - Re-added for runtime Spine assets
3. **Demo folder** - DemoAutoSetup.cs for quick scene setup
4. **DesignPatterns folder** - Reserved for future patterns
5. **GameDemo.unity** - Main game scene in Assets/Scenes/

## Key Dependencies

| Package | Description |
|---------|-------------|
| **Spine** | 2D skeletal animation runtime |
| **Odin Inspector** | Enhanced Unity inspector (Sirenix) |
| **SerializedCollections** | Dictionary serialization support |
| **Unity Input System** | Modern input handling |
| **Voxel Labs** | Voxel graphics library |

## Main Scenes

1. **SampleScene.unity** - Default Unity scene (`Assets/Scenes/SampleScene.unity`)
2. **GameDemo.unity** - Main game scene (`Assets/Scenes/GameDemo.unity`)

## Naming Conventions (Updated)

| Type | Convention | Example |
|------|------------|---------|
| **Classes** | PascalCase | `AutoBattleController`, `BattleUnit` |
| **Private Fields** | `_camelCase` | `_playerUnits`, `_currentTurn` |
| **Public Fields** | `camelCase` | `playerUnits`, `currentTurn` |
| **Properties** | PascalCase | `CurrentHP`, `FinalAttack` |
| **Methods** | PascalCase | `StartBattle`, `CalculateTotalStats` |
| **Constants** | PascalCase | `MaxTurns`, `DefaultManaCost` |
| **Enums** | PascalCase | `BattleState`, `ActionType` |

## Namespaces

```csharp
GameSystems.AutoBattle    // Battle system
GameSystems.Battle        // Battle components
GameSystems.Common        // Shared utilities
GameSystems.Equipment     // Equipment system
GameSystems.Formation     // Formation system
GameSystems.Pet           // Pet system
GameSystems.Skills       // Skill system
GameSystems.Time         // Time management
```

## MCP Integration

This project is configured with Unity MCP for remote editor control:
- **MCP Server**: unityMCP
- **Endpoint**: http://localhost:8080/mcp

Use MCP tools to:
- Manage scenes and GameObjects
- Create/modify scripts
- Run tests
- Control Unity Editor

## Notes

- **Turn-based game** project using Spine for 2D animations
- **Chinese naming convention** for character assets
- **45+ battle characters** in SpineData/Battle/
- **3 main characters**: Feiyu, Jianling, Wusheng
- **Target platform**: **Android**
- Uses modern **Input System** (InputSystem_Actions.inputactions)
- **Variable naming**: Private fields use `_camelCase`, public fields use `camelCase`

