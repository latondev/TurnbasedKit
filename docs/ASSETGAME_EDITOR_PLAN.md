# AssetGame Editor Folder Plan

## Scope
This document covers the contents of:

- `Assets/AssetGame/Editor`
- `Assets/Editor`

Current folder contents:

- `SpineDataOrganizer.cs`
- `SpineSettings.asset`

Current editor tooling added later:

- `UnitAuthoringWindow.cs`
- `SkillSequencePreviewController.cs`

## What the folder does
This folder is a small Unity Editor utility area for Spine-related asset organization.

### `SpineDataOrganizer.cs`
Purpose:

- Adds Project window context menu actions for the selected folder.
- Moves or copies the selected folder into `Assets/SpineData/Battle`.

Menu items:

- `Assets/Move to Battle`
- `Assets/Copy to Battle` exists in code, but the menu attribute is commented out, so the action is not currently exposed.

Behavior:

- Uses the first selected asset GUID from `Selection.assetGUIDs`.
- Only works when the selected asset is a valid folder.
- Refuses to operate if the selected folder is already inside the target folder.
- Ensures `Assets/SpineData/Battle` exists before moving/copying.
- Uses `AssetDatabase.MoveAsset` for move and `AssetDatabase.CopyAsset` for copy.

Target:

- `Assets/SpineData/Battle`

Important detail:

- The file contains several comments and log strings with encoding corruption. The logic is readable, but the text needs cleanup if the file is maintained further.

### `SpineSettings.asset`
Purpose:

- Serialized Spine editor settings asset.
- Stores project-wide Spine defaults used by the Spine Unity integration.

Notable values:

- `defaultScale: 0.01`
- `defaultMix: 0.2`
- `defaultShader: Sprites/Default`
- `textureSettingsReference: Assets/Spine/Editor/spine-unity/Editor/ImporterPresets/PMATexturePreset.preset`

## Latest Editor Updates

### `Assets/Editor/UnitAuthoringWindow.cs`
Purpose:

- Hosts the main `Unit Authoring` editor window.
- Keeps the sequence library, prefab authoring, and preview UX in one place.
- Replaces the old static `Asset Previews` tab with a live `Skill Sequence Preview` tab.

Preview behavior:

- The preview tab defaults to the selected skill slot from `CharacterDataSO` (`Basic`, `Ultimate`, `Passive`, `Awaken`).
- The selected skill is read from the inline `SkillData` on the character, then converted to a runtime `SkillViewSequence` from its step selections.
- The preview host uses the currently bound prefab as the Spine source.
- The tab keeps the preview synchronized through editor update ticks, not play-mode coroutines.
- The tab shows the current step list and highlights the active step while the sequence is running.
- When the inline skill data changes, the runtime sequence cache is invalidated so the preview updates immediately.

UI controls:

- Skill slot picker
- Play
- Pause
- Restart
- Speed
- Focus character data

### `Assets/Editor/SkillSequencePreviewController.cs`
Purpose:

- Encapsulates the editor-only live preview runtime for skill sequences.
- Instantiates a hidden prefab host and renders it with `PreviewRenderUtility`.
- Drives `SkillViewSequence` steps in edit mode using the sequence data model.

Behavior:

- Supports play, pause, restart, and playback speed changes.
- Advances step-by-step through the selected sequence.
- Handles immediate steps, timed movement, animation playback, VFX spawning, and hit triggers.
- Cleans up the preview object, spawned VFX, and render utility on disposal.
- Stops and releases the preview host when playback finishes to avoid editor stalls.

### Skill data model (`Assets/Scripts/Skill/SkillData.cs` + `Assets/Scripts/Battle/Runtime/Data/SkillViewSequence.cs`)
Purpose:

- `SkillData` giữ metadata của skill: id, tên, damage, cooldown, effect, `castTime`, icon, và danh sách step được chọn.
- `SkillData.StepSkills` là nguồn authoring chính hiện tại. Mỗi entry là một `SkillViewStepSelection`, trỏ tới 1 step cụ thể trong 1 `SkillViewSequence`.
- `SkillData.ViewSequence` không phải data gốc nữa; nó build một runtime `SkillViewSequence` từ các selection đang có.
- `viewSequence` và `legacyStepSequences` chỉ còn là fallback/migration cho data cũ.
- `runtimeSequenceCache` là cache không serialize, sẽ bị invalidate khi selection đổi hoặc khi deserialize xong.

### `SkillViewStep` fields
`SkillViewStep` là đơn vị step trong `SkillViewSequence`. Cùng một model này được `SkillBehavior` runtime và `SkillSequencePreviewController` editor dùng chung.

