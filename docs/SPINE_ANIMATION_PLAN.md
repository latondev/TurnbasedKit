# Spine Animation & Event Plan

> File này mô tả chi tiết **tất cả animations và events** của từng Spine character trong folder `SpineData/Battle/`.
> Dùng làm tài liệu tham chiếu khi code Battle Controller, Event Handler, hoặc Effect System.

---

## Quy ước chung

### Prefix `war_`
- Mỗi animation chiến đấu đều có 2 phiên bản: **thường** (`idle`) và **chiến đấu** (`war_idle`).
- Phiên bản `war_` dùng khi nhân vật đang trong trận (có vũ khí, tư thế chiến đấu).
- Phiên bản không prefix dùng ngoài trận (lobby, sảnh, overworld).

### Prefix `zm_`
- Chỉ có ở 3 nhân vật chính (`zhujue_*`).
- Là các animation dùng cho chế độ **Trảm Ma** (Demon Slayer mode) — cutscene hoặc auto-battle đặc biệt.

### Prefix `ttzl_`
- Chỉ có ở 3 nhân vật chính (`zhujue_*`).
- Là các animation dùng cho chế độ **Thông Thiên Trận Liệt** (Tower Defense / special stage).

---

## Danh sách Events toàn hệ thống

| Event Name | Mô tả | Xuất hiện ở |
|---|---|---|
| `audio` | Phát sound effect tại thời điểm đó trong animation | Hầu hết battle characters |
| `hit` | Thời điểm đòn đánh thường trúng mục tiêu → trigger damage/effect | Hầu hết battle characters |
| `stuntHit` | Thời điểm **tuyệt kỹ (stunt/skill)** trúng mục tiêu → trigger damage/skill effect | Hầu hết battle characters |
| `skillHit` | Thời điểm **skill đặc biệt** trúng → trigger damage (riêng nhân vật chính) | `zhujue_feiyu`, `zhujue_jianling`, `zhujue_wusheng` |
| `effectSkillHit` | Thời điểm **effect skill** trúng → trigger VFX + damage (riêng nhân vật chính) | `zhujue_feiyu`, `zhujue_jianling` |
| `stuntBuff` | Thời điểm **tuyệt kỹ áp buff** lên bản thân hoặc đồng đội | `zhujue_wusheng` |
| `jump_end` | Animation nhảy kết thúc → chuyển state | `pixiu` (pet) |
| `paopao_end` | Animation bong bóng (paopao) kết thúc → chuyển state | `pixiu` (pet) |

---

## Danh sách Animations toàn hệ thống

### Animation cơ bản (Battle Characters)

| Animation | Looping? | Mô tả | Event trigger |
|---|---|---|---|
| `idle` | ✅ Loop | Đứng yên, trạng thái mặc định ngoài trận | — |
| `war_idle` | ✅ Loop | Đứng yên trong trận, tư thế chiến đấu | — |
| `move` | ✅ Loop | Di chuyển tiến về phía đối thủ | — |
| `war_move` | ✅ Loop | Di chuyển tiến trong trận | — |
| `moveBack` | ✅ Loop | Di chuyển lùi về vị trí ban đầu | — |
| `war_moveBack` | ✅ Loop | Di chuyển lùi trong trận | — |
| `attack` | ❌ Once | Đánh thường | `audio`, `hit` |
| `war_attack` | ❌ Once | Đánh thường trong trận | `audio`, `hit` |
| `backAttack` | ❌ Once | Đánh thường từ phía sau (quay lưng) | `audio`, `hit` |
| `war_backAttack` | ❌ Once | Đánh thường từ phía sau trong trận | `audio`, `hit` |
| `beAttack` | ❌ Once | Bị đánh trúng, phản ứng chịu sát thương | — |
| `war_beAttack` | ❌ Once | Bị đánh trúng trong trận | — |
| `stun` | ✅ Loop | Bị choáng, không thể hành động | — |
| `war_stun` | ✅ Loop | Bị choáng trong trận | — |
| `stunt` | ❌ Once | Tuyệt kỹ / Skill chính | `audio`, `stuntHit` |
| `war_stunt` | ❌ Once | Tuyệt kỹ trong trận | `audio`, `stuntHit` |
| `die` | ❌ Once | Chết, ngã xuống | — |
| `war_die` | ❌ Once | Chết trong trận | — |
| `win` | ❌ Once | Chiến thắng, ăn mừng | — |
| `war_win` | ❌ Once | Chiến thắng trong trận | — |

### Animation đặc biệt (chỉ một số character)

| Animation | Character | Mô tả | Event trigger |
|---|---|---|---|
| `special` | `tianjiang`, `xingtian` | Kỹ năng đặc biệt riêng nhân vật | `audio`, `stuntHit` |
| `war_special` | `tianjiang`, `xingtian` | Kỹ năng đặc biệt trong trận | `audio`, `stuntHit` |
| `attack2` | `jingwei`, `zhujue_feiyu`, `zhujue_jianling`, `zhujue_wusheng` | Đòn đánh thường combo 2 | `audio`, `hit` |
| `war_attack2` | (tương tự) | Combo 2 trong trận | `audio`, `hit` |
| `attack3` | `zhujue_jianling` | Đòn đánh thường combo 3 | `audio`, `hit` |
| `stunt2` | `yangjian` | Tuyệt kỹ 2 (biến thể) | `audio`, `stuntHit` |

