# AssetGame Editor Folder Plan

## Scope
Document này chỉ mô tả thư mục:

- `Assets/AssetGame/Editor`

File hiện có:

- `SpineDataOrganizer.cs`
- `SpineSettings.asset`

## Current Tooling

### `SpineDataOrganizer.cs`
Mục tiêu:

- Thêm context menu trong Project Window để thao tác với folder đang chọn.
- Chuẩn hóa dữ liệu vào `Assets/SpineData/Battle`.

Menu items:

- `Assets/Move to Battle` (đang bật)
- `Assets/Copy to Battle` (hàm có sẵn nhưng menu action đang comment, chưa expose)

Luồng xử lý chính:

1. Lấy folder đang chọn qua `Selection.assetGUIDs` + `AssetDatabase.GUIDToAssetPath`.
2. Chỉ cho chạy khi selection là folder hợp lệ.
3. Chặn thao tác nếu folder đã nằm trong `Assets/SpineData/Battle`.
4. Tự tạo các folder cha còn thiếu của target bằng `AssetDatabase.CreateFolder`.
5. Move dùng `AssetDatabase.MoveAsset`, copy dùng `AssetDatabase.CopyAsset`.
6. Nếu target trùng tên đã tồn tại thì hiện dialog lỗi và dừng.

Ghi chú:

- `CopyToBattleValidation()` đang có, nhưng action `CopyToBattle()` chưa có `[MenuItem]`, nên hiện tại chỉ move được từ menu.
- Script đang để global namespace.

### `SpineSettings.asset`
Mục tiêu:

- Lưu setting editor của Spine dùng chung cho project.

Thông số đáng chú ý:

- `defaultScale: 0.01`
- `defaultMix: 0.2`
- `defaultShader: Sprites/Default`
- `textureSettingsReference: Assets/Spine/Editor/spine-unity/Editor/ImporterPresets/PMATexturePreset.preset`

## Dependencies

- Unity Editor API: `UnityEditor`, `Selection`, `AssetDatabase`, `EditorUtility`
- Spine Unity editor data dưới `Assets/Spine/Editor/spine-unity/`

## Open Items

1. Quyết định có bật lại menu `Assets/Copy to Battle` hay không.
2. Nếu mở rộng thêm editor tools trong cùng folder, cân nhắc tách namespace để tránh đụng tên class.

## Last read

- 2026-04-03
