---
type: "agent_requested"
---

# New Input System

## Overview
Unity New Input System abstraction layer. Wraps input actions into ScriptableObject-based event channels for decoupled, multi-device input handling.

## When to Use
- Use for multi-platform input (keyboard, gamepad, touch)
- Use for rebindable controls
- Use for local multiplayer
- Use for UI navigation input
- Use for action-based input (not polling)

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                  INPUT SYSTEM ARCHITECTURE                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  INPUT ACTIONS ASSET     INPUT READER (SO)    CONSUMERS    │
│  ┌──────────────────┐   ┌──────────────┐    ┌──────────┐  │
│  │ .inputactions    │──▶│ InputReader  │───▶│ Player   │  │
│  │ ┌──────────────┐ │   │              │    │ UI       │  │
│  │ │ Move         │ │   │ OnMove       │    │ Camera   │  │
│  │ │ Jump         │ │   │ OnJump       │    │ Vehicle  │  │
│  │ │ Attack       │ │   │ OnAttack     │    │ etc.     │  │
│  │ └──────────────┘ │   └──────────────┘    └──────────┘  │
│  └──────────────────┘                                      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Input Action Types

| Type | Use | C# Type |
|------|-----|---------|
| **Button** | Jump, Attack | `bool` |
| **Value** | Move, Look | `Vector2` |
| **PassThrough** | Continuous input | Any |

## Control Schemes

| Scheme | Devices |
|--------|---------|
| **Keyboard&Mouse** | Keyboard + Mouse |
| **Gamepad** | Xbox, PlayStation, Switch |
| **Touch** | Touchscreen |

## Best Practices
- ✅ Create InputReader ScriptableObject
- ✅ Use C# events, not direct polling
- ✅ Generate C# class from .inputactions
- ✅ Use PlayerInput component for multiplayer
- ✅ Implement rebinding system
- ❌ **NEVER** poll Input directly (Input.GetKey)
- ❌ **NEVER** reference InputActions in consumers
- ❌ **NEVER** forget to enable action maps

## Few-Shot Examples

### Example 1: Input Reader ScriptableObject
**User**: "Create an input reader."

**Agent**:
```csharp
[CreateAssetMenu(fileName = "InputReader", menuName = "Input/Input Reader")]
public class InputReader : ScriptableObject, GameInput.IPlayerActions
{
    private GameInput _gameInput;
    
    public event Action<Vector2> OnMoveEvent;
    public event Action OnJumpEvent;
    public event Action OnAttackEvent;
    public event Action<bool> OnSprintEvent;
    
    public Vector2 MoveInput { get; private set; }
    
    private void OnEnable()
    {
        _gameInput ??= new GameInput();
        _gameInput.Player.SetCallbacks(this);
        _gameInput.Player.Enable();
    }
    
    private void OnDisable()
    {
        _gameInput.Player.Disable();
    }
    
    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
        OnMoveEvent?.Invoke(MoveInput);
    }
    
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnJumpEvent?.Invoke();
    }
    
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnAttackEvent?.Invoke();
    }
    
    public void OnSprint(InputAction.CallbackContext context)
    {
        OnSprintEvent?.Invoke(context.performed);
    }
}
```

### Example 2: Consumer (PlayerController)
**User**: "Connect input to player."

**Agent**:
```csharp
public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputReader _inputReader;
    
    private void OnEnable()
    {
        _inputReader.OnJumpEvent += HandleJump;
        _inputReader.OnAttackEvent += HandleAttack;
    }
    
    private void OnDisable()
    {
        _inputReader.OnJumpEvent -= HandleJump;
        _inputReader.OnAttackEvent -= HandleAttack;
    }
    
    private void Update()
    {
        Vector2 move = _inputReader.MoveInput;
        // Use move for continuous input
    }
    
    private void HandleJump() => _jumpRequested = true;
    private void HandleAttack() => _attackRequested = true;
}
```

### Example 3: UI Navigation
**User**: "Enable gamepad UI navigation."

**Agent**:
```csharp
private void SwitchToUI()
{
    _gameInput.Player.Disable();
    _gameInput.UI.Enable();
    
    // UI Toolkit focus
    _uiDocument.rootVisualElement.Q<Button>().Focus();
}

private void SwitchToGameplay()
{
    _gameInput.UI.Disable();
    _gameInput.Player.Enable();
}
```

## Rebinding Flow
1. Start rebinding: `action.PerformInteractiveRebinding()`
2. Wait for input
3. Save to PlayerPrefs: `action.SaveBindingOverridesAsJson()`
4. Load on startup: `action.LoadBindingOverridesFromJson()`

## Related Skills
- `@menu-navigation-flow` - UI with gamepad
- `@ui-toolkit-modern` - Focusable elements
- `@advanced-character-controller` - Movement input
