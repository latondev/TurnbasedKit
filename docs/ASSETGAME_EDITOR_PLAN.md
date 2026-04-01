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
