#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using GameSystems.Inventory;
using System.Linq;

[CustomEditor(typeof(InventoryIteratorController))]
public class InventoryIteratorControllerEditor : Editor
{
    private InventoryIteratorController controller;
    private GUIStyle titleStyle;
    private GUIStyle headerStyle;
    private GUIStyle itemStyle;
    private GUIStyle currentItemStyle;
    private GUIStyle statsStyle;
    private bool isInitialized;

    private bool showInventory = true;
    private bool showStatistics = true;
    private bool showFilters = false;
    private Vector2 inventoryScrollPos;

    private ItemType filterType = ItemType.Weapon;
    private ItemRarity filterRarity = ItemRarity.Common;

    private void OnEnable()
    {
        controller = (InventoryIteratorController)target;
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
            normal = { textColor = new Color(1f, 0.8f, 0.4f) }
        };

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            normal = { textColor = Color.white }
        };

        itemStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(0, 0, 2, 2)
        };

        currentItemStyle = new GUIStyle(itemStyle)
        {
            fontStyle = FontStyle.Bold
        };

        statsStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8)
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

        // Title
        DrawInventoryTitle();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use inventory controls", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Statistics
        DrawStatistics();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Current Item Display
        DrawCurrentItem();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Navigation Controls
        DrawNavigationControls();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Item Actions
        DrawItemActions();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Sorting Controls
        DrawSortingControls();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Filters
        DrawFilters();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Inventory List
        DrawInventoryList();
    }

    private void DrawInventoryTitle()
    {
        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.7f, 0.5f, 0.3f, 0.5f);

        EditorGUILayout.BeginVertical(itemStyle);
        GUI.backgroundColor = previousBg;

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"📦 {controller.InventoryName}", titleStyle);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        if (Application.isPlaying)
        {
            GUIStyle slotsStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
            EditorGUILayout.LabelField(
                $"{controller.InventoryData.Items.Count} / {controller.MaxSlots} slots", 
                slotsStyle
            );
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawStatistics()
    {
        EditorGUILayout.BeginHorizontal();
        showStatistics = EditorGUILayout.Foldout(showStatistics, "Statistics", true, headerStyle);
        EditorGUILayout.EndHorizontal();

        if (!showStatistics) return;

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.2f, 0.3f, 0.4f, 0.5f);
        EditorGUILayout.BeginVertical(statsStyle);
        GUI.backgroundColor = previousBg;

        // Total items
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("📦 Total Items:", GUILayout.Width(140));
        EditorGUILayout.LabelField($"{controller.InventoryData.Items.Count}/{controller.MaxSlots}", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        // Total value
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("💰 Total Value:", GUILayout.Width(140));
        GUIStyle valueStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = new Color(1f, 0.8f, 0.3f) }
        };
        EditorGUILayout.LabelField($"{controller.InventoryData.GetTotalValue()} Gold", valueStyle);
        EditorGUILayout.EndHorizontal();

        // Iterations
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🔁 Iterations:", GUILayout.Width(140));
        EditorGUILayout.LabelField(controller.InventoryData.GetTotalIterations().ToString(), EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        // Item counts by type
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Items by Type:", EditorStyles.miniLabel);
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            int count = controller.InventoryData.GetItemsByType(type).Count;
            if (count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  {type}:", GUILayout.Width(130));
                EditorGUILayout.LabelField(count.ToString(), EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawCurrentItem()
    {
        EditorGUILayout.LabelField("Currently Selected", headerStyle);

        var item = controller.CurrentItem;
        if (item == null)
        {
            EditorGUILayout.HelpBox("No item selected", MessageType.Info);
            return;
        }

        Color bgColor = item.GetRarityColor();
        bgColor.a = 0.4f;

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = bgColor;

        EditorGUILayout.BeginVertical(currentItemStyle);
        GUI.backgroundColor = previousBg;

        // Item name with type icon
        EditorGUILayout.BeginHorizontal();
        GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = item.GetRarityColor() }
        };
        EditorGUILayout.LabelField($"{item.GetTypeIcon()} {item.ItemName}", nameStyle);
        
        if (item.IsEquipped)
        {
            GUIStyle equippedStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.green },
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField("[EQUIPPED]", equippedStyle, GUILayout.Width(80));
        }
        
        EditorGUILayout.EndHorizontal();

        // Description
        GUIStyle descStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontStyle = FontStyle.Italic,
            wordWrap = true
        };
        EditorGUILayout.LabelField(item.Description, descStyle);

        EditorGUILayout.Space(3);

        // Details
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{item.GetRarityIcon()} {item.Rarity}", EditorStyles.miniLabel, GUILayout.Width(100));
        EditorGUILayout.LabelField($"Type: {item.ItemType}", EditorStyles.miniLabel, GUILayout.Width(120));
        if (item.MaxStack > 1)
        {
            EditorGUILayout.LabelField($"Qty: {item.Quantity}/{item.MaxStack}", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndHorizontal();

        // Value
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"💰 Value: {item.Value} Gold", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawNavigationControls()
    {
        EditorGUILayout.LabelField("Navigation", headerStyle);

        // Basic navigation
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("⏮ First", GUILayout.Height(30)))
        {
            controller.First();
        }
        if (GUILayout.Button("◀ Prev", GUILayout.Height(30)))
        {
            controller.Previous();
        }
        if (GUILayout.Button("Next ▶", GUILayout.Height(30)))
        {
            controller.Next();
        }
        if (GUILayout.Button("Last ⏭", GUILayout.Height(30)))
        {
            controller.Last();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Smart navigation
        EditorGUILayout.BeginHorizontal();
        Color previousBg = GUI.backgroundColor;
        
        GUI.backgroundColor = new Color(0.5f, 1f, 0.7f);
        if (GUILayout.Button("🧪 Next Consumable", GUILayout.Height(25)))
        {
            controller.NextConsumable();
        }

        GUI.backgroundColor = previousBg;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawItemActions()
    {
        EditorGUILayout.LabelField("Item Actions", headerStyle);

        EditorGUILayout.BeginHorizontal();
        
        Color previousBg = GUI.backgroundColor;

        // Use button
        GUI.backgroundColor = new Color(0.5f, 1f, 0.7f);
        if (GUILayout.Button("✨ Use", GUILayout.Height(30)))
        {
            controller.UseCurrentItem();
        }

        // Equip button
        GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
        if (GUILayout.Button("⚔️ Equip", GUILayout.Height(30)))
        {
            controller.EquipCurrentItem();
        }

        // Drop button
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("❌ Drop", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Drop Item", "Drop current item?", "Yes", "No"))
            {
                controller.DropCurrentItem();
            }
        }

        GUI.backgroundColor = previousBg;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSortingControls()
    {
        EditorGUILayout.LabelField("Sorting", headerStyle);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔤 Name", GUILayout.Height(25)))
        {
            controller.SortByName();
        }
        if (GUILayout.Button("📁 Type", GUILayout.Height(25)))
        {
            controller.SortByType();
        }
        if (GUILayout.Button("⭐ Rarity", GUILayout.Height(25)))
        {
            controller.SortByRarity();
        }
        if (GUILayout.Button("💰 Value", GUILayout.Height(25)))
        {
            controller.SortByValue();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawFilters()
    {
        EditorGUILayout.BeginHorizontal();
        showFilters = EditorGUILayout.Foldout(showFilters, "Filters", true, headerStyle);
        EditorGUILayout.EndHorizontal();

        if (!showFilters) return;

        EditorGUILayout.Space(5);

        // Filter by type
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("By Type:", GUILayout.Width(60));
        filterType = (ItemType)EditorGUILayout.EnumPopup(filterType);
        if (GUILayout.Button("Show", GUILayout.Width(60)))
        {
            ShowFilteredByType(filterType);
        }
        EditorGUILayout.EndHorizontal();

        // Filter by rarity
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("By Rarity:", GUILayout.Width(60));
        filterRarity = (ItemRarity)EditorGUILayout.EnumPopup(filterRarity);
        if (GUILayout.Button("Show", GUILayout.Width(60)))
        {
            ShowFilteredByRarity(filterRarity);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawInventoryList()
    {
        EditorGUILayout.BeginHorizontal();
        showInventory = EditorGUILayout.Foldout(showInventory, 
            $"Inventory ({controller.InventoryData.Items.Count} items)", true, headerStyle);
        EditorGUILayout.EndHorizontal();

        if (!showInventory) return;

        if (controller.InventoryData.Items.Count == 0)
        {
            EditorGUILayout.HelpBox("Inventory is empty", MessageType.Info);
            return;
        }

        int currentIndex = controller.InventoryData.GetCurrentIndex();

        inventoryScrollPos = EditorGUILayout.BeginScrollView(inventoryScrollPos, GUILayout.MaxHeight(400));

        for (int i = 0; i < controller.InventoryData.Items.Count; i++)
        {
            DrawInventoryItem(controller.InventoryData.Items[i], i, i == currentIndex);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawInventoryItem(Item item, int index, bool isCurrent)
    {
        Color bgColor;
        if (isCurrent)
        {
            bgColor = item.GetRarityColor();
            bgColor.a = 0.4f;
        }
        else
        {
            bgColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
        }

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = bgColor;

        EditorGUILayout.BeginVertical(isCurrent ? currentItemStyle : itemStyle);
        GUI.backgroundColor = previousBg;

        EditorGUILayout.BeginHorizontal();

        // Index and icon
        string prefix = isCurrent ? "➤" : "•";
        GUIStyle indexStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal
        };
        EditorGUILayout.LabelField($"{prefix} [{index}]", indexStyle, GUILayout.Width(45));

        // Item info
        GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal,
            normal = { textColor = isCurrent ? item.GetRarityColor() : Color.white }
        };
        EditorGUILayout.LabelField(item.ToString(), nameStyle);

        // Equipped badge
        if (item.IsEquipped)
        {
            GUIStyle equippedStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.green },
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField("[E]", equippedStyle, GUILayout.Width(25));
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void ShowFilteredByType(ItemType type)
    {
        var items = controller.InventoryData.GetItemsByType(type);
        Debug.Log($"\n<color=cyan>═══ {type} Items ({items.Count}) ═══</color>");
        foreach (var item in items)
        {
            Debug.Log($"  {item}");
        }
        Debug.Log("");
    }

    private void ShowFilteredByRarity(ItemRarity rarity)
    {
        var items = controller.InventoryData.GetItemsByRarity(rarity);
        Debug.Log($"\n<color=cyan>═══ {rarity} Items ({items.Count}) ═══</color>");
        foreach (var item in items)
        {
            Debug.Log($"  {item}");
        }
        Debug.Log("");
    }

    private void DrawSeparator()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }
}

#endif
