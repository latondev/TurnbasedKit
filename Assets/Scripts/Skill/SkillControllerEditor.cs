#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;
using GameSystems.Skills;

[CustomEditor(typeof(SkillController))]
public class SkillControllerEditor : Editor
{
    private SkillController controller;
    private GUIStyle titleStyle;
    private GUIStyle headerStyle;
    private GUIStyle skillStyle;
    private GUIStyle currentSkillStyle;
    private GUIStyle manaStyle;
    private bool isInitialized;

    private bool showSkills = true;
    private bool showByCategory = false;
    private bool showUnlockedOnly = false;
    private bool showReadyOnly = false;
    private Vector2 skillScrollPos;

    private SkillCategory filterCategory = SkillCategory.Active;
    private SkillElement filterElement = SkillElement.Fire;

    private void OnEnable()
    {
        controller = (SkillController)target;
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
            normal = { textColor = new Color(1f, 0.6f, 0.3f) }
        };

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            normal = { textColor = Color.white }
        };

        skillStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(0, 0, 3, 3)
        };

        currentSkillStyle = new GUIStyle(skillStyle)
        {
            fontStyle = FontStyle.Bold
        };

        manaStyle = new GUIStyle(EditorStyles.helpBox)
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
        DrawSkillSystemTitle();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use skill controls", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Mana Bar
        DrawManaBar();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Quick Stats
        DrawQuickStats();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Current Skill Display
        DrawCurrentSkillDetailed();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Navigation
        DrawNavigationControls();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Skill Actions
        DrawSkillActions();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Sorting & Filters
        DrawSortingAndFilters();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Skills on Cooldown
        DrawSkillsOnCooldown();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // All Skills List
        DrawAllSkills();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Category View
        DrawCategoryView();
    }

    private void DrawSkillSystemTitle()
    {
        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.8f, 0.4f, 0.2f, 0.5f);

        EditorGUILayout.BeginVertical(skillStyle);
        GUI.backgroundColor = previousBg;

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"⚡ {controller.ControllerName}", titleStyle);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Player level
        GUIStyle levelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.9f, 0.3f) }
        };
        EditorGUILayout.LabelField($"Player Level {controller.PlayerLevel}", levelStyle);

        EditorGUILayout.EndVertical();
    }

    private void DrawManaBar()
    {
        EditorGUILayout.LabelField("Mana", headerStyle);

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.2f, 0.4f, 0.8f, 0.5f);
        EditorGUILayout.BeginVertical(manaStyle);
        GUI.backgroundColor = previousBg;

        // Mana text
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("💙 Current Mana:", GUILayout.Width(120));
        GUIStyle manaTextStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = new Color(0.3f, 0.7f, 1f) }
        };
        EditorGUILayout.LabelField($"{controller.CurrentMana} / {controller.MaxMana}", manaTextStyle);
        EditorGUILayout.EndHorizontal();

        // Mana bar
        float manaPercent = (float)controller.CurrentMana / controller.MaxMana;
        EditorGUILayout.Space(5);
        Rect manaRect = EditorGUILayout.GetControlRect(false, 25);
        
        // Background
        EditorGUI.DrawRect(manaRect, new Color(0.2f, 0.2f, 0.2f));
        
        // Fill
        Rect fillRect = manaRect;
        fillRect.width *= manaPercent;
        Color manaColor = new Color(0.3f, 0.7f, 1f);
        EditorGUI.DrawRect(fillRect, manaColor);

        // Border
        Handles.BeginGUI();
        Handles.color = Color.black;
        Handles.DrawLine(new Vector3(manaRect.x, manaRect.y), new Vector3(manaRect.x + manaRect.width, manaRect.y));
        Handles.DrawLine(new Vector3(manaRect.x, manaRect.y + manaRect.height), new Vector3(manaRect.x + manaRect.width, manaRect.y + manaRect.height));
        Handles.EndGUI();

        // Text on bar
        GUIStyle barTextStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold,
            fontSize = 11
        };
        GUI.Label(manaRect, $"{manaPercent * 100:F0}%", barTextStyle);

        EditorGUILayout.EndVertical();

        // Restore mana button
        EditorGUILayout.Space(5);
        Color btnBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
        if (GUILayout.Button("✨ Restore Mana (+50)", GUILayout.Height(25)))
        {
            controller.RestoreMana(50);
        }
        GUI.backgroundColor = btnBg;
    }

    private void DrawQuickStats()
    {
        EditorGUILayout.LabelField("Quick Statistics", headerStyle);

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.2f, 0.3f, 0.4f, 0.5f);
        EditorGUILayout.BeginVertical(skillStyle);
        GUI.backgroundColor = previousBg;

        // Total skills
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("📚 Total Skills:", GUILayout.Width(130));
        EditorGUILayout.LabelField(controller.SkillData.Items.Count.ToString(), EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        // Unlocked
        int unlockedCount = controller.SkillData.GetUnlockedSkills().Count;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🔓 Unlocked:", GUILayout.Width(130));
        GUIStyle unlockedStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = new Color(0.5f, 1f, 0.5f) }
        };
        EditorGUILayout.LabelField($"{unlockedCount}/{controller.SkillData.Items.Count}", unlockedStyle);
        EditorGUILayout.EndHorizontal();

        // Ready to cast
        int readyCount = controller.SkillData.GetReadySkills(controller.CurrentMana).Count;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("⚡ Ready:", GUILayout.Width(130));
        GUIStyle readyStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = new Color(1f, 0.9f, 0.3f) }
        };
        EditorGUILayout.LabelField(readyCount.ToString(), readyStyle);
        EditorGUILayout.EndHorizontal();

        // On cooldown
        int cooldownCount = controller.SkillData.GetSkillsOnCooldown().Count;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("⏱ On Cooldown:", GUILayout.Width(130));
        GUIStyle cdStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = new Color(1f, 0.5f, 0.5f) }
        };
        EditorGUILayout.LabelField(cooldownCount.ToString(), cdStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawCurrentSkillDetailed()
    {
        EditorGUILayout.LabelField("Currently Selected Skill", headerStyle);

        var skill = controller.CurrentSkill;
        if (skill == null)
        {
            EditorGUILayout.HelpBox("No skill selected", MessageType.Info);
            return;
        }

        // Background color based on element
        Color bgColor = skill.GetElementColor();
        bgColor.a = 0.3f;

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = bgColor;

        EditorGUILayout.BeginVertical(currentSkillStyle);
        GUI.backgroundColor = previousBg;

        // Header: Name, Category, Element
        EditorGUILayout.BeginHorizontal();
        
        // Skill name with icon
        GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            normal = { textColor = skill.GetElementColor() }
        };
        EditorGUILayout.LabelField($"{skill.GetElementIcon()} {skill.SkillName}", nameStyle);

        // Category badge
        DrawCategoryBadge(skill.Category, skill.GetCategoryColor());

        EditorGUILayout.EndHorizontal();

        // Description
        GUIStyle descStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontStyle = FontStyle.Italic,
            wordWrap = true
        };
        EditorGUILayout.LabelField(skill.Description, descStyle);

        EditorGUILayout.Space(5);

        // Level and status
        EditorGUILayout.BeginHorizontal();
        
        if (skill.IsUnlocked)
        {
            // Level display
            GUIStyle levelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.9f, 0.3f) }
            };
            EditorGUILayout.LabelField($"⭐ Level {skill.CurrentLevel}/{skill.MaxLevel}", levelStyle, GUILayout.Width(120));
        }
        else
        {
            // Locked status
            GUIStyle lockedStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.3f, 0.3f) }
            };
            EditorGUILayout.LabelField("🔒 LOCKED", lockedStyle, GUILayout.Width(120));
        }

        // Element badge
        DrawElementBadge(skill.Element, skill.GetElementColor());

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Stats
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"💠 Mana: {skill.GetScaledManaCost()}", EditorStyles.miniLabel, GUILayout.Width(100));
        EditorGUILayout.LabelField($"⚔️ Damage: {skill.GetTotalDamage():F0}", EditorStyles.miniLabel, GUILayout.Width(120));
        EditorGUILayout.LabelField($"📏 Range: {skill.Range:F0}m", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"⏱ Cooldown: {skill.BaseCooldown:F1}s", EditorStyles.miniLabel, GUILayout.Width(130));
        EditorGUILayout.LabelField($"🎯 Casts: {skill.TotalCasts}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        // Cooldown bar (if on cooldown)
        if (skill.IsOnCooldown)
        {
            EditorGUILayout.Space(5);
            DrawCooldownBar(skill);
        }

        EditorGUILayout.EndVertical();

        // Cast button
        EditorGUILayout.Space(5);
        DrawCastButton(skill);
    }

    private void DrawCooldownBar(SkillData skill)
    {
        Rect cdRect = EditorGUILayout.GetControlRect(false, 20);
        float percentage = skill.GetCooldownPercentage();

        // Background
        EditorGUI.DrawRect(cdRect, new Color(0.2f, 0.2f, 0.2f));

        // Fill (progress)
        Rect fillRect = cdRect;
        fillRect.width *= percentage;
        Color cdColor = Color.Lerp(new Color(1f, 0.3f, 0.3f), new Color(0.3f, 1f, 0.3f), percentage);
        EditorGUI.DrawRect(fillRect, cdColor);

        // Border
        Handles.BeginGUI();
        Handles.color = Color.black;
        Handles.DrawLine(new Vector3(cdRect.x, cdRect.y), new Vector3(cdRect.x + cdRect.width, cdRect.y));
        Handles.DrawLine(new Vector3(cdRect.x, cdRect.y + cdRect.height), new Vector3(cdRect.x + cdRect.width, cdRect.y + cdRect.height));
        Handles.EndGUI();

        // Text
        GUIStyle cdTextStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold
        };
        GUI.Label(cdRect, $"⏱ {skill.CurrentCooldown:F1}s / {skill.BaseCooldown:F1}s", cdTextStyle);
    }

    private void DrawCastButton(SkillData skill)
    {
        bool canCast = skill.CanCast(controller.CurrentMana);
        
        GUI.enabled = canCast;
        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = canCast ? new Color(0.5f, 1f, 0.5f) : new Color(0.5f, 0.5f, 0.5f);

        string buttonText = "✨ Cast Skill";
        if (!skill.IsUnlocked)
            buttonText = "🔒 Locked";
        else if (skill.IsOnCooldown)
            buttonText = $"⏱ On Cooldown ({skill.CurrentCooldown:F1}s)";
        else if (controller.CurrentMana < skill.GetScaledManaCost())
            buttonText = $"💠 Need {skill.GetScaledManaCost()} Mana";

        if (GUILayout.Button(buttonText, GUILayout.Height(35)))
        {
            controller.CastCurrentSkill();
        }

        GUI.backgroundColor = previousBg;
        GUI.enabled = true;
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
        if (GUILayout.Button("🔓 Next Unlocked", GUILayout.Height(25)))
        {
            controller.NextUnlocked();
        }

        GUI.backgroundColor = new Color(0.7f, 1f, 0.5f);
        if (GUILayout.Button("⚡ Next Ready", GUILayout.Height(25)))
        {
            controller.NextReady();
        }

        GUI.backgroundColor = previousBg;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSkillActions()
    {
        EditorGUILayout.LabelField("Skill Actions", headerStyle);

        EditorGUILayout.BeginHorizontal();
        
        Color previousBg = GUI.backgroundColor;

        // Unlock
        GUI.backgroundColor = new Color(0.5f, 1f, 0.7f);
        if (GUILayout.Button("🔓 Unlock", GUILayout.Height(30)))
        {
            controller.UnlockCurrentSkill();
        }

        // Level Up
        GUI.backgroundColor = new Color(1f, 0.9f, 0.3f);
        if (GUILayout.Button("⬆ Level Up", GUILayout.Height(30)))
        {
            controller.LevelUpCurrentSkill();
        }

        // Reset CD
        GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
        if (GUILayout.Button("↺ Reset CD", GUILayout.Height(30)))
        {
            controller.ResetCooldownCurrent();
        }

        GUI.backgroundColor = previousBg;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        // Unlock All
        GUI.backgroundColor = new Color(0.5f, 1f, 0.8f);
        if (GUILayout.Button("🔓 Unlock All", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Unlock All", "Unlock all skills?", "Yes", "No"))
            {
                controller.UnlockAll();
            }
        }

        // Reset All CD
        GUI.backgroundColor = new Color(0.5f, 0.9f, 1f);
        if (GUILayout.Button("↺ Reset All CD", GUILayout.Height(25)))
        {
            controller.ResetAllCooldowns();
        }

        // Level Up Player
        GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
        if (GUILayout.Button("🎉 Player +1", GUILayout.Height(25)))
        {
            controller.LevelUpPlayer();
        }

        GUI.backgroundColor = previousBg;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSortingAndFilters()
    {
        EditorGUILayout.LabelField("Sorting & Filters", headerStyle);

        // Sorting
        EditorGUILayout.LabelField("Sort by:", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("⭐ Level", GUILayout.Height(25)))
        {
            controller.SortByLevel();
        }
        if (GUILayout.Button("⚔️ Damage", GUILayout.Height(25)))
        {
            controller.SortByDamage();
        }
        if (GUILayout.Button("⏱ Cooldown", GUILayout.Height(25)))
        {
            controller.SortByCooldown();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Filters
        EditorGUILayout.LabelField("Filters:", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        showUnlockedOnly = GUILayout.Toggle(showUnlockedOnly, "Unlocked Only", GUILayout.Width(120));
        showReadyOnly = GUILayout.Toggle(showReadyOnly, "Ready Only");
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSkillsOnCooldown()
    {
        var skillsOnCD = controller.SkillData.GetSkillsOnCooldown();
        if (skillsOnCD.Count == 0) return;

        EditorGUILayout.LabelField($"⏱ Skills on Cooldown ({skillsOnCD.Count})", headerStyle);

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.6f, 0.3f, 0.3f, 0.4f);

        foreach (var skill in skillsOnCD)
        {
            EditorGUILayout.BeginVertical(skillStyle);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{skill.GetElementIcon()} {skill.SkillName}", GUILayout.Width(150));
            
            // Mini cooldown bar
            Rect miniCdRect = EditorGUILayout.GetControlRect(false, 15);
            float percentage = skill.GetCooldownPercentage();
            
            EditorGUI.DrawRect(miniCdRect, new Color(0.2f, 0.2f, 0.2f));
            Rect fillRect = miniCdRect;
            fillRect.width *= percentage;
            EditorGUI.DrawRect(fillRect, new Color(1f, 0.5f, 0.3f));

            GUIStyle miniTextStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(miniCdRect, $"{skill.CurrentCooldown:F1}s", miniTextStyle);

            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        GUI.backgroundColor = previousBg;
    }

    private void DrawAllSkills()
    {
        EditorGUILayout.BeginHorizontal();
        showSkills = EditorGUILayout.Foldout(showSkills, 
            $"All Skills ({controller.SkillData.Items.Count})", true, headerStyle);
        EditorGUILayout.EndHorizontal();

        if (!showSkills) return;

        if (controller.SkillData.Items.Count == 0)
        {
            EditorGUILayout.HelpBox("No skills available", MessageType.Info);
            return;
        }

        int currentIndex = controller.SkillData.GetCurrentIndex();

        // Filter skills
        var displaySkills = controller.SkillData.Items.AsEnumerable();
        if (showUnlockedOnly)
            displaySkills = displaySkills.Where(s => s.IsUnlocked);
        if (showReadyOnly)
            displaySkills = displaySkills.Where(s => s.CanCast(controller.CurrentMana));

        var skillList = displaySkills.ToList();

        if (skillList.Count == 0)
        {
            EditorGUILayout.HelpBox("No skills match current filters", MessageType.Info);
            return;
        }

        skillScrollPos = EditorGUILayout.BeginScrollView(skillScrollPos, GUILayout.MaxHeight(350));

        for (int i = 0; i < controller.SkillData.Items.Count; i++)
        {
            var skill = controller.SkillData.Items[i];
            
            // Apply filters
            if (showUnlockedOnly && !skill.IsUnlocked) continue;
            if (showReadyOnly && !skill.CanCast(controller.CurrentMana)) continue;

            bool isCurrent = i == currentIndex;
            DrawSkillListItem(skill, i, isCurrent);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSkillListItem(SkillData skill, int index, bool isCurrent)
    {
        Color bgColor;
        if (isCurrent)
        {
            bgColor = skill.GetElementColor();
            bgColor.a = 0.4f;
        }
        else if (!skill.IsUnlocked)
        {
            bgColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
        }
        else if (skill.IsOnCooldown)
        {
            bgColor = new Color(0.6f, 0.3f, 0.3f, 0.3f);
        }
        else
        {
            bgColor = new Color(0.3f, 0.4f, 0.5f, 0.3f);
        }

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = bgColor;

        EditorGUILayout.BeginVertical(isCurrent ? currentSkillStyle : skillStyle);
        GUI.backgroundColor = previousBg;

        EditorGUILayout.BeginHorizontal();

        // Prefix and icon
        string prefix = isCurrent ? "➤" : "•";
        GUIStyle prefixStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal
        };
        EditorGUILayout.LabelField($"{prefix} [{index}]", prefixStyle, GUILayout.Width(45));

        // Skill info
        GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal,
            normal = { textColor = isCurrent ? skill.GetElementColor() : Color.white }
        };
        EditorGUILayout.LabelField($"{skill.GetElementIcon()} {skill.SkillName}", nameStyle, GUILayout.Width(150));

        // Level/Status
        if (skill.IsUnlocked)
        {
            GUIStyle levelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 0.9f, 0.3f) }
            };
            EditorGUILayout.LabelField($"Lv.{skill.CurrentLevel}", levelStyle, GUILayout.Width(40));
        }
        else
        {
            GUIStyle lockedStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 0.3f, 0.3f) }
            };
            EditorGUILayout.LabelField("🔒", lockedStyle, GUILayout.Width(25));
        }

        // Damage
        EditorGUILayout.LabelField($"⚔️{skill.GetTotalDamage():F0}", EditorStyles.miniLabel, GUILayout.Width(60));

        // Cooldown indicator
        if (skill.IsOnCooldown)
        {
            GUIStyle cdStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 0.5f, 0.3f) }
            };
            EditorGUILayout.LabelField($"⏱{skill.CurrentCooldown:F1}s", cdStyle, GUILayout.Width(50));
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawCategoryView()
    {
        EditorGUILayout.BeginHorizontal();
        showByCategory = EditorGUILayout.Foldout(showByCategory, "View by Category", true, headerStyle);
        EditorGUILayout.EndHorizontal();

        if (!showByCategory) return;

        foreach (SkillCategory category in System.Enum.GetValues(typeof(SkillCategory)))
        {
            var skills = controller.SkillData.GetSkillsByCategory(category);
            if (skills.Count == 0) continue;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"{GetCategoryIconString(category)} {category} ({skills.Count})", EditorStyles.boldLabel);

            foreach (var skill in skills)
            {
                DrawMiniSkillInfo(skill);
            }
        }
    }

    private void DrawMiniSkillInfo(SkillData skill)
    {
        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = skill.GetElementColor();
        GUI.backgroundColor = new Color(GUI.backgroundColor.r, GUI.backgroundColor.g, GUI.backgroundColor.b, 0.3f);

        EditorGUILayout.BeginVertical(skillStyle);
        GUI.backgroundColor = previousBg;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{skill.GetElementIcon()} {skill.SkillName}", GUILayout.Width(150));
        
        if (skill.IsUnlocked)
        {
            EditorGUILayout.LabelField($"Lv.{skill.CurrentLevel}", EditorStyles.miniLabel, GUILayout.Width(40));
        }
        else
        {
            EditorGUILayout.LabelField("🔒", EditorStyles.miniLabel, GUILayout.Width(25));
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawCategoryBadge(SkillCategory category, Color color)
    {
        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = color;

        GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fontSize = 9,
            fontStyle = FontStyle.Bold
        };

        GUILayout.Label(category.ToString(), badgeStyle, GUILayout.Width(70));
        GUI.backgroundColor = previousBg;
    }

    private void DrawElementBadge(SkillElement element, Color color)
    {
        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = color;

        GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fontSize = 9,
            fontStyle = FontStyle.Bold
        };

        GUILayout.Label($"{GetElementIconString(element)} {element}", badgeStyle, GUILayout.Width(90));
        GUI.backgroundColor = previousBg;
    }

    private string GetCategoryIconString(SkillCategory category)
    {
        return category switch
        {
            SkillCategory.Active => "⚡",
            SkillCategory.Passive => "🛡️",
            SkillCategory.Ultimate => "🔥",
            SkillCategory.Buff => "✨",
            SkillCategory.Debuff => "💀",
            SkillCategory.Healing => "💚",
            _ => "•"
        };
    }

    private string GetElementIconString(SkillElement element)
    {
        return element switch
        {
            SkillElement.Fire => "🔥",
            SkillElement.Ice => "❄️",
            SkillElement.Lightning => "⚡",
            SkillElement.Earth => "🌍",
            SkillElement.Wind => "💨",
            SkillElement.Holy => "✨",
            SkillElement.Dark => "🌑",
            SkillElement.Physical => "⚔️",
            _ => "•"
        };
    }

    private void DrawSeparator()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }
}

#endif
