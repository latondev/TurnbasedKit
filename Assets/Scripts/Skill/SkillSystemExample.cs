using UnityEngine;
using GameSystems.Skills;

/// <summary>
/// Example usage of the complete skill system
/// </summary>
public class SkillSystemExample : MonoBehaviour
{
    [SerializeField] SkillController skillController;

    void Start()
    {
        // Add component
        skillController.ControllerName = "Hero Skill System";

        Debug.Log("⚡ Skill System Ready!");
        Debug.Log("→/← = Navigate | Space = Cast | U = Unlock | L = Level Up");
        Debug.Log("R = Reset CD | M = Restore Mana | Tab = Next Ready");
    }
}