using UnityEngine;
using GameSystems.AutoBattle;

public class AutoBattleExample : MonoBehaviour
{
    [SerializeField] AutoBattleController battleController;

    void Start()
    {
        
        // Subscribe to events
        battleController.OnBattleStateChanged += OnBattleStateChanged;
        battleController.OnActionExecuted += OnActionExecuted;
        battleController.OnBattleEnded += OnBattleEnded;
        
        Debug.Log("⚔️ Auto Battle System Ready!");
        Debug.Log("Open Inspector to control battles!");
    }

    private void OnBattleStateChanged(BattleState state)
    {
        Debug.Log($"<color=yellow>Battle State:</color> {state}");
    }

    private void OnActionExecuted(BattleAction action)
    {
        Debug.Log($"<color=orange>Action:</color> {action.description}");
    }

    private void OnBattleEnded(BattleResult result)
    {
        Debug.Log($"<color=cyan>═══ Battle Ended ═══</color>");
        Debug.Log($"<color=yellow>Result: {result.outcome}</color>");
        Debug.Log($"Turns: {result.totalTurns}");
    }
}