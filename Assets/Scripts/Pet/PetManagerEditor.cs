#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using GameSystems.Pet;
using System.Linq;

[CustomEditor(typeof(PetManager))]
public class PetManagerEditor : Editor
{
    private PetManager mgr;
    private GUIStyle titleStyle, headerStyle, boxStyle, petBoxStyle;
    private bool stylesInited;

    private bool showCollection = true;
    private bool showActivePet = true;
    private bool showActions = true;

    private string summonPetId = "";
    private long addExp = 100;
    private int addAffection = 5;

    private Vector2 scrollPos;

    private void OnEnable()
    {
        mgr = (PetManager)target;
        EditorApplication.update += RepaintIfPlaying;
    }

    private void OnDisable()
    {
        EditorApplication.update -= RepaintIfPlaying;
    }

    private void RepaintIfPlaying()
    {
        if (Application.isPlaying) Repaint();
    }

    private void EnsureStyles()
    {
        if (stylesInited) return;

        var baseBold = EditorStyles.boldLabel ?? EditorStyles.label ?? GUI.skin.label;
        titleStyle = new GUIStyle(baseBold)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.6f, 0.4f) } // orange
        };

        headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };

        var baseBox = EditorStyles.helpBox ?? GUI.skin.box;
        boxStyle = new GUIStyle(baseBox)
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(0, 0, 6, 6)
        };

        petBoxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(8, 8, 6, 6),
            margin = new RectOffset(0, 0, 3, 3)
        };

        stylesInited = true;
    }

    public override void OnInspectorGUI()
    {
        EnsureStyles();
        DrawDefaultInspector();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode để test Pet Manager.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);
        DrawTitle();

        DrawActivePet();
        DrawActions();
        DrawCollection();
        DrawDatabasePets();
    }

    private void DrawTitle()
    {
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.6f, 0.4f, 0.3f);

        EditorGUILayout.BeginVertical(boxStyle);
        GUI.backgroundColor = prevBg;

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("🐾 Pet Manager", titleStyle);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUIStyle subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };
        EditorGUILayout.LabelField("Collection • Progression • Buffs", subtitleStyle);

        EditorGUILayout.EndVertical();
    }

    private void DrawActivePet()
    {
        showActivePet = EditorGUILayout.Foldout(showActivePet, "Active Pet", true, headerStyle);
        if (!showActivePet) return;

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.5f, 0.8f, 0.5f, 0.4f);
        EditorGUILayout.BeginVertical(boxStyle);
        GUI.backgroundColor = prevBg;

        if (mgr.ActivePet == null)
        {
            EditorGUILayout.HelpBox("No active pet. Select one from collection.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        var pet = mgr.ActivePet;
        var data = mgr.database?.GetById(pet.petId);
        if (data == null)
        {
            EditorGUILayout.HelpBox("Pet data not found in database.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        if (data.icon != null)
            GUILayout.Label(data.icon.texture, GUILayout.Width(64), GUILayout.Height(64));
        else
            GUILayout.Box("🐾", GUILayout.Width(64), GUILayout.Height(64));

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField($"{data.petName} ({data.rarity})", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Level {pet.level} / {data.maxLevel}");
        EditorGUILayout.LabelField($"Affection: {pet.affection} / {data.maxAffection}");
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        DrawProgressBar(pet.ExpProgress01(data), $"Exp: {pet.exp}/{data.GetExpForLevel(pet.level)}", new Color(0.5f, 0.9f, 1f));
        DrawProgressBar((float)pet.affection / data.maxAffection, $"Affection: {pet.affection}/{data.maxAffection}", new Color(1f, 0.5f, 0.7f));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Current Buffs:", EditorStyles.miniBoldLabel);
        var stats = pet.GetCurrentStats(data);
        EditorGUILayout.LabelField($"ATK +{stats.atkBonus:0.#}% | HP +{stats.hpBonus:0.#}% | EXP +{stats.expBonus:0.#}% | Gold +{stats.goldBonus:0.#}%");

        if (data.evolvesTo != null)
        {
            EditorGUILayout.Space(4);
            bool canEvolve = pet.CanEvolve(data, null);
            GUI.backgroundColor = canEvolve ? new Color(1f, 0.84f, 0f) : new Color(0.7f, 0.7f, 0.7f);
            GUI.enabled = canEvolve;
            if (GUILayout.Button($"✨ Evolve to {data.evolvesTo.petName}", GUILayout.Height(26)))
            {
                mgr.EvolvePet(pet.instanceId, (id, qty) => true); // mock material check
            }
            GUI.enabled = true;
            GUI.backgroundColor = prevBg;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawActions()
    {
        showActions = EditorGUILayout.Foldout(showActions, "Actions", true, headerStyle);
        if (!showActions) return;

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.6f, 0.5f, 0.7f, 0.4f);
        EditorGUILayout.BeginVertical(boxStyle);
        GUI.backgroundColor = prevBg;

        EditorGUILayout.LabelField("Summon Pet", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Pet ID:", GUILayout.Width(60));
        summonPetId = EditorGUILayout.TextField(summonPetId);
        if (GUILayout.Button("Summon", GUILayout.Width(80)))
        {
            mgr.SummonPet(summonPetId);
        }
        EditorGUILayout.EndHorizontal();

        if (mgr.ActivePet != null)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Add Progress to Active Pet", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Exp", GUILayout.Width(60));
            addExp = EditorGUILayout.LongField(addExp, GUILayout.Width(100));
            if (GUILayout.Button("Add", GUILayout.Width(80)))
                mgr.AddExpToActivePet(addExp);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Affection", GUILayout.Width(60));
            addAffection = EditorGUILayout.IntField(addAffection, GUILayout.Width(100));
            if (GUILayout.Button("Add", GUILayout.Width(80)))
                mgr.AddAffectionToActivePet(addAffection);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(6);
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("🗑️ Reset All Pets", GUILayout.Height(26)))
        {
            if (EditorUtility.DisplayDialog("Reset Pets", "Delete all pet data?", "Yes", "No"))
                mgr.ResetAll();
        }
        GUI.backgroundColor = prevBg;

        EditorGUILayout.EndVertical();
    }

    private void DrawCollection()
    {
        showCollection = EditorGUILayout.Foldout(showCollection, "Pet Collection", true, headerStyle);
        if (!showCollection) return;

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.5f, 0.6f, 0.8f, 0.4f);
        EditorGUILayout.BeginVertical(boxStyle);
        GUI.backgroundColor = prevBg;

        var all = mgr.GetAllPets().ToList();
        EditorGUILayout.LabelField($"Total: {all.Count} pets", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(500));

        foreach (var pair in all)
        {
            DrawPetItem(pair.instance, pair.data);
        }

        if (all.Count == 0)
            EditorGUILayout.HelpBox("No pets collected yet. Summon one!", MessageType.Info);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawPetItem(PetInstance pet, PetData data)
    {
        bool isActive = mgr.ActivePet != null && mgr.ActivePet.instanceId == pet.instanceId;
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = isActive ? new Color(1f, 0.84f, 0f, 0.4f) : new Color(0.5f, 0.5f, 0.5f, 0.2f);

        EditorGUILayout.BeginVertical(petBoxStyle);
        GUI.backgroundColor = prevBg;

        EditorGUILayout.BeginHorizontal();

        if (data.icon != null)
            GUILayout.Label(data.icon.texture, GUILayout.Width(48), GUILayout.Height(48));
        else
            GUILayout.Box("🐾", GUILayout.Width(48), GUILayout.Height(48));

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField($"{data.petName} | Lv.{pet.level} | {data.rarity}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Type: {data.type} | Affection: {pet.affection}/{data.maxAffection}", EditorStyles.miniLabel);
        DrawProgressBar(pet.ExpProgress01(data), $"{pet.exp}/{data.GetExpForLevel(pet.level)}", new Color(0.5f, 0.9f, 1f));
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);
        GUI.enabled = !isActive;
        GUI.backgroundColor = new Color(0.5f, 1f, 0.7f);
        if (GUILayout.Button("⭐ Set Active", GUILayout.Height(22)))
        {
            mgr.SetActivePet(pet.instanceId);
        }
        GUI.enabled = true;
        GUI.backgroundColor = prevBg;

        EditorGUILayout.EndVertical();
    }
    private void DrawDatabasePets()
    {
        if (mgr.database == null || mgr.database.allPets.Count == 0) return;

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Available in Database", EditorStyles.boldLabel);

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.4f, 0.5f, 0.6f, 0.4f);
        EditorGUILayout.BeginVertical(boxStyle);
        GUI.backgroundColor = prevBg;

        foreach (var petData in mgr.database.allPets)
        {
            if (petData == null) continue;

            EditorGUILayout.BeginHorizontal();

            // Icon
            if (petData.icon != null)
                GUILayout.Label(petData.icon.texture, GUILayout.Width(32), GUILayout.Height(32));
            else
                GUILayout.Box("🐾", GUILayout.Width(32), GUILayout.Height(32));

            // Info
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"{petData.petName} ({petData.rarity})", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"ID: {petData.petId}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            // Check if already owned
            bool owned = mgr.GetAllPets().Any(p => p.instance.petId == petData.petId);
        
            GUI.enabled = !owned;
            GUI.backgroundColor = owned ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.5f, 1f, 0.7f);
        
            if (GUILayout.Button(owned ? "✓ Owned" : "Summon", GUILayout.Width(80), GUILayout.Height(32)))
            {
                mgr.SummonPet(petData.petId);
            }
        
            GUI.enabled = true;
            GUI.backgroundColor = prevBg;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndVertical();
    }
    private void DrawProgressBar(float progress, string label, Color barColor)
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 14);
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1f));

        Rect fillRect = rect;
        fillRect.width *= Mathf.Clamp01(progress);
        EditorGUI.DrawRect(fillRect, barColor);

        GUIStyle textStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold
        };
        GUI.Label(rect, label, textStyle);
    }
}
#endif
