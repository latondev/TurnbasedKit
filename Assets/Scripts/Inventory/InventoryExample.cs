using UnityEngine;
using GameSystems.Inventory;

public class InventoryExample : MonoBehaviour
{
    [SerializeField] InventoryIteratorController inventory;

    void Start()
    {
        // Add component
        inventory.InventoryName = "Hero's Backpack";

        Debug.Log("📦 Inventory System Ready!");
        Debug.Log("→/← = Navigate | E = Equip | U = Use | D = Drop");
    }
}