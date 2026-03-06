using UnityEngine;
using GameSystems.Formation;

public class FormationExample : MonoBehaviour
{
    [SerializeField] FormationController formationController;

    void Start()
    {
        
        // Subscribe to events
        formationController.OnFormationChanged += OnFormationChanged;
        formationController.OnUnitPlaced += OnUnitPlaced;
        formationController.OnUnitRemoved += OnUnitRemoved;
        
        Debug.Log("📍 Formation System Ready!");
        Debug.Log("Open Inspector to design formations!");
    }

    private void OnFormationChanged(FormationType formation)
    {
        Debug.Log($"<color=cyan>Formation changed to: {formation.FormationName}</color>");
    }

    private void OnUnitPlaced(FormationSlot slot, string unitId)
    {
        Debug.Log($"<color=green>Placed {unitId} in {slot.SlotName}</color>");
        Debug.Log($"Bonuses: ATK+{slot.AttackBonus*100:F0}% DEF+{slot.DefenseBonus*100:F0}%");
    }

    private void OnUnitRemoved(FormationSlot slot)
    {
        Debug.Log($"<color=yellow>Removed unit from {slot.SlotName}</color>");
    }
}