- `stepType`: loại hành động của step. Các giá trị hiện có là `MoveToTarget`, `MoveBack`, `PlayAnimation`, `Wait`, `SpawnVfx`, `TriggerHit`, `SetFlipX`, `ResetSortingOrder`, `SetSortingOrder`, `SetIdleAnimation`.
- `targetType`: đích áp dụng của step. `PrimaryTarget` là mục tiêu chính, `AllTargets` là tất cả target, `Actor` là vị trí của người dùng skill, `WorldPosition` là tọa độ world cố định.
- `moveMode`: cách tính vị trí di chuyển khi `stepType == MoveToTarget`. `Direct` đi tới gần target theo `moveDistance`, `ThroughTarget` đi xuyên qua target, `OffsetFromTarget` bỏ qua `moveDistance` và lấy `offset` quanh target.
- `animationName`: tên animation chính dùng cho `PlayAnimation` và cũng là tên hiển thị trong editor.
- `fallbackAnimationName`: tên animation dự phòng nếu `animationName` không tồn tại. Nếu để trống thì runtime sẽ rơi về metadata của sequence hoặc quay lại animation chính.
- `loop`: bật loop cho animation. Có ý nghĩa với `PlayAnimation` và `SetIdleAnimation`.
- `delay`: độ trễ trước khi step bắt đầu. Runtime và preview đều chờ theo giá trị này, rồi mới xử lý step tiếp theo.
- `duration`: thời lượng của step. Dùng để canh thời gian di chuyển ở `MoveToTarget`/`MoveBack`, thời gian chờ ở `Wait`, và thời gian giữ step ở `PlayAnimation` khi `waitForAnimationEnd` bật. Field này là per-step timing, không phải `SkillData.castTime`.
- `moveDistance`: khoảng cách lệch so với target khi di chuyển. Chỉ có ý nghĩa khi `moveMode` là `Direct` hoặc `ThroughTarget`.
- `sortingOrder`: sorting order sẽ set cho `SetSortingOrder`.
- `flipX`: cờ lật trục X khi `SetFlipX`.
- `worldPosition`: vị trí world-space cố định khi `targetType == WorldPosition`.
- `offset`: vector offset cộng thêm sau khi resolve target. Runtime dùng nó cho movement và VFX spawn.
- `vfxPrefab`: `ParticleSystem` sẽ được spawn khi `stepType == SpawnVfx`.
- `waitForAnimationEnd`: cờ cho `PlayAnimation`. Nếu bật thì step sẽ giữ nhịp theo `duration`; nếu tắt thì step coi như immediate sau khi trigger animation.
- `triggerHitEffect`: cờ truyền xuống listener của `OnEndStepAction` để consumer biết có cần bật hit effect hay không.
- `hitCount`: số hit được report khi `TriggerHit` chạy. Nếu giá trị này `<= 0`, runtime sẽ fallback về số target hiện có trong context.

### Inspector notes

- `SkillViewStepDrawer` chỉ hiện `duration` cho `MoveToTarget`, `MoveBack`, `PlayAnimation`, và `Wait`.
- `moveMode` chỉ hiện cho `MoveToTarget`.
- `moveDistance` chỉ hiện cho `MoveToTarget`.
- `sortingOrder` chỉ hiện cho `SetSortingOrder`.
- `flipX` chỉ hiện cho `SetFlipX`.
- `worldPosition` chỉ hiện cho `SpawnVfx`.
- `offset` chỉ hiện cho `MoveToTarget` và `SpawnVfx` trong inspector, nhưng runtime cũng dùng nó khi resolve target.

## Dependencies
This folder depends on:

- Unity Editor APIs: `UnityEditor`, `Selection`, `AssetDatabase`, `EditorUtility`
- Spine Unity editor installation under `Assets/Spine/Editor/spine-unity/`
- Editor-only preview rendering via `PreviewRenderUtility`

## Current observations

1. `CopyToBattle()` is implemented but not available from the menu because its `MenuItem` attribute is commented out.
2. The class is in the global namespace, so it may be worth namespacing if the editor tooling grows.
3. The folder is intentionally narrow: one utility script and one settings asset only.
4. The `Unit Authoring` window now has a live skill-sequence preview path and no longer relies on the old static asset preview tab for that workflow.

## Recommended next steps

1. Clean the encoding in `SpineDataOrganizer.cs` so comments and logs are readable.
2. Decide whether `Copy to Battle` should be enabled again.
3. Keep this doc updated if more files are added to `Assets/AssetGame/Editor` or `Assets/Editor`.
4. If the editor tooling keeps growing, split the preview logic into dedicated docs or add a second index file for authoring tools.

## Last read

- 2026-04-01
