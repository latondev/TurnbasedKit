#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using GameSystems.Stats;

[CustomEditor(typeof(UnitStatController))]
public class UnitStatControllerEditor : Editor
{
	private UnitStatController controller;
	private GUIStyle titleStyle;
	private GUIStyle headerStyle;
	private GUIStyle statStyle;
	private bool isInitialized;

	private bool showVitals = true;
	private bool showCombat = true;
	private Vector2 statsScrollPos;
	private float lastRepaintTime;

	private void OnEnable()
	{
		controller = (UnitStatController)target;
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
			float currentTime = (float)EditorApplication.timeSinceStartup;
			if (currentTime - lastRepaintTime >= 0.1f)
			{
				Repaint();
				lastRepaintTime = currentTime;
			}
		}
	}

	private void InitializeStyles()
	{
		if (isInitialized) return;

		titleStyle = new GUIStyle(EditorStyles.boldLabel)
		{
			fontSize = 16,
			alignment = TextAnchor.MiddleCenter,
			normal = { textColor = new Color(0.5f, 1f, 0.9f) }
		};

		headerStyle = new GUIStyle(EditorStyles.boldLabel)
		{
			fontSize = 13,
			normal = { textColor = Color.white }
		};

		statStyle = new GUIStyle(EditorStyles.helpBox)
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

		if (controller == null || controller.Stats == null) return;

		EditorGUILayout.Space(10);
		DrawSeparator();
		DrawUnitTitle();

		if (!Application.isPlaying)
		{
			EditorGUILayout.HelpBox("Enter Play Mode to view live stats", MessageType.Info);
			return;
		}

		EditorGUILayout.Space(10);
		DrawSeparator();
		DrawQuickOverview();

		EditorGUILayout.Space(10);
		DrawSeparator();
		DrawQuickActions();

		EditorGUILayout.Space(10);
		DrawSeparator();
		DrawVitalStats();

		EditorGUILayout.Space(10);
		DrawSeparator();
		DrawCombatStats();
	}

	private void DrawUnitTitle()
	{
		Color previousBg = GUI.backgroundColor;
		GUI.backgroundColor = new Color(0.3f, 0.6f, 0.6f, 0.5f);

		EditorGUILayout.BeginVertical(statStyle);
		GUI.backgroundColor = previousBg;

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		EditorGUILayout.LabelField($"⚔️ {controller.UnitName}", titleStyle);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();

		GUIStyle levelStyle = new GUIStyle(EditorStyles.miniLabel)
		{
			alignment = TextAnchor.MiddleCenter,
			fontStyle = FontStyle.Bold,
			normal = { textColor = new Color(1f, 0.9f, 0.3f) }
		};
		EditorGUILayout.LabelField($"Level {controller.Level}", levelStyle);

		EditorGUILayout.EndVertical();
	}

	private void DrawQuickOverview()
	{
		EditorGUILayout.LabelField("Quick Overview", headerStyle);

		Color previousBg = GUI.backgroundColor;
		GUI.backgroundColor = new Color(0.2f, 0.3f, 0.4f, 0.5f);
		EditorGUILayout.BeginVertical(statStyle);
		GUI.backgroundColor = previousBg;

		var hp = controller.GetStat("hp");
		var mp = controller.GetStat("mp");
		var stamina = controller.GetStat("stamina");

		if (hp != null) DrawMiniStatBar("❤️ HP", hp, hp.GetStatColor());
		if (mp != null) DrawMiniStatBar("💙 MP", mp, mp.GetStatColor());
		if (stamina != null) DrawMiniStatBar("💚 STA", stamina, stamina.GetStatColor());

		EditorGUILayout.EndVertical();
	}

	private void DrawMiniStatBar(string label, Stat stat, Color color)
	{
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField(label, GUILayout.Width(60));

		Rect barRect = EditorGUILayout.GetControlRect(false, 18);
		float percentage = stat.CurrentValue / stat.MaxValue;

		EditorGUI.DrawRect(barRect, new Color(0.2f, 0.2f, 0.2f));

		Rect fillRect = barRect;
		fillRect.width *= percentage;
		EditorGUI.DrawRect(fillRect, stat.GetStatColor());

		GUIStyle textStyle = new GUIStyle(EditorStyles.label)
		{
			alignment = TextAnchor.MiddleCenter,
			normal = { textColor = Color.white },
			fontStyle = FontStyle.Bold,
			fontSize = 11
		};
		GUI.Label(barRect, $"{stat.CurrentValue:F0} / {stat.MaxValue:F0} ({percentage * 100:F0}%)", textStyle);

		EditorGUILayout.EndHorizontal();
	}

	private void DrawQuickActions()
	{
		EditorGUILayout.LabelField("Quick Actions", headerStyle);

		EditorGUILayout.BeginHorizontal();

		Color previousBg = GUI.backgroundColor;

		GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
		if (GUILayout.Button("❤️ Heal +25", GUILayout.Height(25))) controller.Heal(25f);

		GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
		if (GUILayout.Button("💥 Damage -25", GUILayout.Height(25))) controller.TakeDamage(25f);

		GUI.backgroundColor = new Color(0.5f, 0.9f, 1f);
		if (GUILayout.Button("♻️ Restore All", GUILayout.Height(25))) controller.RestoreAll();

		GUI.backgroundColor = previousBg;
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space(5);

		EditorGUILayout.BeginHorizontal();

		GUI.backgroundColor = new Color(1f, 0.9f, 0.3f);
		if (GUILayout.Button("⬆ Level Up", GUILayout.Height(25))) controller.LevelUp();

		GUI.backgroundColor = new Color(0.5f, 0.5f, 0.8f);
		if (GUILayout.Button("🛡️ Toggle Regen", GUILayout.Height(25)))
			controller.EnableRegen = !controller.EnableRegen;

		GUI.backgroundColor = previousBg;
		EditorGUILayout.EndHorizontal();
	}

	private void DrawVitalStats()
	{
		EditorGUILayout.BeginHorizontal();
		showVitals = EditorGUILayout.Foldout(showVitals, "Vital Stats", true, headerStyle);
		EditorGUILayout.EndHorizontal();

		if (!showVitals) return;

		var vitalStats = controller.Stats.GetVitalStats();
		foreach (var stat in vitalStats)
		{
			DrawStatWithBar(stat);
		}
	}

	private void DrawCombatStats()
	{
		EditorGUILayout.BeginHorizontal();
		showCombat = EditorGUILayout.Foldout(showCombat, "Combat Stats", true, headerStyle);
		EditorGUILayout.EndHorizontal();

		if (!showCombat) return;

		var combatStats = controller.Stats.GetCombatStats();
		foreach (var stat in combatStats)
		{
			DrawStatInfo(stat);
		}
	}

	private void DrawStatWithBar(Stat stat)
	{
		Color bgColor = new Color(0.3f, 0.3f, 0.3f);
		bgColor.a = 0.3f;

		Color previousBg = GUI.backgroundColor;
		GUI.backgroundColor = bgColor;

		EditorGUILayout.BeginVertical(statStyle);
		GUI.backgroundColor = previousBg;

		EditorGUILayout.BeginHorizontal();

		EditorGUILayout.LabelField($"{stat.GetStatIcon()} {stat.StatName}", GUILayout.Width(120));

		Rect miniBarRect = EditorGUILayout.GetControlRect(false, 15);
		float percentage = stat.CurrentValue / stat.MaxValue;

		EditorGUI.DrawRect(miniBarRect, new Color(0.2f, 0.2f, 0.2f));
		Rect fillRect = miniBarRect;
		fillRect.width *= percentage;
		EditorGUI.DrawRect(fillRect, stat.GetStatColor());

		GUIStyle textStyle = new GUIStyle(EditorStyles.miniLabel)
		{
			alignment = TextAnchor.MiddleCenter,
			normal = { textColor = Color.white },
			fontStyle = FontStyle.Bold
		};
		GUI.Label(miniBarRect, $"{stat.CurrentValue:F0}/{stat.MaxValue:F0}", textStyle);

		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
	}

	private void DrawStatInfo(Stat stat)
	{
		Color bgColor = new Color(0.3f, 0.3f, 0.3f);
		bgColor.a = 0.3f;

		Color previousBg = GUI.backgroundColor;
		GUI.backgroundColor = bgColor;

		EditorGUILayout.BeginVertical(statStyle);
		GUI.backgroundColor = previousBg;

		EditorGUILayout.BeginHorizontal();

		EditorGUILayout.LabelField($"{stat.GetStatIcon()} {stat.StatName}", GUILayout.Width(120));

		GUIStyle valueStyle = new GUIStyle(EditorStyles.label)
		{
			normal = { textColor = stat.GetStatColor() }
		};
		EditorGUILayout.LabelField($"{stat.GetFinalValue():F1}", valueStyle);

		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
	}

	private void DrawSeparator()
	{
		Rect rect = EditorGUILayout.GetControlRect(false, 1);
		EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
	}
}

#endif
