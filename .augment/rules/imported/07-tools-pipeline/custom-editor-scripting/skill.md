---
type: "agent_requested"
---

# Custom Editor Scripting

## Overview
Creation of custom Unity Editor tools, Inspector extensions, and Property Drawers. Accelerates development by building project-specific tooling and improving the authoring experience.

## When to Use
- Use when Inspector is cluttered
- Use for level design tools
- Use for data validation
- Use for efficient asset management
- Use for debugging visualization

## Editor Classes Hierarchy

| Class | Base | Use Case |
|-------|------|----------|
| **Editor** | `ScriptableObject` | Custom Inspector for Components |
| **EditorWindow** | `ScriptableObject` | Floating/Dockable Tool Windows |
| **PropertyDrawer** | `GUIDrawer` | Custom UI for Serializable fields |
| **AssetPostprocessor** | `AssetPostprocessor` | Import pipeline automation |
| **ScriptableWizard** | `EditorWindow` | Multi-step setup wizards |

## Immediate Mode GUI (IMGUI)

```
┌─────────────────────────────────────────────────────────────┐
│                     EDITOR GUI STACK                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  EditorGUILayout (Auto-layout)                              │
│  ┌──────────────────────┐                                   │
│  │ BeginVertical()      │                                   │
│  │  LabelField          │                                   │
│  │  PropertyField       │                                   │
│  │  HelpBox             │                                   │
│  │ EndVertical()        │                                   │
│  └──────────────────────┘                                   │
│             │                                               │
│             ▼                                               │
│  SerializedObject / SerializedProperty                      │
│  ┌──────────────────────┐                                   │
│  │ Update()             │                                   │
│  │ FindProperty()       │                                   │
│  │ ApplyModifiedProperties()                                │
│  └──────────────────────┘                                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Best Practices
- ✅ Wrap code in `#if UNITY_EDITOR`
- ✅ Use `SerializedObject` / `SerializedProperty` for Undo support
- ✅ Use `OnEnable` / `OnDisable` for initialization
- ✅ Use `EditorGUILayout` for automatic layout
- ✅ Clean up resources (textures, handles) immediately
- ❌ **NEVER** modify `target` directly (breaks Undo/Prefab)
- ❌ **NEVER** usage `GetComponent` in `OnGUI` (performance)
- ❌ **NEVER** put Editor scripts in runtime folders

## Few-Shot Examples

### Example 1: Custom Inspector
**User**: "Improve the Enemy inspector."

**Agent**:
```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemy))]
public class EnemyEditor : Editor
{
    private SerializedProperty _healthProp;
    private SerializedProperty _attackProp;

    private void OnEnable()
    {
        _healthProp = serializedObject.FindProperty("_maxHealth");
        _attackProp = serializedObject.FindProperty("_attackDamage");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(_healthProp);
        if (_healthProp.intValue < 0)
        {
            EditorGUILayout.HelpBox("Health cannot be negative!", MessageType.Error);
        }

        EditorGUILayout.PropertyField(_attackProp);

        if (GUILayout.Button("Reset Stats"))
        {
            _healthProp.intValue = 100;
            _attackProp.floatValue = 10f;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
```

### Example 2: Simple Editor Window
**User**: "Tool to bulk rename objects."

**Agent**:
```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class BulkRenamer : EditorWindow
{
    private string _baseName = "Enemy";
    private int _startIndex = 1;

    [MenuItem("Tools/Bulk Renamer")]
    public static void ShowWindow()
    {
        GetWindow<BulkRenamer>("Renamer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Bulk Rename Selected", EditorStyles.boldLabel);
        
        _baseName = EditorGUILayout.TextField("Base Name", _baseName);
        _startIndex = EditorGUILayout.IntField("Start Index", _startIndex);

        if (GUILayout.Button("Rename"))
        {
            RenameSelected();
        }
    }

    private void RenameSelected()
    {
        var selected = Selection.gameObjects;
        for (int i = 0; i < selected.Length; i++)
        {
            Undo.RecordObject(selected[i], "Bulk Rename");
            selected[i].name = $"{_baseName}_{_startIndex + i}";
        }
    }
}
#endif
```

### Example 3: Property Drawer
**User**: "Draw a range as a min-max slider."

**Agent**:
```csharp
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
public class MinMaxRangeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var minProp = property.FindPropertyRelative("min");
        var maxProp = property.FindPropertyRelative("max");
        
        float minVal = minProp.floatValue;
        float maxVal = maxProp.floatValue;
        
        EditorGUI.MinMaxSlider(position, label, ref minVal, ref maxVal, 0f, 100f);
        
        minProp.floatValue = minVal;
        maxProp.floatValue = maxVal;
    }
}
#endif
```

## Related Skills
- `@unity-mcp-connector` - Connect tools to AI
- `@automated-unit-testing` - Test your tools
- `@version-control-git` - Commit tool configurations
