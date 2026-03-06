#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using GameSystems.Formation;
using System.Linq;

[CustomEditor(typeof(FormationController))]
public class FormationControllerEditor : Editor
{
    private FormationController controller;
    private GUIStyle titleStyle;
    private GUIStyle headerStyle;
    private GUIStyle slotStyle;
    private GUIStyle unitStyle;
    private bool isInitialized;

    private bool showFormationGrid = true;
    private bool showSlotDetails = true;
    private bool showAvailableUnits = true;
    private Vector2 slotScrollPos;
    private Vector2 unitScrollPos;

    private int selectedSlotId = -1;
    private string selectedUnitId = "";

    private const float SLOT_SIZE = 60f;
    private const float SLOT_SPACING = 5f;

    private void OnEnable()
    {
        controller = (FormationController)target;
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
            normal = { textColor = new Color(1f, 0.7f, 0.5f) }
        };

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            normal = { textColor = Color.white }
        };

        slotStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(5, 5, 5, 5),
            margin = new RectOffset(2, 2, 2, 2),
            alignment = TextAnchor.MiddleCenter
        };

        unitStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(0, 0, 3, 3)
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

        DrawFormationTitle();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use formation system", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawFormationStatus();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawFormationSelector();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawFormationGrid();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawFormationControls();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawSlotDetails();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawAvailableUnits();
    }

    private void DrawFormationTitle()
    {
        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.7f, 0.5f, 0.5f);

        EditorGUILayout.BeginVertical(unitStyle);
        GUI.backgroundColor = previousBg;

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("📍 Formation System", titleStyle);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUIStyle subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };
        EditorGUILayout.LabelField("Tactical Unit Positioning", subtitleStyle);

        EditorGUILayout.EndVertical();
    }

    private void DrawFormationStatus()
    {
        EditorGUILayout.LabelField("Formation Status", headerStyle);

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.3f, 0.5f, 0.7f, 0.5f);
        EditorGUILayout.BeginVertical(unitStyle);
        GUI.backgroundColor = previousBg;

        if (controller.CurrentFormation != null)
        {
            var formation = controller.CurrentFormation;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📋 Formation:", GUILayout.Width(100));
            GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = formation.GetLayoutColor() }
            };
            EditorGUILayout.LabelField(formation.FormationName, nameStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📐 Layout:", GUILayout.Width(100));
            EditorGUILayout.LabelField(formation.Layout.ToString(), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("👥 Units:", GUILayout.Width(100));
            GUIStyle unitsStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.5f, 1f, 0.7f) }
            };
            EditorGUILayout.LabelField($"{controller.UnitsPlaced}/{controller.TotalSlots}", unitsStyle);
            EditorGUILayout.EndHorizontal();

            // Progress bar
            DrawUnitsProgressBar();
        }
        else
        {
            EditorGUILayout.LabelField("No formation selected", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawUnitsProgressBar()
    {
        Rect progressRect = EditorGUILayout.GetControlRect(false, 15);
        float percentage = controller.TotalSlots > 0 
            ? (float)controller.UnitsPlaced / controller.TotalSlots 
            : 0f;

        EditorGUI.DrawRect(progressRect, new Color(0.2f, 0.2f, 0.2f));

        Rect fillRect = progressRect;
        fillRect.width *= percentage;
        Color progressColor = percentage >= 1f ? new Color(0.5f, 1f, 0.5f) : new Color(0.5f, 0.9f, 1f);
        EditorGUI.DrawRect(fillRect, progressColor);

        GUIStyle progressTextStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold
        };
        GUI.Label(progressRect, $"{controller.UnitsPlaced}/{controller.TotalSlots} ({percentage * 100:F0}%)", progressTextStyle);
    }

    private void DrawFormationSelector()
    {
        EditorGUILayout.LabelField("Formation Selection", headerStyle);

        EditorGUILayout.BeginHorizontal();
        
        Color previousBg = GUI.backgroundColor;

        GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
        if (GUILayout.Button("◀ Previous", GUILayout.Height(30)))
        {
            controller.PreviousFormation();
        }

        GUI.backgroundColor = new Color(0.5f, 1f, 0.8f);
        if (GUILayout.Button("Next ▶", GUILayout.Height(30)))
        {
            controller.NextFormation();
        }

        GUI.backgroundColor = previousBg;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Formation type buttons
        for (int i = 0; i < controller.AvailableFormations.Count; i++)
        {
            var formation = controller.AvailableFormations[i];
            bool isCurrent = controller.CurrentFormation == formation;

            GUI.backgroundColor = isCurrent 
                ? formation.GetLayoutColor() 
                : new Color(0.5f, 0.5f, 0.5f, 0.5f);

            if (GUILayout.Button($"{(isCurrent ? "✓ " : "")}{formation.FormationName} ({formation.Layout})", GUILayout.Height(25)))
            {
                controller.SetFormation(i);
            }
        }

        GUI.backgroundColor = previousBg;
    }

    private void DrawFormationGrid()
    {
        EditorGUILayout.BeginHorizontal();
        showFormationGrid = EditorGUILayout.Foldout(showFormationGrid, "Formation Grid (Visual)", true, headerStyle);
        EditorGUILayout.EndHorizontal();

        if (!showFormationGrid || controller.CurrentFormation == null) return;

        var formation = controller.CurrentFormation;
    
        // Check if formation has slots
        if (formation.Slots == null || formation.Slots.Count == 0)
        {
            EditorGUILayout.HelpBox("Formation has no slots", MessageType.Warning);
            return;
        }

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
        EditorGUILayout.BeginVertical(unitStyle);
        GUI.backgroundColor = previousBg;

        // Get grid dimensions with safety check
        int maxX = 0;
        int maxY = 0;
    
        try
        {
            maxX = (int)formation.Slots.Max(s => s.GridPosition.x) + 1;
            maxY = (int)formation.Slots.Max(s => s.GridPosition.y) + 1;
        }
        catch (System.InvalidOperationException)
        {
            EditorGUILayout.HelpBox("Formation slots are empty or invalid", MessageType.Error);
            EditorGUILayout.EndVertical();
            return;
        }

        // Ensure minimum dimensions
        maxX = Mathf.Max(1, maxX);
        maxY = Mathf.Max(1, maxY);

        // Draw grid
        for (int y = 0; y < maxY; y++)
        {
            EditorGUILayout.BeginHorizontal();

            for (int x = 0; x < maxX; x++)
            {
                var slot = formation.Slots.FirstOrDefault(s => 
                    s.GridPosition.x == x && s.GridPosition.y == y);

                if (slot != null)
                {
                    DrawGridSlot(slot);
                }
                else
                {
                    DrawEmptyGridCell();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }


    private void DrawGridSlot(FormationSlot slot)
    {
        bool isSelected = selectedSlotId == slot.SlotId;
        
        Color slotColor = slot.GetRowColor();
        if (isSelected)
        {
            slotColor = new Color(slotColor.r * 1.5f, slotColor.g * 1.5f, slotColor.b * 1.5f, 0.8f);
        }
        else
        {
            slotColor.a = 0.5f;
        }

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = slotColor;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        string buttonText = slot.IsOccupied 
            ? $"{slot.GetPositionIcon()}\n{slot.OccupiedUnitId}"
            : $"{slot.GetPositionIcon()}\n[Empty]";

        if (GUILayout.Button(buttonText, buttonStyle, 
            GUILayout.Width(SLOT_SIZE), GUILayout.Height(SLOT_SIZE)))
        {
            selectedSlotId = slot.SlotId;
            
            // If unit selected, place it
            if (!string.IsNullOrEmpty(selectedUnitId))
            {
                if (slot.IsOccupied)
                {
                    controller.RemoveUnit(slot.SlotId);
                }
                controller.PlaceUnit(slot.SlotId, selectedUnitId);
                selectedUnitId = "";
            }
        }

        GUI.backgroundColor = previousBg;
    }

    private void DrawEmptyGridCell()
    {
        GUILayout.Box("", GUILayout.Width(SLOT_SIZE), GUILayout.Height(SLOT_SIZE));
    }

    private void DrawFormationControls()
    {
        EditorGUILayout.LabelField("Formation Controls", headerStyle);

        EditorGUILayout.BeginHorizontal();
        
        Color previousBg = GUI.backgroundColor;

        GUI.backgroundColor = new Color(0.5f, 1f, 0.7f);
        if (GUILayout.Button("🎲 Auto Arrange", GUILayout.Height(30)))
        {
            controller.AutoArrangeUnits();
            selectedSlotId = -1;
            selectedUnitId = "";
        }

        GUI.backgroundColor = new Color(1f, 0.9f, 0.5f);
        if (GUILayout.Button("🗑️ Clear All", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Clear Formation", 
                "Remove all units from formation?", "Yes", "No"))
            {
                controller.ClearFormation();
                selectedSlotId = -1;
            }
        }

        GUI.backgroundColor = previousBg;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
        if (GUILayout.Button("📊 Show Formation Info", GUILayout.Height(25)))
        {
            controller.ShowFormationInfo();
        }
        GUI.backgroundColor = previousBg;
    }

    private void DrawSlotDetails()
    {
        EditorGUILayout.BeginHorizontal();
        showSlotDetails = EditorGUILayout.Foldout(showSlotDetails, "Slot Details", true, headerStyle);
        EditorGUILayout.EndHorizontal();

        if (!showSlotDetails || controller.CurrentFormation == null) return;

        if (selectedSlotId >= 0)
        {
            var slot = controller.CurrentFormation.GetSlot(selectedSlotId);
            if (slot != null)
            {
                DrawSelectedSlotInfo(slot);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Click a slot in the grid to see details", MessageType.Info);
        }

        EditorGUILayout.Space(5);

        // All slots list
        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
        EditorGUILayout.BeginVertical(unitStyle);
        GUI.backgroundColor = previousBg;

        slotScrollPos = EditorGUILayout.BeginScrollView(slotScrollPos, GUILayout.MaxHeight(200));

        foreach (var slot in controller.CurrentFormation.Slots)
        {
            DrawSlotListItem(slot);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedSlotInfo(FormationSlot slot)
    {
        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = slot.GetRowColor();
        GUI.backgroundColor = new Color(GUI.backgroundColor.r, GUI.backgroundColor.g, GUI.backgroundColor.b, 0.4f);

        EditorGUILayout.BeginVertical(unitStyle);
        GUI.backgroundColor = previousBg;

        // Slot name
        GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = slot.GetRowColor() }
        };
        EditorGUILayout.LabelField($"{slot.GetPositionIcon()} {slot.SlotName}", nameStyle);

        // Position info
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Row: {slot.Row}", EditorStyles.miniLabel, GUILayout.Width(100));
        EditorGUILayout.LabelField($"Position: {slot.Position}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);

        // Bonuses
        EditorGUILayout.LabelField("Position Bonuses:", EditorStyles.miniLabel);
        
        GUIStyle bonusStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.7f, 1f, 0.7f) }
        };

        if (slot.AttackBonus != 0)
            EditorGUILayout.LabelField($"  ⚔️ Attack: {slot.AttackBonus*100:+0;-0}%", bonusStyle);
        if (slot.DefenseBonus != 0)
            EditorGUILayout.LabelField($"  🛡️ Defense: {slot.DefenseBonus*100:+0;-0}%", bonusStyle);
        if (slot.SpeedBonus != 0)
            EditorGUILayout.LabelField($"  ⚡ Speed: {slot.SpeedBonus*100:+0;-0}%", bonusStyle);
        if (slot.CritBonus != 0)
            EditorGUILayout.LabelField($"  🎯 Crit: {slot.CritBonus*100:+0;-0}%", bonusStyle);

        EditorGUILayout.Space(3);

        // Occupied unit
        if (slot.IsOccupied)
        {
            EditorGUILayout.LabelField($"👤 Unit: {slot.OccupiedUnitId}", EditorStyles.boldLabel);
            
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✖ Remove Unit", GUILayout.Height(25)))
            {
                controller.RemoveUnit(slot.SlotId);
            }
            GUI.backgroundColor = previousBg;
        }
        else
        {
            EditorGUILayout.LabelField("Empty slot", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSlotListItem(FormationSlot slot)
    {
        bool isSelected = selectedSlotId == slot.SlotId;
        
        Color bgColor = slot.GetRowColor();
        bgColor.a = isSelected ? 0.4f : 0.2f;

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = bgColor;

        EditorGUILayout.BeginVertical(slotStyle);
        GUI.backgroundColor = previousBg;

        EditorGUILayout.BeginHorizontal();

        GUIStyle slotNameStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal
        };
        EditorGUILayout.LabelField($"{slot.GetPositionIcon()} {slot.SlotName}", slotNameStyle, GUILayout.Width(120));

        string unit = slot.IsOccupied ? slot.OccupiedUnitId : "[Empty]";
        GUIStyle unitLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = slot.IsOccupied ? new Color(0.5f, 1f, 0.7f) : new Color(0.7f, 0.7f, 0.7f) }
        };
        EditorGUILayout.LabelField(unit, unitLabelStyle);

        if (GUILayout.Button("Select", GUILayout.Width(50)))
        {
            selectedSlotId = slot.SlotId;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawAvailableUnits()
    {
        EditorGUILayout.BeginHorizontal();
        showAvailableUnits = EditorGUILayout.Foldout(showAvailableUnits, 
            $"Available Units ({controller.AvailableUnits.Count})", true, headerStyle);
        EditorGUILayout.EndHorizontal();

        if (!showAvailableUnits) return;

        if (controller.AvailableUnits.Count == 0)
        {
            EditorGUILayout.HelpBox("No units available", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Click to select unit, then click slot to place", EditorStyles.miniLabel);

        EditorGUILayout.Space(5);

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.3f, 0.5f, 0.4f, 0.5f);
        EditorGUILayout.BeginVertical(unitStyle);
        GUI.backgroundColor = previousBg;

        unitScrollPos = EditorGUILayout.BeginScrollView(unitScrollPos, GUILayout.MaxHeight(200));

        foreach (var unitId in controller.AvailableUnits)
        {
            bool isSelected = selectedUnitId == unitId;
            
            GUI.backgroundColor = isSelected 
                ? new Color(0.5f, 1f, 0.7f) 
                : new Color(0.5f, 0.5f, 0.5f);

            if (GUILayout.Button($"{(isSelected ? "✓ " : "")}👤 {unitId}", GUILayout.Height(25)))
            {
                selectedUnitId = isSelected ? "" : unitId;
            }
        }

        GUI.backgroundColor = previousBg;

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        if (!string.IsNullOrEmpty(selectedUnitId))
        {
            EditorGUILayout.HelpBox($"Selected: {selectedUnitId}\nClick a slot to place this unit", MessageType.Info);
        }
    }

    private void DrawSeparator()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }
}

#endif
