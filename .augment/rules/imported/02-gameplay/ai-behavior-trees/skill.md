---
type: "agent_requested"
---

# AI Behavior Trees

## Overview
Modular Behavior Tree system for AI decision making. Supports composite nodes (Selector, Sequence), decorators (Inverter, Repeater), and custom action/condition nodes.

## When to Use
- Use when implementing complex enemy AI
- Use when NPCs need decision-making logic
- Use when AI needs to prioritize tasks
- Use when state machines become too complex
- Use when AI behavior needs to be modular/reusable

## Architecture

```
                    ┌─────────────────┐
                    │   Root Selector │
                    └────────┬────────┘
            ┌────────────────┼────────────────┐
            ↓                                 ↓
    ┌───────────────┐               ┌───────────────┐
    │ Chase Sequence│               │ Patrol Sequence│
    └───────┬───────┘               └───────┬───────┘
        ┌───┴───┐                       ┌───┴───┐
        ↓       ↓                       ↓       ↓
    ┌──────┐ ┌──────┐              ┌──────┐ ┌──────┐
    │CanSee│ │MoveTo│              │HasPath│ │Follow │
    │Player│ │Player│              │      │ │Path   │
    └──────┘ └──────┘              └──────┘ └──────┘
```

## Node Types

| Type | Behavior |
|------|----------|
| **Selector** | Tries children until one succeeds (OR) |
| **Sequence** | Runs children until one fails (AND) |
| **Inverter** | Flips child result |
| **Repeater** | Repeats child N times |
| **Condition** | Returns success/failure based on check |
| **Action** | Performs game action |

## Node Return Values
- **Success**: Node completed successfully
- **Failure**: Node failed
- **Running**: Node still processing

## Best Practices
- ✅ Keep nodes small and focused
- ✅ Use Blackboard for shared data
- ✅ Cache frequently used references
- ✅ Use decorators for flow control
- ❌ **NEVER** put heavy calculations in Tick
- ❌ **NEVER** use deep nested trees (max 5-6 levels)

## Few-Shot Examples

### Example 1: Enemy AI
**User**: "Enemy patrols, chases player when seen."

**Agent**:
```
Root Selector
├── Chase Sequence
│   ├── Condition: CanSeePlayer
│   └── Action: MoveToPlayer
└── Patrol Sequence
    ├── Condition: HasPatrolPath
    └── Action: FollowPatrolPath
```

### Example 2: Attack Priority
**User**: "Attack melee if close, else ranged."

**Agent**:
```
Root Selector
├── Melee Sequence
│   ├── Condition: InMeleeRange
│   └── Action: MeleeAttack
└── Ranged Sequence
    ├── Condition: HasAmmo
    └── Action: RangedAttack
```

## Related Skills
- `@state-machine-architect` - Simpler state-based AI
- `@navmesh-pathfinding` - Movement for AI
- `@damage-health-framework` - Combat integration

## Template Files
Available in templates/ folder.
