# Battle System Change Plan

## Mục tiêu
Chuẩn hóa luồng battle + stats để mỗi lớp chỉ làm đúng một việc:
- `AutoBattleController` điều phối combat loop
- `BattleUnit` giữ combat state theo stat runtime
- `BattleVisualManager` chỉ xử lý visual sync
- `BattleSceneSetup` chỉ spawn team và gắn hệ thống
- `BattleUIView` chỉ hiển thị UI và nhận input

## Những thay đổi đã triển khai

### 1. Logic đánh
- Giữ `AutoBattleController` làm orchestration chính cho battle flow.
- Reset đúng cờ `IsWaitingForVisuals` khi `StartBattle`, `StopBattle`, `InitializeBattle`, và `EndBattle`.
- Battle end/stop không còn để state chờ visual bị treo.

### 2. Damage / stat
- `BattleUnit` được làm null-safe hơn khi đọc stat từ `UnitStatController`.
- `BattleUnit` có `Dispose()` để dọn `UnitStatController` GameObject khi reset/destroy team.
- `BattleUnit.Reset()` và các hàm combat/buff đều guard khi stat controller bị thiếu.

### 3. Animation / visual
- `BattleVisualManager` có fallback formation nếu chưa gán `BattlePrefabConfig`.
- `BattleVisualManager` chịu được input list null.
- `BattleVisualManager` tự clear cờ chờ visual khi scene unload.

### 4. UI
- `BattleUIView` vẫn là lớp hiển thị, không nắm logic combat.
- Nút điều khiển battle và stats demo được gom về UI layer.
- HUD stats được dock gọn khi scene battle có UI gốc.

### 5. Data / asset
- `BattleSceneSetup` dọn team cũ trước khi spawn lại để tránh leak stat controller cũ.
- `BattleSceneSetup.OnDestroy()` dọn cả event subscription lẫn team runtime object.
- Mục tiêu là giữ data/asset layer không lẫn logic battle runtime.

## File đã chạm chính
- `Assets/Scripts/Battle/Runtime/Controllers/AutoBattleController.cs`
- `Assets/Scripts/Battle/Runtime/Entities/BattleUnit.cs`
- `Assets/Scripts/Battle/Examples/BattleSceneSetup.cs`
- `Assets/Scripts/Battle/Examples/BattleVisualManager.cs`

## Checklist kiểm tra trong Unity
- Mở `Battle.unity`
- Bấm `Start`
- Bấm `Reset`
- Bấm `Speed`
- Xác nhận không bị kẹt trạng thái chờ visual
- Xác nhận reset không để lại unit/stat controller cũ
- Xác nhận fallback placeholder vẫn chạy nếu thiếu prefab config

## Ghi chú
- Đây là tài liệu mô tả trạng thái hiện tại sau khi refactor, không phải proposal mới.
- Nếu muốn tách tiếp legacy battle layer, phần tiếp theo nên là cleanup và gom luồng UI/visual cũ về một chuẩn duy nhất.
