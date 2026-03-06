#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using GameSystems.AutoBattle;
using System.Linq;

[CustomEditor(typeof(AutoBattleController))]
public class AutoBattleEditor : Editor
{
    private AutoBattleController controller;
    private GUIStyle titleStyle;
    private GUIStyle headerStyle;
    private GUIStyle unitStyle;
    private GUIStyle turnStyle;
    private bool isInitialized;

    private bool showPlayerUnits = true;
    private bool showEnemyUnits = true;
    private bool showTurnHistory = false;
    private Vector2 playerScrollPos;
    private Vector2 enemyScrollPos;
    private Vector2 turnScrollPos;

    private void OnEnable()
    {
        controller = (AutoBattleController)target;
        EditorApplication.update += UpdateInspector;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdateInspector;
    }

    private void UpdateInspector()
    {
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private void InitializeStyles()
    {
        if (isInitialized) return;

        titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.5f, 0.3f) }
        };

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            normal = { textColor = Color.white }
        };

        unitStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(0, 0, 3, 3)
        };

        turnStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(8, 8, 5, 5),
            margin = new RectOffset(0, 0, 2, 2)
        };

        isInitialized = true;
    }

    public override void OnInspectorGUI()
    {
        InitializeStyles();
        
        DrawDefaultInspector();

        if (controller == null) return;

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawBattleTitle();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to start auto battles", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawBattleStatus();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawBattleControls();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawStatistics();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawPlayerUnits();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawEnemyUnits();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawTurnHistory();

        if (controller.LastResult != null)
        {
            EditorGUILayout.Space(10);
            DrawSeparator();
            DrawBattleResult();
        }
    }

    private void DrawBattleTitle()
    {
        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.5f, 0.3f, 0.5f);

        EditorGUILayout.BeginVertical(unitStyle);
        GUI.backgroundColor = previousBg;

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("⚔️ Auto Battle System", titleStyle);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUIStyle subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };
        EditorGUILayout.LabelField("Turn-Based Combat", subtitleStyle);

        EditorGUILayout.EndVertical();
    }

    private void DrawBattleStatus()
    {
        EditorGUILayout.LabelField("Battle Status", headerStyle);

        Color previousBg = GUI.backgroundColor;
        
        Color stateColor = controller.CurrentState switch
        {
            BattleState.Idle => new Color(0.5f, 0.5f, 0.5f, 0.5f),
            BattleState.InProgress => new Color(1f, 0.9f, 0.3f, 0.5f),
            BattleState.Ended => new Color(0.3f, 0.8f, 0.5f, 0.5f),
            _ => new Color(0.3f, 0.3f, 0.3f, 0.5f)
        };
        
        GUI.backgroundColor = stateColor;
        EditorGUILayout.BeginVertical(unitStyle);
        GUI.backgroundColor = previousBg;

        // State
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🎮 State:", GUILayout.Width(100));
        GUIStyle stateStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = GetStateColor(controller.CurrentState) }
        };
        EditorGUILayout.LabelField(controller.CurrentState.ToString(), stateStyle);
        EditorGUILayout.EndHorizontal();

        // Current Turn
        if (controller.CurrentState == BattleState.InProgress)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔄 Turn:", GUILayout.Width(100));
            EditorGUILayout.LabelField($"{controller.CurrentTurn}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            // Active Unit
            if (controller.CurrentActiveUnit != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("⚡ Active:", GUILayout.Width(100));
                GUIStyle activeStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = controller.CurrentActiveUnit.GetUnitColor() }
                };
                EditorGUILayout.LabelField(controller.CurrentActiveUnit.UnitName, activeStyle);
                EditorGUILayout.EndHorizontal();
            }
        }

        // Alive counts
        int alivePlayer = controller.PlayerUnits.Count(u => u.IsAlive);
        int aliveEnemy = controller.EnemyUnits.Count(u => u.IsAlive);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("👥 Players Alive:", GUILayout.Width(100));
        GUIStyle playerCountStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = new Color(0.3f, 0.7f, 1f) }
        };
        EditorGUILayout.LabelField($"{alivePlayer}/{controller.PlayerUnits.Count}", playerCountStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("💀 Enemies Alive:", GUILayout.Width(100));
        GUIStyle enemyCountStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = new Color(1f, 0.3f, 0.3f) }
        };
        EditorGUILayout.LabelField($"{aliveEnemy}/{controller.EnemyUnits.Count}", enemyCountStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawBattleControls()
    {
        EditorGUILayout.LabelField("Battle Controls", headerStyle);

        Color previousBg = GUI.backgroundColor;

        // Start/Stop buttons
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = controller.CurrentState == BattleState.Idle;
        GUI.backgroundColor = new Color(0.5f, 1f, 0.7f);
        if (GUILayout.Button("▶ Start Battle", GUILayout.Height(35)))
        {
            controller.StartBattle();
        }

        GUI.enabled = controller.CurrentState == BattleState.InProgress;
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("■ Stop Battle", GUILayout.Height(35)))
        {
            controller.StopBattle();
        }

        GUI.enabled = true;
        GUI.backgroundColor = previousBg;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Speed control
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("⚡ Battle Speed:", GUILayout.Width(100));
        
        float speed = EditorGUILayout.Slider(1f, 0.5f, 5f);
        if (GUILayout.Button("Set", GUILayout.Width(50)))
        {
            controller.SetBattleSpeed(speed);
        }
        
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Quick speed buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("0.5x", GUILayout.Height(25)))
        {
            controller.SetBattleSpeed(0.5f);
        }
        if (GUILayout.Button("1x", GUILayout.Height(25)))
        {
            controller.SetBattleSpeed(1f);
        }
        if (GUILayout.Button("2x", GUILayout.Height(25)))
        {
            controller.SetBattleSpeed(2f);
        }
        if (GUILayout.Button("5x", GUILayout.Height(25)))
        {
            controller.SetBattleSpeed(5f);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Info button
        GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
        if (GUILayout.Button("📊 Show Battle Info", GUILayout.Height(25)))
        {
            controller.ShowBattleInfo();
        }
        GUI.backgroundColor = previousBg;
    }

    private void DrawStatistics()
    {
        EditorGUILayout.LabelField("Statistics", headerStyle);

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.3f, 0.4f, 0.6f, 0.5f);
        EditorGUILayout.BeginVertical(unitStyle);
        GUI.backgroundColor = previousBg;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("📊 Total Battles:", GUILayout.Width(120));
        EditorGUILayout.LabelField(controller.TotalBattles.ToString(), EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("✓ Victories:", GUILayout.Width(120));
        GUIStyle victoryStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = new Color(0.5f, 1f, 0.5f) }
        };
        EditorGUILayout.LabelField(controller.VictoriesCount.ToString(), victoryStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("✗ Defeats:", GUILayout.Width(120));
        GUIStyle defeatStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = new Color(1f, 0.5f, 0.5f) }
        };
        EditorGUILayout.LabelField(controller.DefeatsCount.ToString(), defeatStyle);
        EditorGUILayout.EndHorizontal();

        float winRate = controller.TotalBattles > 0 
            ? (float)controller.VictoriesCount / controller.TotalBattles * 100f 
            : 0f;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("📈 Win Rate:", GUILayout.Width(120));
        EditorGUILayout.LabelField($"{winRate:F1}%", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawPlayerUnits()
    {
        EditorGUILayout.BeginHorizontal();
        showPlayerUnits = EditorGUILayout.Foldout(showPlayerUnits, 
            $"👥 Player Units ({controller.PlayerUnits.Count})", true, headerStyle);
        EditorGUILayout.EndHorizontal();

        if (!showPlayerUnits) return;

        if (controller.PlayerUnits.Count == 0)
        {
            EditorGUILayout.HelpBox("No player units", MessageType.Info);
            return;
        }

        playerScrollPos = EditorGUILayout.BeginScrollView(playerScrollPos, GUILayout.MaxHeight(300));

        foreach (var unit in controller.PlayerUnits)
        {
            DrawUnit(unit);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawEnemyUnits()
    {
        EditorGUILayout.BeginHorizontal();
        showEnemyUnits = EditorGUILayout.Foldout(showEnemyUnits, 
            $"💀 Enemy Units ({controller.EnemyUnits.Count})", true, headerStyle);
        EditorGUILayout.EndHorizontal();

        if (!showEnemyUnits) return;

        if (controller.EnemyUnits.Count == 0)
        {
            EditorGUILayout.HelpBox("No enemy units", MessageType.Info);
            return;
        }

        enemyScrollPos = EditorGUILayout.BeginScrollView(enemyScrollPos, GUILayout.MaxHeight(300));

        foreach (var unit in controller.EnemyUnits)
        {
            DrawUnit(unit);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawUnit(BattleUnit unit)
    {
        Color bgColor = unit.GetUnitColor();
        bgColor.a = unit.IsAlive ? 0.3f : 0.15f;

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = bgColor;

        EditorGUILayout.BeginVertical(unitStyle);
        GUI.backgroundColor = previousBg;

        // Unit header
        EditorGUILayout.BeginHorizontal();
        
        GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = unit.GetUnitColor() }
        };
        EditorGUILayout.LabelField(unit.UnitName, nameStyle, GUILayout.Width(120));

        if (!unit.IsAlive)
        {
            GUIStyle deadStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 0.3f, 0.3f) },
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField("💀 DEFEATED", deadStyle);
        }

        EditorGUILayout.EndHorizontal();

        // HP Bar
        DrawHPBar(unit);

        EditorGUILayout.Space(3);

        // Stats
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"⚔️ ATK: {unit.FinalAttack}", EditorStyles.miniLabel, GUILayout.Width(80));
        EditorGUILayout.LabelField($"🛡️ DEF: {unit.FinalDefense}", EditorStyles.miniLabel, GUILayout.Width(80));
        EditorGUILayout.LabelField($"⚡ SPD: {unit.FinalSpeed}", EditorStyles.miniLabel, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        // Battle stats
        if (controller.CurrentState != BattleState.Idle)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"💥 Dealt: {unit.DamageDealt}", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField($"💔 Taken: {unit.DamageTaken}", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField($"🔄 Turns: {unit.TurnsTaken}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawHPBar(BattleUnit unit)
    {
        Rect hpRect = EditorGUILayout.GetControlRect(false, 20);
        float percentage = unit.GetHPPercentage();

        EditorGUI.DrawRect(hpRect, new Color(0.2f, 0.2f, 0.2f));

        Rect fillRect = hpRect;
        fillRect.width *= percentage;
        Color hpColor = percentage > 0.5f ? new Color(0.5f, 1f, 0.5f) : 
                       percentage > 0.25f ? new Color(1f, 0.9f, 0.3f) : 
                       new Color(1f, 0.3f, 0.3f);
        EditorGUI.DrawRect(fillRect, hpColor);

        GUIStyle hpTextStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold
        };
        GUI.Label(hpRect, $"HP: {unit.CurrentHP}/{unit.MaxHP} ({percentage * 100:F0}%)", hpTextStyle);
    }

    private void DrawTurnHistory()
    {
        EditorGUILayout.BeginHorizontal();
        showTurnHistory = EditorGUILayout.Foldout(showTurnHistory, 
            $"📜 Turn History ({controller.CurrentTurn})", true, headerStyle);
        EditorGUILayout.EndHorizontal();

        if (!showTurnHistory) return;

        if (controller.CurrentTurn == 0)
        {
            EditorGUILayout.HelpBox("No turns yet", MessageType.Info);
            return;
        }

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
        EditorGUILayout.BeginVertical(unitStyle);
        GUI.backgroundColor = previousBg;

        turnScrollPos = EditorGUILayout.BeginScrollView(turnScrollPos, GUILayout.MaxHeight(200));

        // Show last 20 turns
        var recentTurns = controller.PlayerUnits
            .Concat(controller.EnemyUnits)
            .SelectMany(u => u.ActionsLog)
            .Reverse()
            .Take(20)
            .Reverse();

        foreach (var log in recentTurns)
        {
            EditorGUILayout.LabelField(log, EditorStyles.miniLabel);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawBattleResult()
    {
        EditorGUILayout.LabelField("Battle Result", headerStyle);

        var result = controller.LastResult;
        
        Color resultColor = result.outcome switch
        {
            BattleOutcome.Victory => new Color(0.3f, 0.8f, 0.5f, 0.5f),
            BattleOutcome.Defeat => new Color(1f, 0.3f, 0.3f, 0.5f),
            _ => new Color(0.5f, 0.5f, 0.5f, 0.5f)
        };

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = resultColor;
        EditorGUILayout.BeginVertical(unitStyle);
        GUI.backgroundColor = previousBg;

        // Outcome
        GUIStyle outcomeStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = GetOutcomeColor(result.outcome) }
        };
        EditorGUILayout.LabelField(GetOutcomeText(result.outcome), outcomeStyle);

        EditorGUILayout.Space(5);

        // Total turns
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Total Turns:", GUILayout.Width(100));
        EditorGUILayout.LabelField(result.totalTurns.ToString(), EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private Color GetStateColor(BattleState state)
    {
        return state switch
        {
            BattleState.Idle => new Color(0.7f, 0.7f, 0.7f),
            BattleState.InProgress => new Color(1f, 0.9f, 0.3f),
            BattleState.Paused => new Color(1f, 0.6f, 0.3f),
            BattleState.Ended => new Color(0.5f, 1f, 0.5f),
            _ => Color.white
        };
    }

    private Color GetOutcomeColor(BattleOutcome outcome)
    {
        return outcome switch
        {
            BattleOutcome.Victory => new Color(0.3f, 1f, 0.5f),
            BattleOutcome.Defeat => new Color(1f, 0.3f, 0.3f),
            BattleOutcome.Draw => new Color(0.8f, 0.8f, 0.8f),
            _ => Color.white
        };
    }

    private string GetOutcomeText(BattleOutcome outcome)
    {
        return outcome switch
        {
            BattleOutcome.Victory => "🎉 VICTORY! 🎉",
            BattleOutcome.Defeat => "💀 DEFEAT 💀",
            BattleOutcome.Draw => "⚔️ DRAW ⚔️",
            _ => "???"
        };
    }

    private void DrawSeparator()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }
}

#endif
