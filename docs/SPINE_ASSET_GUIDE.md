# Spine Asset Documentation

## Overview

Project sử dụng Spine runtime cho 2D skeletal animation. Có 2 loại Spine asset chính:

1. **Battle Assets** - Dùng cho scene chiến đấu
2. **Avatar/Other Assets** - Dùng cho UI/Avatar

---

## 1. Battle Assets (`SpineData/Battle/`)

### Đặc điểm

| Property | Value |
|----------|-------|
| **Thư mục** | `Assets/SpineData/Battle/{character_name}/` |
| **Sử dụng** | Battle scene, world space |
| **Spine Component** | `SkeletonAnimation` |
| **Shader** | `Spine/SkeletonLit` (không có Screen blend mode) |
| **Scale** | 0.01 |
| **Prefab Location** | `Assets/AssetGame/ArtWork/Prefab/Role/{name}.prefab` |

### Cấu trúc thư mục

```
SpineData/Battle/yangjian/
├── yangjian.atlas.txt          # Atlas definition
├── yangjian.png                 # Texture atlas
├── yangjian.skel.bytes          # Skeleton binary data
├── yangjian_Atlas.asset         # Unity Atlas asset
├── yangjian_Material.mat         # Material (Spine/SkeletonLit)
└── yangjian_SkeletonData.asset  # SkeletonData asset
```

### SkeletonData Config

```yaml
atlasAssets: [yangjian_Atlas.asset]
scale: 0.01
skeletonJSON: yangjian.skel.bytes
blendModeMaterials:
  requiresBlendModeMaterials: 0   # Không có screen blend mode
  screenMaterials: []            # Không có screen material
```

### Prefab Structure

```
yang_jian (Root - Transform)
├── model
│   ├── MeshFilter
│   ├── MeshRenderer
│   ├── SkeletonAnimation      # guid: fb435fa8fda8934bce430ed7f6d34587
│   └── [Custom Spine Event]
├── UIPos                      # Vị trí HP bar, name
├── BuffTop                    # Buff phía trên
├── BuffMiddle                 # Buff giữa
├── BuffBottom                 # Buff dưới
├── FlyStart                   # Vị trí effect bay
└── PetPos                     # Vị trí pet
```

- **Model Scale**: 0.63
- **Layer**: 0 (Default)

---

## 2. Avatar Assets (`SpineData/Other/avator_*`)

### Đặc điểm

| Property | Value |
|----------|-------|
| **Thư mục** | `Assets/SpineData/Other/avator_{name}/` |
| **Sử dụng** | UI Avatar, selection screen |
| **Spine Component** | `SkeletonGraphic` |
| **Shader** | `Spine/SkeletonGraphic` (Screen blend mode) |
| **Scale** | 0.01 |
| **Prefab Location** | `Assets/AssetGame/ArtWork/Prefab/Role/avator_{name}.prefab` |

### Cấu trúc thư mục

```
SpineData/Other/avator_nvwa/
├── avator_nvwa.atlas.txt              # Atlas definition
├── avator_nvwa.png                     # Texture atlas
├── avator_nvwa.skel.bytes              # Skeleton binary data
├── avator_nvwa_Atlas.asset             # Unity Atlas asset
├── avator_nvwa_Material.mat            # Material (Spine/SkeletonLit)
├── avator_nvwa_Material-Screen.mat     # Material with Screen blend mode
└── avator_nvwa_SkeletonData.asset      # SkeletonData asset
```

### SkeletonData Config

```yaml
atlasAssets: [avator_nvwa_Atlas.asset]
scale: 0.01
skeletonJSON: avator_nvwa.skel.bytes
blendModeMaterials:
  requiresBlendModeMaterials: 1   # Có screen blend mode
  screenMaterials:
  - pageName: avator_nvwa.png
    material: avator_nvwa_Material-Screen.mat
```

### Prefab Structure

```
avator_nv_wa (Root - RectTransform)
├── model
│   ├── RectTransform
│   ├── CanvasRenderer
│   ├── SkeletonGraphic          # guid: fe2f04b5e09bd99e8a0db5c8f3eded21
│   └── [Custom Spine Event]
├── UIPos                        # Vị trí HP bar
├── BuffTop                      # Buff phía trên
├── BuffMiddle                   # Buff giữa
├── BuffBottom                   # Buff dưới
└── PetPos                       # Vị trí pet
```

- **Model Scale**: 0.75764
- **Layer**: 9 (UI Layer)
- **Anchor**: Center (0.5, 0.5)

