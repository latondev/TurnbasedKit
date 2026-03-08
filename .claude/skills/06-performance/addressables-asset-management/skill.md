---
type: "agent_requested"
---

# Addressables & Asset Management

## Overview
Unity Addressables system for async asset loading, memory management, and remote content delivery. Reduces startup time and memory footprint.

## When to Use
- Use for large prefabs loaded on demand
- Use for remote/DLC content
- Use for memory-constrained platforms
- Use with AssetReference in ScriptableObjects
- Use for runtime asset bundles

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                 ADDRESSABLES ARCHITECTURE                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ASSET REFERENCE        ADDRESSABLES           MEMORY       │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────┐  │
│  │ AssetRef<T>  │─────▶│ Load Async   │─────▶│ Loaded   │  │
│  │ (Inspector)  │      │ Instantiate  │      │ Asset    │  │
│  └──────────────┘      │ Release      │      └──────────┘  │
│                        └──────────────┘            │        │
│                              ↑                     ▼        │
│                        ┌─────┴─────┐         ┌──────────┐  │
│                        │ LOCAL     │         │ RELEASE  │  │
│                        │ REMOTE    │◀────────│ Unload   │  │
│                        │ CACHED    │         └──────────┘  │
│                        └───────────┘                        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Key Concepts

| Concept | Description |
|---------|-------------|
| **AssetReference** | Inspector-friendly key |
| **Address** | String key for assets |
| **Label** | Group assets by tag |
| **Handle** | Async operation result |
| **Release** | Free loaded memory |

## Loading Patterns

| Pattern | Use |
|---------|-----|
| **LoadAssetAsync** | Get asset without instantiation |
| **InstantiateAsync** | Load + Instantiate |
| **Release** | Free single asset |
| **ReleaseInstance** | Destroy + Release |

## Best Practices
- ✅ Always release when done
- ✅ Use AssetReference over string keys
- ✅ Group assets by usage patterns
- ✅ Pre-download remote content
- ✅ Use labels for batch loading
- ❌ **NEVER** forget to release loaded assets
- ❌ **NEVER** use Resources.Load with Addressables
- ❌ **NEVER** hold references to released assets

## Few-Shot Examples

### Example 1: Asset Loader Service
**User**: "Load assets asynchronously."

**Agent**:
```csharp
public class AssetLoader
{
    private readonly Dictionary<string, AsyncOperationHandle> _loadedAssets = new();
    
    public async UniTask<T> LoadAsync<T>(AssetReference reference) where T : Object
    {
        var handle = reference.LoadAssetAsync<T>();
        await handle.Task;
        
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            _loadedAssets[reference.AssetGUID] = handle;
            return handle.Result;
        }
        
        Debug.LogError($"Failed to load: {reference}");
        return null;
    }
    
    public void Release(AssetReference reference)
    {
        if (_loadedAssets.TryGetValue(reference.AssetGUID, out var handle))
        {
            Addressables.Release(handle);
            _loadedAssets.Remove(reference.AssetGUID);
        }
    }
    
    public void ReleaseAll()
    {
        foreach (var handle in _loadedAssets.Values)
        {
            Addressables.Release(handle);
        }
        _loadedAssets.Clear();
    }
}
```

### Example 2: Prefab Spawner
**User**: "Spawn addressable prefab."

**Agent**:
```csharp
public class AddressableSpawner : MonoBehaviour
{
    [SerializeField] private AssetReferenceGameObject _prefabRef;
    
    private AsyncOperationHandle<GameObject> _handle;
    private GameObject _instance;
    
    public async UniTask<GameObject> SpawnAsync(Vector3 position)
    {
        _handle = _prefabRef.InstantiateAsync(position, Quaternion.identity);
        _instance = await _handle.Task;
        return _instance;
    }
    
    public void Despawn()
    {
        if (_instance != null)
        {
            Addressables.ReleaseInstance(_instance);
            _instance = null;
        }
    }
    
    private void OnDestroy()
    {
        Despawn();
    }
}
```

### Example 3: Preload by Label
**User**: "Preload all level assets."

**Agent**:
```csharp
public async UniTask PreloadLevelAsync(string levelLabel)
{
    var locations = await Addressables.LoadResourceLocationsAsync(levelLabel);
    
    var downloadSize = await Addressables.GetDownloadSizeAsync(levelLabel);
    if (downloadSize > 0)
    {
        await Addressables.DownloadDependenciesAsync(levelLabel);
    }
    
    foreach (var location in locations.Result)
    {
        await Addressables.LoadAssetAsync<Object>(location);
    }
}
```

## Memory Management
```csharp
// CRITICAL: Always pair Load with Release
var handle = Addressables.LoadAssetAsync<GameObject>(key);
// ... use asset ...
Addressables.Release(handle);
```

## Related Skills
- `@object-pooling-system` - Pool addressable instances
- `@memory-profiler-expert` - Track loaded assets
- `@asynchronous-programming` - Async patterns
