#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;
using GameSystems.Equipment;

[CustomEditor(typeof(EquipmentController))]
public class EquipmentControllerEditor : Editor
{
    private EquipmentController controller;
    private GUIStyle titleStyle;
    private GUIStyle headerStyle;
    private GUIStyle equipmentStyle;
    private GUIStyle currentEquipmentStyle;
    private GUIStyle slotStyle;
    private bool isInitialized;

    private bool showEquipment = true;
    private bool showBySlot = true;
    private bool showBySet = false;
    private bool showEquippedOnly = false;
    private bool useSlotFilter = false;
    private bool useRarityFilter = false;
    private bool useSetFilter = false;
    private Vector2 equipmentScrollPos;

    private EquipmentSlot filterSlot = EquipmentSlot.Weapon;
    private EquipmentRarity filterRarity = EquipmentRarity.Legendary;
    private string filterSetName = "";
    private string[] availableSetNames = new string[0];
    private int selectedSetIndex = 0;

    private void OnEnable()
    {
        controller = (EquipmentController)target;
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
            normal = { textColor = new Color(1f, 0.7f, 0.3f) }
        };

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            normal = { textColor = Color.white }
        };

        equipmentStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(0, 0, 3, 3)
        };

        currentEquipmentStyle = new GUIStyle(equipmentStyle)
        {
            fontStyle = FontStyle.Bold
        };

        slotStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(8, 8, 8, 8),
            margin = new RectOffset(2, 2, 2, 2)
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

        DrawEquipmentSystemTitle();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use equipment controls", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawTotalStats();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawEquipmentSlots();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawCurrentEquipmentDetailed();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawNavigationControls();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawEquipmentActions();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawSortingAndFilters();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawSetBonuses();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawAllEquipment();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawSlotView();

        EditorGUILayout.Space(10);
        DrawSeparator();

        DrawSetView();
    }

    private void DrawEquipmentSystemTitle()
    {
        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.8f, 0.5f, 0.2f, 0.5f);

        EditorGUILayout.BeginVertical(equipmentStyle);
        GUI.backgroundColor = previousBg;

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"⚔️ {controller.ControllerName}", titleStyle);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUIStyle levelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.9f, 0.3f) }
        };
        EditorGUILayout.LabelField($"Player Level {controller.PlayerLevel}", levelStyle);

        EditorGUILayout.EndVertical();
    }

    private void DrawTotalStats()
    {
        EditorGUILayout.LabelField("Total Equipment Stats", headerStyle);

        var stats = controller.GetTotalStatsWithSetBonuses();

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.3f, 0.6f, 0.3f, 0.5f);
        EditorGUILayout.BeginVertical(equipmentStyle);
        GUI.backgroundColor = previousBg;

        GUIStyle statStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = new Color(0.5f, 1f, 0.5f) },
            fontStyle = FontStyle.Bold
        };

        if (stats.Attack > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("⚔️ Attack:", GUILayout.Width(100));
            EditorGUILayout.LabelField($"+{stats.Attack}", statStyle);
            EditorGUILayout.EndHorizontal();
        }

        if (stats.Defense > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🛡️ Defense:", GUILayout.Width(100));
            EditorGUILayout.LabelField($"+{stats.Defense}", statStyle);
            EditorGUILayout.EndHorizontal();
        }

        if (stats.Health > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("❤️ Health:", GUILayout.Width(100));
            EditorGUILayout.LabelField($"+{stats.Health}", statStyle);
            EditorGUILayout.EndHorizontal();
        }

        if (stats.Mana > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("💙 Mana:", GUILayout.Width(100));
            EditorGUILayout.LabelField($"+{stats.Mana}", statStyle);
            EditorGUILayout.EndHorizontal();
        }

        if (stats.Speed > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("⚡ Speed:", GUILayout.Width(100));
            EditorGUILayout.LabelField($"+{stats.Speed}", statStyle);
            EditorGUILayout.EndHorizontal();
        }

        if (stats.CritRate > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🎯 Crit Rate:", GUILayout.Width(100));
            EditorGUILayout.LabelField($"+{stats.CritRate * 100:F1}%", statStyle);
            EditorGUILayout.EndHorizontal();
        }

        if (stats.CritDamage > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("💥 Crit Damage:", GUILayout.Width(100));
            EditorGUILayout.LabelField($"+{stats.CritDamage * 100:F1}%", statStyle);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawEquipmentSlots()
    {
        EditorGUILayout.LabelField("Equipment Slots", headerStyle);

        Color previousBg = GUI.backgroundColor;

        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        DrawEquipmentSlot(EquipmentSlot.Weapon);
        DrawEquipmentSlot(EquipmentSlot.Helmet);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        DrawEquipmentSlot(EquipmentSlot.Armor);
        DrawEquipmentSlot(EquipmentSlot.Gloves);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        DrawEquipmentSlot(EquipmentSlot.Boots);
        DrawEquipmentSlot(EquipmentSlot.Ring);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        DrawEquipmentSlot(EquipmentSlot.Necklace);
        DrawEquipmentSlot(EquipmentSlot.Accessory);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        GUI.backgroundColor = previousBg;
    }

    private void DrawEquipmentSlot(EquipmentSlot slot)
    {
        var equippedItem = controller.EquipmentData.GetEquippedItemInSlot(slot);

        Color previousBg = GUI.backgroundColor;
        
        if (equippedItem != null)
        {
            GUI.backgroundColor = equippedItem.GetRarityColor();
            GUI.backgroundColor = new Color(GUI.backgroundColor.r, GUI.backgroundColor.g, GUI.backgroundColor.b, 0.4f);
        }
        else
        {
            GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
        }

        EditorGUILayout.BeginVertical(slotStyle, GUILayout.Width(150), GUILayout.Height(80));
        GUI.backgroundColor = previousBg;

        GUIStyle slotNameStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        EditorGUILayout.LabelField(slot.ToString(), slotNameStyle);

        EditorGUILayout.Space(3);

        if (equippedItem != null)
        {
            GUIStyle itemNameStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = equippedItem.GetRarityColor() }
            };
            EditorGUILayout.LabelField($"{equippedItem.GetSlotIcon()}", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField(equippedItem.ItemName, itemNameStyle);

            if (equippedItem.EnhancementLevel > 0)
            {
                GUIStyle enhanceStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.5f, 1f, 0.9f) }
                };
                EditorGUILayout.LabelField($"+{equippedItem.EnhancementLevel}", enhanceStyle);
            }
        }
        else
        {
            GUIStyle emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 20
            };
            EditorGUILayout.LabelField($"{GetSlotIconString(slot)}", emptyStyle);
            EditorGUILayout.LabelField("Empty", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawCurrentEquipmentDetailed()
    {
        EditorGUILayout.LabelField("Currently Selected Equipment", headerStyle);

        var item = controller.CurrentItem;
        if (item == null)
        {
            EditorGUILayout.HelpBox("No equipment selected", MessageType.Info);
            return;
        }

        Color bgColor = item.GetRarityColor();
        bgColor.a = 0.3f;

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = bgColor;

        EditorGUILayout.BeginVertical(currentEquipmentStyle);
        GUI.backgroundColor = previousBg;

        EditorGUILayout.BeginHorizontal();
        
        GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            normal = { textColor = item.GetRarityColor() }
        };
        EditorGUILayout.LabelField($"{item.GetSlotIcon()} {item.ItemName}", nameStyle);

        if (item.IsEquipped)
        {
            GUIStyle equippedStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.5f, 1f, 0.5f) },
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField("[EQUIPPED]", equippedStyle, GUILayout.Width(80));
        }

        EditorGUILayout.EndHorizontal();

        GUIStyle descStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontStyle = FontStyle.Italic,
            wordWrap = true
        };
        EditorGUILayout.LabelField(item.Description, descStyle);

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        
        GUIStyle rarityStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = item.GetRarityColor() },
            fontStyle = FontStyle.Bold
        };
        EditorGUILayout.LabelField($"{item.GetRarityIcon()} {item.Rarity}", rarityStyle, GUILayout.Width(120));

        EditorGUILayout.LabelField($"Lv.{item.RequiredLevel}", EditorStyles.miniLabel, GUILayout.Width(50));

        if (item.EnhancementLevel > 0)
        {
            GUIStyle enhanceStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.5f, 1f, 0.9f) },
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField($"+{item.EnhancementLevel}", enhanceStyle, GUILayout.Width(40));
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        DrawItemStats(item);

        EditorGUILayout.Space(5);

        DrawDurabilityBar(item);

        EditorGUILayout.Space(5);

        if (item.IsPartOfSet())
        {
            DrawSetInfo(item);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);
        DrawEquipmentActionButtons(item);
    }

    private void DrawItemStats(EquipmentItem item)
    {
        EditorGUILayout.LabelField("Stats:", EditorStyles.miniLabel);

        GUIStyle statStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.7f, 1f, 0.7f) }
        };

        if (item.AttackBonus > 0)
            EditorGUILayout.LabelField($"  ⚔️ Attack: +{item.AttackBonus}", statStyle);
        
        if (item.DefenseBonus > 0)
            EditorGUILayout.LabelField($"  🛡️ Defense: +{item.DefenseBonus}", statStyle);
        
        if (item.HealthBonus > 0)
            EditorGUILayout.LabelField($"  ❤️ Health: +{item.HealthBonus}", statStyle);
        
        if (item.ManaBonus > 0)
            EditorGUILayout.LabelField($"  💙 Mana: +{item.ManaBonus}", statStyle);
        
        if (item.SpeedBonus > 0)
            EditorGUILayout.LabelField($"  ⚡ Speed: +{item.SpeedBonus}", statStyle);
        
        if (item.CritRateBonus > 0)
            EditorGUILayout.LabelField($"  🎯 Crit Rate: +{item.CritRateBonus * 100:F1}%", statStyle);
        
        if (item.CritDamageBonus > 0)
            EditorGUILayout.LabelField($"  💥 Crit Damage: +{item.CritDamageBonus * 100:F1}%", statStyle);
    }

    private void DrawDurabilityBar(EquipmentItem item)
    {
        EditorGUILayout.LabelField("Durability:", EditorStyles.miniLabel);
        
        Rect durabilityRect = EditorGUILayout.GetControlRect(false, 15);
        float percentage = item.GetDurabilityPercentage();

        EditorGUI.DrawRect(durabilityRect, new Color(0.2f, 0.2f, 0.2f));

        Rect fillRect = durabilityRect;
        fillRect.width *= percentage;
        Color durabilityColor = percentage > 0.5f ? new Color(0.5f, 1f, 0.5f) : 
                               percentage > 0.25f ? new Color(1f, 0.9f, 0.3f) : 
                               new Color(1f, 0.3f, 0.3f);
        EditorGUI.DrawRect(fillRect, durabilityColor);

        GUIStyle durabilityTextStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        GUI.Label(durabilityRect, $"{item.CurrentDurability}/{item.MaxDurability}", durabilityTextStyle);
    }

    private void DrawSetInfo(EquipmentItem item)
    {
        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.8f, 0.5f, 1f, 0.3f);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = previousBg;

        GUIStyle setStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.9f, 0.7f, 1f) },
            fontStyle = FontStyle.Bold
        };
        EditorGUILayout.LabelField($"✨ Set: {item.SetName}", setStyle);

        EditorGUILayout.EndVertical();
    }

    private void DrawEquipmentActionButtons(EquipmentItem item)
    {
        EditorGUILayout.BeginHorizontal();
        
        Color previousBg = GUI.backgroundColor;

        GUI.enabled = !item.IsEquipped;
        GUI.backgroundColor = new Color(0.5f, 1f, 0.7f);
        if (GUILayout.Button("⚔️ Equip", GUILayout.Height(30)))
        {
            controller.EquipCurrentItem();
        }

        GUI.enabled = item.IsEquipped;
        GUI.backgroundColor = new Color(1f, 0.9f, 0.5f);
        if (GUILayout.Button("↓ Unequip", GUILayout.Height(30)))
        {
            controller.UnequipCurrentItem();
        }

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = item.EnhancementLevel < item.MaxEnhancement;
        GUI.backgroundColor = new Color(0.5f, 0.9f, 1f);
        if (GUILayout.Button($"⬆ Enhance (+{item.EnhancementLevel})", GUILayout.Height(25)))
        {
            controller.EnhanceCurrentItem();
        }

        GUI.enabled = item.CurrentDurability < item.MaxDurability;
        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
        if (GUILayout.Button("🔧 Repair", GUILayout.Height(25)))
        {
            controller.RepairCurrentItem();
        }

        GUI.enabled = true;
        GUI.backgroundColor = previousBg;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawNavigationControls()
    {
        EditorGUILayout.LabelField("Navigation", headerStyle);

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
    }

    private void DrawEquipmentActions()
    {
        EditorGUILayout.LabelField("Quick Actions", headerStyle);

        EditorGUILayout.BeginHorizontal();
        
        Color previousBg = GUI.backgroundColor;

        GUI.backgroundColor = new Color(0.5f, 0.9f, 1f);
        if (GUILayout.Button("📊 Show Equipment Info", GUILayout.Height(25)))
        {
            controller.ShowEquipmentInfo();
        }

        GUI.backgroundColor = previousBg;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSortingAndFilters()
    {
        EditorGUILayout.LabelField("Sorting & Filters", headerStyle);

        EditorGUILayout.LabelField("Sort by:", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("💪 Power", GUILayout.Height(25)))
        {
            controller.SortByPowerScore();
        }
        if (GUILayout.Button("⭐ Rarity", GUILayout.Height(25)))
        {
            controller.SortByRarity();
        }
        if (GUILayout.Button("⬆ Enhancement", GUILayout.Height(25)))
        {
            controller.SortByEnhancement();
        }
        if (GUILayout.Button("📍 Slot", GUILayout.Height(25)))
        {
            controller.SortBySlot();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Quick Filters:", EditorStyles.miniLabel);
        showEquippedOnly = GUILayout.Toggle(showEquippedOnly, "Equipped Only");

        EditorGUILayout.Space(5);

        DrawSlotFilter();

        EditorGUILayout.Space(5);

        DrawRarityFilter();

        EditorGUILayout.Space(5);

        DrawSetFilter();
    }

    private void DrawSlotFilter()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        useSlotFilter = EditorGUILayout.Toggle(useSlotFilter, GUILayout.Width(20));
        EditorGUILayout.LabelField("Filter by Slot:", EditorStyles.miniLabel, GUILayout.Width(100));
        
        GUI.enabled = useSlotFilter;
        filterSlot = (EquipmentSlot)EditorGUILayout.EnumPopup(filterSlot);
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        if (useSlotFilter)
        {
            int count = controller.EquipmentData.GetItemsBySlot(filterSlot).Count;
            GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.5f, 1f, 0.7f) },
                fontStyle = FontStyle.Italic
            };
            EditorGUILayout.LabelField($"  → {count} item(s) found", countStyle);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawRarityFilter()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        useRarityFilter = EditorGUILayout.Toggle(useRarityFilter, GUILayout.Width(20));
        EditorGUILayout.LabelField("Filter by Rarity:", EditorStyles.miniLabel, GUILayout.Width(100));
        
        GUI.enabled = useRarityFilter;
        filterRarity = (EquipmentRarity)EditorGUILayout.EnumPopup(filterRarity);
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        if (useRarityFilter)
        {
            int count = controller.EquipmentData.GetItemsByRarity(filterRarity).Count;
            GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 0.9f, 0.5f) },
                fontStyle = FontStyle.Italic
            };
            EditorGUILayout.LabelField($"  → {count} item(s) found", countStyle);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSetFilter()
    {
        UpdateAvailableSetNames();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        useSetFilter = EditorGUILayout.Toggle(useSetFilter, GUILayout.Width(20));
        EditorGUILayout.LabelField("Filter by Set:", EditorStyles.miniLabel, GUILayout.Width(100));
        
        GUI.enabled = useSetFilter;
        
        if (availableSetNames.Length > 0)
        {
            selectedSetIndex = EditorGUILayout.Popup(selectedSetIndex, availableSetNames);
            filterSetName = availableSetNames[selectedSetIndex];
        }
        else
        {
            EditorGUILayout.LabelField("No sets available", EditorStyles.miniLabel);
        }
        
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        if (useSetFilter && !string.IsNullOrEmpty(filterSetName))
        {
            int count = controller.EquipmentData.GetItemsBySet(filterSetName).Count;
            int equippedCount = controller.EquipmentData.GetItemsBySet(filterSetName)
                .Count(item => item.IsEquipped);
            
            GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.9f, 0.7f, 1f) },
                fontStyle = FontStyle.Italic
            };
            EditorGUILayout.LabelField($"  → {count} item(s) found ({equippedCount} equipped)", countStyle);
            
            if (controller.EquipmentSets.ContainsKey(filterSetName))
            {
                var set = controller.EquipmentSets[filterSetName];
                GUIStyle setInfoStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.8f, 0.9f, 1f) },
                    fontStyle = FontStyle.Italic
                };
                EditorGUILayout.LabelField($"  ✨ Set Pieces: {equippedCount}/{set.TotalPieces}", setInfoStyle);
            }
        }

        EditorGUILayout.EndVertical();

        if (useSetFilter && availableSetNames.Length > 0)
        {
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Quick Select:", EditorStyles.miniLabel);
            
            DrawQuickSetButtons();
        }
    }

    private void UpdateAvailableSetNames()
    {
        var setNames = new System.Collections.Generic.HashSet<string>();
        
        foreach (var item in controller.EquipmentData.Items)
        {
            if (item.IsPartOfSet())
            {
                setNames.Add(item.SetName);
            }
        }
        
        availableSetNames = setNames.ToArray();
        
        if (selectedSetIndex >= availableSetNames.Length)
        {
            selectedSetIndex = 0;
        }
    }

    private void DrawQuickSetButtons()
    {
        Color previousBg = GUI.backgroundColor;
        
        int buttonsPerRow = 2;
        int buttonCount = 0;
        
        EditorGUILayout.BeginHorizontal();
        
        foreach (var setName in availableSetNames)
        {
            Color setColor = GetSetColor(setName);
            GUI.backgroundColor = setColor;
            
            if (GUILayout.Button($"✨ {setName}", GUILayout.Height(20)))
            {
                selectedSetIndex = System.Array.IndexOf(availableSetNames, setName);
                filterSetName = setName;
            }
            
            buttonCount++;
            
            if (buttonCount >= buttonsPerRow)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                buttonCount = 0;
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUI.backgroundColor = previousBg;
    }

    private Color GetSetColor(string setName)
    {
        return setName switch
        {
            "Dragon's Fury" => new Color(1f, 0.3f, 0.3f, 0.8f),
            "Knight's Honor" => new Color(0.7f, 0.7f, 1f, 0.8f),
            "Shadow Strike" => new Color(0.5f, 0.3f, 0.6f, 0.8f),
            _ => new Color(0.8f, 0.5f, 1f, 0.8f)
        };
    }

    private void DrawSetBonuses()
    {
        var setCounts = controller.EquipmentData.GetEquippedSetCounts();
        if (setCounts.Count == 0) return;

        EditorGUILayout.LabelField("✨ Active Set Bonuses", headerStyle);

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.7f, 0.5f, 1f, 0.3f);

        foreach (var kvp in setCounts)
        {
            string setName = kvp.Key;
            int equippedCount = kvp.Value;
            
            if (controller.EquipmentSets.ContainsKey(setName))
            {
                var set = controller.EquipmentSets[setName];
                
                EditorGUILayout.BeginVertical(equipmentStyle);
                
                GUIStyle setNameStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = new Color(0.9f, 0.7f, 1f) }
                };
                EditorGUILayout.LabelField($"✨ {setName} ({equippedCount}/{set.TotalPieces})", setNameStyle);

                var activeBonuses = set.GetActiveBonuses(equippedCount);
                
                foreach (var bonus in activeBonuses)
                {
                    GUIStyle bonusStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(0.7f, 1f, 0.7f) }
                    };
                    EditorGUILayout.LabelField($"  ✓ {bonus}", bonusStyle);
                }
                
                EditorGUILayout.EndVertical();
            }
        }

        GUI.backgroundColor = previousBg;
    }

    private void DrawAllEquipment()
    {
        EditorGUILayout.BeginHorizontal();
        
        string title = GetFilteredTitle();
        showEquipment = EditorGUILayout.Foldout(showEquipment, title, true, headerStyle);
        
        EditorGUILayout.EndHorizontal();

        if (!showEquipment) return;

        if (controller.EquipmentData.Items.Count == 0)
        {
            EditorGUILayout.HelpBox("No equipment available", MessageType.Info);
            return;
        }

        int currentIndex = controller.EquipmentData.CurrentIndex;

        var displayItems = controller.EquipmentData.Items.AsEnumerable();
        
        if (showEquippedOnly)
            displayItems = displayItems.Where(item => item.IsEquipped);
        
        if (useSlotFilter)
            displayItems = displayItems.Where(item => item.Slot == filterSlot);
        
        if (useRarityFilter)
            displayItems = displayItems.Where(item => item.Rarity == filterRarity);
        
        if (useSetFilter && !string.IsNullOrEmpty(filterSetName))
            displayItems = displayItems.Where(item => item.SetName == filterSetName);

        var itemList = displayItems.ToList();

        if (itemList.Count == 0)
        {
            EditorGUILayout.HelpBox("No equipment matches current filters", MessageType.Info);
            ShowActiveFilters();
            return;
        }

        if (showEquippedOnly || useSlotFilter || useRarityFilter || useSetFilter)
        {
            ShowActiveFilters();
        }

        equipmentScrollPos = EditorGUILayout.BeginScrollView(equipmentScrollPos, GUILayout.MaxHeight(350));

        for (int i = 0; i < controller.EquipmentData.Items.Count; i++)
        {
            var item = controller.EquipmentData.Items[i];
            
            if (showEquippedOnly && !item.IsEquipped) continue;
            if (useSlotFilter && item.Slot != filterSlot) continue;
            if (useRarityFilter && item.Rarity != filterRarity) continue;
            if (useSetFilter && !string.IsNullOrEmpty(filterSetName) && item.SetName != filterSetName) continue;

            bool isCurrent = i == currentIndex;
            DrawEquipmentListItem(item, i, isCurrent);
        }

        EditorGUILayout.EndScrollView();
    }

    private string GetFilteredTitle()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("All Equipment");
        
        int totalCount = controller.EquipmentData.Items.Count;
        
        var displayItems = controller.EquipmentData.Items.AsEnumerable();
        if (showEquippedOnly)
            displayItems = displayItems.Where(item => item.IsEquipped);
        if (useSlotFilter)
            displayItems = displayItems.Where(item => item.Slot == filterSlot);
        if (useRarityFilter)
            displayItems = displayItems.Where(item => item.Rarity == filterRarity);
        if (useSetFilter && !string.IsNullOrEmpty(filterSetName))
            displayItems = displayItems.Where(item => item.SetName == filterSetName);
        
        int filteredCount = displayItems.Count();
        
        sb.Append($" ({filteredCount}");
        if (filteredCount != totalCount)
        {
            sb.Append($"/{totalCount}");
        }
        sb.Append(")");
        
        return sb.ToString();
    }

    private void ShowActiveFilters()
    {
        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.3f, 0.5f, 0.7f, 0.5f);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = previousBg;
        
        GUIStyle filterStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontStyle = FontStyle.Italic,
            normal = { textColor = new Color(0.7f, 0.9f, 1f) }
        };
        
        EditorGUILayout.LabelField("Active Filters:", EditorStyles.boldLabel);
        
        if (showEquippedOnly)
            EditorGUILayout.LabelField("  • Equipped Only", filterStyle);
        
        if (useSlotFilter)
            EditorGUILayout.LabelField($"  • Slot: {GetSlotIconString(filterSlot)} {filterSlot}", filterStyle);
        
        if (useRarityFilter)
            EditorGUILayout.LabelField($"  • Rarity: {GetRarityIconString(filterRarity)} {filterRarity}", filterStyle);
        
        if (useSetFilter && !string.IsNullOrEmpty(filterSetName))
        {
            GUIStyle setFilterStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.9f, 0.7f, 1f) }
            };
            EditorGUILayout.LabelField($"  • Set: ✨ {filterSetName}", setFilterStyle);
        }
        
        EditorGUILayout.Space(3);
        
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("✖ Clear All Filters", GUILayout.Height(20)))
        {
            showEquippedOnly = false;
            useSlotFilter = false;
            useRarityFilter = false;
            useSetFilter = false;
        }
        GUI.backgroundColor = previousBg;
        
        EditorGUILayout.EndVertical();
    }

    private void DrawEquipmentListItem(EquipmentItem item, int index, bool isCurrent)
    {
        Color bgColor = item.GetRarityColor();
        bgColor.a = isCurrent ? 0.4f : 0.2f;

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = bgColor;

        EditorGUILayout.BeginVertical(isCurrent ? currentEquipmentStyle : equipmentStyle);
        GUI.backgroundColor = previousBg;

        EditorGUILayout.BeginHorizontal();

        string prefix = isCurrent ? "➤" : "•";
        GUIStyle prefixStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal
        };
        EditorGUILayout.LabelField($"{prefix} [{index}]", prefixStyle, GUILayout.Width(45));

        GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal,
            normal = { textColor = isCurrent ? item.GetRarityColor() : Color.white }
        };
        
        string enhancement = item.EnhancementLevel > 0 ? $" +{item.EnhancementLevel}" : "";
        EditorGUILayout.LabelField($"{item.GetSlotIcon()} {item.ItemName}{enhancement}", nameStyle, GUILayout.Width(180));

        EditorGUILayout.LabelField($"{item.GetRarityIcon()}", EditorStyles.miniLabel, GUILayout.Width(25));

        if (item.IsEquipped)
        {
            GUIStyle equippedStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.5f, 1f, 0.5f) },
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField("[E]", equippedStyle, GUILayout.Width(25));
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawSlotView()
    {
        EditorGUILayout.BeginHorizontal();
        showBySlot = EditorGUILayout.Foldout(showBySlot, "View by Slot", true, headerStyle);
        EditorGUILayout.EndHorizontal();

        if (!showBySlot) return;

        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            var items = controller.EquipmentData.GetItemsBySlot(slot);
            if (items.Count == 0) continue;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"{GetSlotIconString(slot)} {slot} ({items.Count})", EditorStyles.boldLabel);

            foreach (var item in items)
            {
                DrawMiniEquipmentInfo(item);
            }
        }
    }

    private void DrawSetView()
    {
        EditorGUILayout.BeginHorizontal();
        showBySet = EditorGUILayout.Foldout(showBySet, "View by Equipment Set", true, headerStyle);
        EditorGUILayout.EndHorizontal();

        if (!showBySet) return;

        UpdateAvailableSetNames();

        if (availableSetNames.Length == 0)
        {
            EditorGUILayout.HelpBox("No equipment sets found", MessageType.Info);
            return;
        }

        foreach (var setName in availableSetNames)
        {
            var items = controller.EquipmentData.GetItemsBySet(setName);
            if (items.Count == 0) continue;

            EditorGUILayout.Space(5);

            Color previousBg = GUI.backgroundColor;
            GUI.backgroundColor = GetSetColor(setName);
            GUI.backgroundColor = new Color(GUI.backgroundColor.r, GUI.backgroundColor.g, GUI.backgroundColor.b, 0.3f);

            EditorGUILayout.BeginVertical(equipmentStyle);
            GUI.backgroundColor = previousBg;

            int equippedCount = items.Count(item => item.IsEquipped);
            int totalPieces = 0;
            
            if (controller.EquipmentSets.ContainsKey(setName))
            {
                totalPieces = controller.EquipmentSets[setName].TotalPieces;
            }

            GUIStyle setHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.9f, 0.7f, 1f) }
            };
            EditorGUILayout.LabelField($"✨ {setName} ({equippedCount}/{totalPieces})", setHeaderStyle);

            if (equippedCount > 0 && controller.EquipmentSets.ContainsKey(setName))
            {
                var set = controller.EquipmentSets[setName];
                var activeBonuses = set.GetActiveBonuses(equippedCount);
                
                if (activeBonuses.Count > 0)
                {
                    GUIStyle bonusStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(0.7f, 1f, 0.7f) }
                    };
                    
                    foreach (var bonus in activeBonuses)
                    {
                        EditorGUILayout.LabelField($"  ✓ {bonus}", bonusStyle);
                    }
                    
                    EditorGUILayout.Space(3);
                }
            }

            foreach (var item in items)
            {
                DrawMiniEquipmentInfo(item);
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawMiniEquipmentInfo(EquipmentItem item)
    {
        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = item.GetRarityColor();
        GUI.backgroundColor = new Color(GUI.backgroundColor.r, GUI.backgroundColor.g, GUI.backgroundColor.b, 0.3f);

        EditorGUILayout.BeginVertical(equipmentStyle);
        GUI.backgroundColor = previousBg;

        EditorGUILayout.BeginHorizontal();
        
        string enhancement = item.EnhancementLevel > 0 ? $" +{item.EnhancementLevel}" : "";
        EditorGUILayout.LabelField($"{item.GetRarityIcon()} {item.ItemName}{enhancement}", GUILayout.Width(180));
        
        if (item.IsEquipped)
        {
            GUIStyle equippedStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.5f, 1f, 0.5f) }
            };
            EditorGUILayout.LabelField("[E]", equippedStyle, GUILayout.Width(25));
        }
        
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private string GetSlotIconString(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => "⚔️",
            EquipmentSlot.Helmet => "🪖",
            EquipmentSlot.Armor => "🛡️",
            EquipmentSlot.Gloves => "🧤",
            EquipmentSlot.Boots => "👢",
            EquipmentSlot.Ring => "💍",
            EquipmentSlot.Necklace => "📿",
            EquipmentSlot.Accessory => "✨",
            _ => "•"
        };
    }

    private string GetRarityIconString(EquipmentRarity rarity)
    {
        return rarity switch
        {
            EquipmentRarity.Common => "⚪",
            EquipmentRarity.Uncommon => "🟢",
            EquipmentRarity.Rare => "🔵",
            EquipmentRarity.Epic => "🟣",
            EquipmentRarity.Legendary => "🟠",
            EquipmentRarity.Mythic => "🔴",
            _ => "⚪"
        };
    }

    private void DrawSeparator()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }
}

#endif