---

## So sánh Battle vs Avatar

| Feature | Battle | Avatar |
|---------|--------|--------|
| **Root Type** | Transform | RectTransform |
| **Spine Component** | SkeletonAnimation | SkeletonGraphic |
| **Renderer** | MeshRenderer | CanvasRenderer |
| **Shader** | SkeletonLit | SkeletonGraphic |
| **Screen Blend Mode** | ❌ Không | ✅ Có |
| **Layer** | 0 (Default) | 9 (UI) |
| **Model Scale** | 0.63 | 0.75764 |

---

## Danh sách Character (Battle)

### Main Characters (3)
- `zhujue_fei_yu` - Feiyu (nhân vật chính)
- `zhujue_jian_ling` - Jianling (nhân vật chính)
- `zhujue_wu_sheng` - Wusheng (nhân vật chính)

### Battle Units (45+)
```
bingyi, caoyao, chuchu, daobatu, donghuangtaiyi,
feiyu, goumang, huayao, hupo, jianghuke, jianling,
jingwei, leizhenzi, mojianshi, pixiu, qihun, shuyao,
taotie, tianbing, tianjiang, tianlangyao, wusheng,
xiaohundun, xingtian, yinglong, zhangmazi, zhangzhongxian,
yangjian, leizhenzi_shouling, pixiulongzi, yangjian_shouling...
```

---

## Danh sách Avatar (Other - avator_*)

```
avator_chuchu, avator_daobatu, avator_donghuangtaiyi,
avator_feiyu, avator_jianling, avator_jingwei, avator_leizhenzi,
avator_mudanxianzi, avator_niexiaoqian, avator_nvwa, avator_pi_xiu,
avator_qinglong, avator_wusheng, avator_yangjian, avator_yinglong,
avator_zhangmazi
```

---

## Cách sử dụng trong code

### Load Battle Unit

```csharp
// Load từ prefab
var prefab = Resources.Load<GameObject>("Prefab/Role/yang_jian");
var unit = Instantiate(prefab, battlePosition, Quaternion.identity);
var skeletonAnimation = unit.GetComponent<SkeletonAnimation>();
skeletonAnimation.AnimationState.SetAnimation(0, "idle", true);
```

### Load Avatar

```csharp
// Load từ prefab (UI)
var prefab = Resources.Load<GameObject>("Prefab/Role/avator_nv_wa");
var avatar = Instantiate(prefab, uiParent);
var skeletonGraphic = avatar.GetComponent<SkeletonGraphic>();
skeletonGraphic.AnimationState.SetAnimation(0, "idle", true);
```

### Load Spine Data trực tiếp

```csharp
// Battle
var skeletonData = Resources.Load<SkeletonData>("SpineData/Battle/yangjian/yangjian_SkeletonData");

// Avatar
var avatarData = Resources.Load<SkeletonData>("SpineData/Other/avator_nvwa/avator_nvwa_SkeletonData");
```

---

## Ghi chú quan trọng

1. **Prefab Battle**: Không có suffix, dùng `SkeletonAnimation`
2. **Prefab Avatar**: Có prefix `avator_`, dùng `SkeletonGraphic`
3. **Material**: Avatar có 2 material - thường và Screen blend mode
4. **Animation**: Cả 2 đều có thể dùng chung animation name (idle, attack, skill...)
5. **Scale**: Battle dùng scale 0.63, Avatar dùng 0.75764 trong RectTransform

---

## Các folder khác trong SpineData/Other

Ngoài avator, còn có các character khác:

```
caishenyelao, caishenyelao2, caishenyenv, caishenyexiao,
caishenyezhong1, caishenyezhong2, en_caishenyelao, en_fengshou,
en_gongyin, en_guotaimingan, en_jiahaoyou, en_jingweijingwei,
en_laihetangba, en_maomao, en_mowojiugeinibaobei, en_qiutiandediyibeizhongyao,
en_songniyiduoxiaohonghua, en_suixinsuixing, fengshou, fulixingjun,
gongyin, jiahaoyou, jingweijingwei, keai, laihetangba, lengjing,
longtaitou, maigeleileizhanimen, maomao, mowojiugeinibaobei, mubu,
penglaijie, pixiulongzi, qiutiandediyibeizhongyao, shancaitongzi,
shenmishangren, songniyiduoxiaohonghua, suixinsuixing
```

> **Note**: Các character này có thể là NPC, enemy, hoặc decoration.
