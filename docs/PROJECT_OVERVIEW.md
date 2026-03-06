# Project Overview: TurnbasedKit

## Basic Information

| Property | Value |
|----------|-------|
| **Project Name** | TurnbasedKit |
| **Unity Version** | 6000.3.6f1 |
| **Platform** | Android |
| **Project Root** | F:/Latondev-git/TurnbasedKit |
| **Assets Path** | F:/Latondev-git/TurnbasedKit/Assets |

## Project Structure (Updated 2026-03-06)

```
Assets/
├── Scripts/                      # Game scripts (NEW)
│   └── Tool/
│       └── SpineFindAndSetup.cs
│
├── AssetGame/                    # Game assets
│   ├── AnimationClip/            # Animation clips
│   ├── AnimatorController/       # Animation controllers
│   ├── ArtWork/                 # Artwork assets
│   ├── Editor/                  # Editor scripts
│   ├── Emoji/                   # Emoji assets
│   ├── Font/                    # Font files
│   ├── Material/                # Materials
│   ├── Mesh/                    # 3D models
│   ├── Resources/               # Runtime loadable resources
│   ├── Scenes/                  # Game scenes
│   ├── SerializedCollections/   # Dictionary serialization lib
│   ├── Shader/                  # Custom shaders
│   ├── TextAsset/               # Spine Atlas & Materials
│   └── Texture2D/               # 2D textures
│
├── Plugins/                      # Third-party plugins
│   ├── Sirenix/                 # Odin Inspector
│   ├── Spine/                   # Spine runtime
│   ├── Spine Examples/          # Spine examples
│   └── Voxel Labs/              # Voxel graphics lib
│
├── SpineData/                    # Spine data
│   ├── Battle/                  # Battle characters (45+ characters)
│   │   ├── bingyi
│   │   ├── caoyao
│   │   ├── chuchu
│   │   ├── daobatu
│   │   ├── donghuangtaiyi
│   │   ├── goumang
│   │   ├── guotaimingan
│   │   ├── hexiangu
│   │   ├── huahaoyueyuan
│   │   ├── huayao
│   │   ├── huodongxingjun
│   │   ├── hupo
│   │   ├── jianghuke
│   │   ├── jingwei
│   │   ├── leizhenzi
│   │   ├── mojianshi
│   │   ├── pixiu
│   │   ├── qihun
│   │   ├── shuyao
│   │   ├── taotie
│   │   ├── tianbing
│   │   ├── tianjiang
│   │   ├── tianlangyao
│   │   ├── xiaohundun
│   │   ├── xingtian
│   │   ├── yangjian
│   │   ├── yaozhu
│   │   ├── yinglong
│   │   ├── zhangmazi
│   │   ├── zhangzhongxian
│   │   ├── zhujue_feiyu        # Main character: Feiyu
│   │   ├── zhujue_jianling     # Main character: Jianling
│   │   └── zhujue_wusheng     # Main character: Wusheng
│   │
│   └── Other/                   # Other Spine data
│
├── Scenes/                       # Unity scenes
│   └── SampleScene.unity
│
└── InputSystem_Actions.inputactions  # Unity Input System
```

## Key Changes from Previous Structure

1. **Scripts folder** - All C# scripts now in `Assets/Scripts/`
2. **Plugins folder** - Third-party plugins consolidated in `Assets/Plugins/`
3. **Simplified root** - Removed SkeletonAsset, keeping only SpineData

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
2. **GameMain.unity** - Main game scene (in `Assets/AssetGame/Scenes/`)

## Key Scripts

| File | Description |
|------|-------------|
| `Scripts/Tool/SpineFindAndSetup.cs` | Main script for finding and setting up Spine assets |
| `AssetGame/Editor/SpineDataOrganizer.cs` | Editor script for organizing Spine data |

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

- This is a **turn-based game** project using Spine for 2D animations
- Uses **Chinese naming convention** for character assets
- **45+ battle characters** in SpineData/Battle/
- **3 main characters**: Feiyu (feiyu), Jianling (jianling), Wusheng (wusheng)
- Target platform: **Android**
- Uses modern **Input System** (InputSystem_Actions.inputactions)