---

## Chi tiết từng Character

### 🗡️ Battle Characters (Quái / Tướng thông thường)

> Tất cả character dưới đây đều có chung bộ animation cơ bản và 3 events: `audio`, `hit`, `stuntHit`.

#### bingyi (Băng Nghị)
- **Animations**: `idle`, `move`, `moveBack`, `attack`, `backAttack`, `beAttack`, `stun`, `stunt`, `die`, `win` + prefix `war_`
- **Events**: `audio`, `hit`, `stuntHit`

#### caoyao (Thảo Yêu)
- **Animations**: Giống bingyi (bộ cơ bản đầy đủ)
- **Events**: `audio`, `hit`, `stuntHit`

#### chuchu (Sở Sở)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### daobatu (Đao Bạt Đồ)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### donghuangtaiyi (Đông Hoàng Thái Ất)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### goumang (Câu Mang)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### hexiangu (Hà Tiên Cô)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### huayao (Hoa Yêu)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### hupo (Hồ Phách)
- **Animations**: `idle`, `beAttack`, `die`, `stun`, `win` + prefix `war_` (⚠️ **KHÔNG có** `attack`, `move`, `moveBack`, `stunt`)
- **Events**: *(Không có event)*
- **Ghi chú**: Đây là unit bị động / summon, không tự tấn công

#### jianghuke (Giang Hồ Khách)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### jingwei (Tinh Vệ)
- **Animations**: Giống bingyi + thêm `attack2`, `war_attack2` (combo đánh thường 2)
- **Events**: `audio`, `hit`, `stuntHit`

#### leizhenzi (Lôi Chấn Tử)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### mojianshi (Mạc Kiếm Sĩ)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### qihun (Khí Hồn)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### shuyao (Thụ Yêu)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### taotie (Thao Thiết)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### tianbing (Thiên Binh)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### tianjiang (Thiên Tướng)
- **Animations**: Giống bingyi + thêm `special`, `war_special` (kỹ năng đặc biệt)
- **Events**: `audio`, `hit`, `stuntHit`

#### tianlangyao (Thiên Lang Yêu)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### xiaohundun (Tiểu Hỗn Độn)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### xingtian (Hình Thiên)
- **Animations**: Giống bingyi + thêm `special`, `war_special`
- **Events**: `audio`, `hit`, `stuntHit`

#### yangjian (Dương Tiễn)
- **Animations**: Giống bingyi + thêm `stunt2`, `war_stunt2` (tuyệt kỹ biến thể 2)
- **Events**: `audio`, `hit`, `stuntHit`

#### yinglong (Ứng Long)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### zhangmazi (Trương Mã Tử)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

#### zhangzhongxian (Trương Trọng Hiên)
- **Animations**: Giống bingyi
- **Events**: `audio`, `hit`, `stuntHit`

---

### ⭐ Main Characters (Nhân vật chính)

#### zhujue_feiyu (Phi Vũ)
- **Animations cơ bản**: `idle`, `move`, `moveBack`, `beAttack`, `stun`, `die`, `win`, `run` + prefix `war_`
- **Attack combo**: `attack`, `attack2`, `backAttack`, `backAttack2` + prefix `war_`
- **Skill**: `skill`, `war_skill` — kỹ năng chủ động
- **Effect Skill**: `effectSkill`, `war_effectSkill` — kỹ năng hiệu ứng đặc biệt
- **Tuyệt kỹ (4 cấp)**: `stunt1` → `stunt2` → `stunt3` → `stunt4` + prefix `war_`
- **Trảm Ma**: `zm_idle`, `zm_start`, `zm_attack`, `zm_stunt`, `zm_die`, `zm_win`
- **Thông Thiên**: `ttzl_idle`, `ttzl_tiaochu` (nhảy ra), `ttzl_luoxia` (rơi xuống)
- **Events**: `audio`, `hit`, `stuntHit`, `skillHit`, `effectSkillHit`

#### zhujue_jianling (Kiếm Linh)
- **Animations cơ bản**: `idle`, `move`, `moveBack`, `beAttack`, `stun`, `die`, `win`, `run` + prefix `war_`
- **Attack combo**: `attack`, `attack2`, `attack3`, `backAttack`, `backAttack2`, `backAttack3` + prefix `war_`
- **Skill**: `skill`, `war_skill`
- **Effect Skill**: `effectSkill`, `war_effectSkill`
- **Tuyệt kỹ (4 cấp)**: `stunt1` → `stunt2` → `stunt3` → `stunt4` + prefix `war_`
  - ⚠️ `war_stunt3` chia thành **3 phase**: `war_stunt3_1`, `war_stunt3_2`, `war_stunt3_3`
