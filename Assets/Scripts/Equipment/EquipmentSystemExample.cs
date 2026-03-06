using UnityEngine;
using GameSystems.Equipment;

public class EquipmentSystemExample : MonoBehaviour
{
    [SerializeField] EquipmentController equipmentController;

    void Start()
    {
        equipmentController.ControllerName = "Hero's Equipment";

        Debug.Log("⚔️ Equipment System Ready!");
        Debug.Log("→/← = Navigate | E = Equip | U = Unequip | + = Enhance | R = Repair");
    }
}