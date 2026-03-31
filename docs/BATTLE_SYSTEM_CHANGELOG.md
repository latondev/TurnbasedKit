# Battle System Changelog

## [Unreleased] - 2026-03-31

### Added
- `docs/BATTLE_SYSTEM_CHANGE_PLAN.md` để mô tả phạm vi refactor battle/stats.
- Battle demo UI integration trong `Battle.unity`.
- Stats demo overlay + 1v1 mini battle demo cho `UnitStatController`.
- `Assets/Editor/BattleUnitPrefabBuilderWindow.cs` để build battle-ready prefab từ prefab Role drag-and-drop.

### Changed
- `AutoBattleController` được giữ làm orchestration layer chính và reset đúng `IsWaitingForVisuals`.
- `BattleUnit` được làm null-safe hơn và có `Dispose()` để dọn `UnitStatController` runtime object.
- `BattleSceneSetup` dọn team runtime cũ trước khi spawn lại, giảm leak khi reset battle.
- `BattleVisualManager` có fallback formation khi thiếu `BattlePrefabConfig` và tự clear visual-wait flag khi unload.
- `BattleUIView` nhận nút điều khiển battle/stats demo và giữ vai trò hiển thị בלבד.
- `AnimationHandle` trở thành adapter Spine duy nhất cho battle; `AnimationController` đã được loại khỏi battle code path.

### Fixed
- Tránh treo state chờ visual khi battle start/stop/end.
- Tránh leak unit stat controller khi reset team hoặc destroy scene.
- Tránh crash khi `BattleVisualManager` không có config hoặc nhận list null.
- Tránh prefab builder tự add component animation wrapper thừa.

### Notes
- Behavior battle hiện tại được giữ nguyên, chỉ dọn kiến trúc, an toàn vòng đời, và khả năng demo.
- Nếu cần cleanup tiếp, bước tiếp theo là gom/loại các file legacy battle song song.