- **Trảm Ma**: `zm_idle`, `zm_start`, `zm_attack`, `zm_stunt`, `zm_die`, `zm_win`
- **Thông Thiên**: `ttzl_idle`, `ttzl_tiaochu`, `ttzl_luoxia`
- **Events**: `audio`, `hit`, `stuntHit`, `skillHit`, `effectSkillHit`

#### zhujue_wusheng (Vũ Thánh)
- **Animations cơ bản**: `idle`, `move`, `moveBack`, `beAttack`, `stun`, `die`, `win`, `run` + prefix `war_`
- **Attack combo**: `attack`, `attack2`, `backAttack`, `backAttack2` + prefix `war_`
- **Skill**: `skill`, `war_skill`
- **Tuyệt kỹ (4 cấp)**: `stunt1` → `stunt2` → `stunt3` → `stunt4` + prefix `war_`
- **Trảm Ma**: `zm_idle`, `zm_start`, `zm_attack`, `zm_stunt`, `zm_die`, `zm_win`
- **Thông Thiên**: `ttzl_idle`, `ttzl_tiaochu`, `ttzl_luoxia`
- **Events**: `audio`, `hit`, `stuntHit`, `skillHit`, `stuntBuff`
- **Ghi chú**: Vũ Thánh có event `stuntBuff` (buff đồng đội khi tuyệt kỹ) thay vì `effectSkillHit`

---

### 🐾 Pet / Linh Vật

#### pixiu (Tỳ Hưu)
- **Idle**: `idle`, `idle3`, `idle4`, `idle5`, `idle6` — nhiều biến thể đứng yên
- **Tương tác**: `click`, `click2` — animation khi người chơi click
- **Di chuyển**: `run1-6`, `jump1-6` — chạy và nhảy với nhiều biến thể
- **Bong bóng**: `paopao1-6` — animation hiển thị cảm xúc
- **Ngẫu nhiên**: `random`, `random1`, `random2` — animation ngẫu nhiên khi idle
- **Khác**: `sleep`, `start`, `start3-6`
- **Events**: `jump_end`, `paopao_end`

---

### 🏮 Background / Hoạt Cảnh

> Các character này chỉ dùng làm trang trí, NPC sảnh, hoặc hoạt cảnh nền. Không có event.

#### guotaimingan (Quốc Thái Dân An)
- **Animations**: `idle`
- **Events**: *(Không có)*

#### huahaoyueyuan (Hoa Hảo Nguyệt Viên)
- **Animations**: `idle`
- **Events**: *(Không có)*

#### huodongxingjun (Hoạt Động Hành Quân)
- **Animations**: `idle`, `idle2`
- **Events**: *(Không có)*

#### yaozhu (Yêu Chủ)
- **Animations**: `idle`
- **Events**: *(Không có)*

---

## Flow xử lý Event trong Battle

```
┌─────────────────────────────────────────────────────┐
│  SkeletonAnimation.AnimationState.Event += OnEvent  │
└──────────────────────┬──────────────────────────────┘
                       │
         ┌─────────────┼─────────────┐
         ▼             ▼             ▼
    event="hit"   event="stuntHit"  event="audio"
         │             │             │
         ▼             ▼             ▼
   Apply damage   Apply skill     Play SFX
   Spawn hit VFX  damage + VFX    via AudioManager
                  Apply debuff
```

### Ví dụ code xử lý event:
```csharp
void OnSpineEvent(TrackEntry trackEntry, Spine.Event e) {
    switch (e.Data.Name) {
        case "hit":
            // Đòn đánh thường trúng → tính sát thương + spawn hit effect
            DealDamage(normalDamage);
            SpawnEffect("attack_hit");
            break;
        case "stuntHit":
            // Tuyệt kỹ trúng → tính sát thương skill + spawn skill effect
            DealSkillDamage(stuntDamage);
            SpawnEffect("skill_hit");
            break;
        case "skillHit":
            // Skill đặc biệt trúng (chỉ nhân vật chính)
            DealSkillDamage(skillDamage);
            break;
        case "effectSkillHit":
            // Effect skill trúng (chỉ nhân vật chính)
            DealSkillDamage(effectDamage);
            SpawnEffect("effect_skill_hit");
            break;
        case "stuntBuff":
            // Áp buff (chỉ Vũ Thánh)
            ApplyBuffToTeam(stuntBuff);
            break;
        case "audio":
            // Phát âm thanh
            PlaySFX(e.String); // event string chứa tên file audio
            break;
    }
}
```

---

## Tổng kết nhanh

| Loại Character | Số lượng | Animations | Events |
|---|---|---|---|
| Battle thường | 24 | 20 anim (10 + 10 war_) | `audio`, `hit`, `stuntHit` |
| Nhân vật chính | 3 | 35-45 anim (nhiều combo, skill, zm_, ttzl_) | + `skillHit`, `effectSkillHit`/`stuntBuff` |
| Pet (pixiu) | 1 | 33 anim (biến thể idle, run, jump) | `jump_end`, `paopao_end` |
| Background/NPC | 4 | 1-2 anim (chỉ idle) | *(Không có)* |
