#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using GameSystems;

[CustomEditor(typeof(TimeManager))]
public class TimeManagerEditor : Editor
{
    private TimeManager tm;
    private GUIStyle titleStyle;
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private bool init;

    // Foldouts
    private bool showControls = true;
    private bool showCooldowns = true;
    private bool showTimers = true;
    private bool showSchedules = true;
    private bool showOffline = true;

    // Inputs
    private string cdId = "skill_fireball";
    private float cdDur = 5f;

    private string timerId = "countdown10";
    private float timerDur = 10f;
    private bool timerRepeat = false;
    private bool timerCountUp = false;

    private string dailyId = "daily_reset";
    private int dailyHour = 0;
    private int dailyMinute = 0;

    private string hourlyId = "hourly_reward";
    private int hourlyMinute = 0;

    private string weeklyId = "weekly_event";
    private DayOfWeek weeklyDay = DayOfWeek.Monday;
    private int weeklyHour = 0;
    private int weeklyMinute = 0;

    private void OnEnable()
    {
        tm = (TimeManager)target;
        EditorApplication.update += RepaintOnPlay;
    }

    private void OnDisable()
    {
        EditorApplication.update -= RepaintOnPlay;
    }

    private void RepaintOnPlay()
    {
        if (Application.isPlaying) Repaint();
    }

    private void InitStyles()
    {
        if (init) return;
        titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.3f, 0.9f, 1f) }
        };
        headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        boxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(0, 0, 8, 8)
        };
        init = true;
    }

    public override void OnInspectorGUI()
    {
        InitStyles();

        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        DrawTitle();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode để dùng Time Editor (xem cooldowns/timers/schedules realtime).", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(6);
        DrawControlsSection();

        EditorGUILayout.Space(6);
        DrawCooldownsSection();

        EditorGUILayout.Space(6);
        DrawTimersSection();

        EditorGUILayout.Space(6);
        DrawSchedulesSection();

        EditorGUILayout.Space(6);
        DrawOfflineSection();
    }

    private void DrawTitle()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        GUILayout.Label("⏰ Time Manager (Editor)", titleStyle);
        GUILayout.Label("Cooldowns • Timers • Schedules • Pause • Speed • Offline", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    private void DrawControlsSection()
    {
        showControls = EditorGUILayout.Foldout(showControls, "Controls", true, headerStyle);
        if (!showControls) return;

        EditorGUILayout.BeginVertical(boxStyle);

        // Pause & Speed
        EditorGUILayout.BeginHorizontal();
        if (tm.paused)
        {
            GUI.backgroundColor = new Color(0.5f, 1f, 0.7f);
            if (GUILayout.Button("▶ Resume", GUILayout.Height(26)))
                tm.SetPaused(false);
        }
        else
        {
            GUI.backgroundColor = new Color(1f, 0.9f, 0.5f);
            if (GUILayout.Button("⏸ Pause", GUILayout.Height(26)))
                tm.SetPaused(true);
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(8);
        GUILayout.Label($"Speed: {tm.timeScale:F2}x", GUILayout.Width(90));
        if (GUILayout.Button("0.5x", GUILayout.Height(26))) tm.SetTimeScale(0.5f);
        if (GUILayout.Button("1x", GUILayout.Height(26))) tm.SetTimeScale(1f);
        if (GUILayout.Button("2x", GUILayout.Height(26))) tm.SetTimeScale(2f);
        if (GUILayout.Button("5x", GUILayout.Height(26))) tm.SetTimeScale(5f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawCooldownsSection()
    {
        showCooldowns = EditorGUILayout.Foldout(showCooldowns, "Cooldowns", true, headerStyle);
        if (!showCooldowns) return;

        EditorGUILayout.BeginVertical(boxStyle);

        // Creator
        EditorGUILayout.LabelField("Start Cooldown", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        cdId = EditorGUILayout.TextField("Id", cdId);
        cdDur = EditorGUILayout.FloatField("Duration (s)", cdDur);
        if (GUILayout.Button("Start", GUILayout.Width(80)))
        {
            tm.StartCooldown(cdId, Mathf.Max(0f, cdDur));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Display active cds (we cannot access private list directly, so show known ids)
        // Gợi ý: bạn có thể thêm public accessor nếu cần liệt kê toàn bộ.
        // Ở đây, demo hiển thị nhanh theo id đang nhập.
        EditorGUILayout.LabelField("Quick View", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"[{cdId}] Remain: {tm.CooldownRemaining(cdId):F2}s", GUILayout.Width(220));
        DrawProgressBar(tm.CooldownProgress01(cdId), tm.CooldownReady(cdId) ? "Ready" : "Running");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawTimersSection()
    {
        showTimers = EditorGUILayout.Foldout(showTimers, "Timers", true, headerStyle);
        if (!showTimers) return;

        EditorGUILayout.BeginVertical(boxStyle);

        // Creator
        EditorGUILayout.LabelField("Create/Start Timer", EditorStyles.miniBoldLabel);
        timerId = EditorGUILayout.TextField("Id", timerId);
        timerDur = EditorGUILayout.FloatField("Duration (s)", timerDur);
        EditorGUILayout.BeginHorizontal();
        timerRepeat = EditorGUILayout.ToggleLeft("Repeat", timerRepeat, GUILayout.Width(80));
        timerCountUp = EditorGUILayout.ToggleLeft("Count Up", timerCountUp, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create", GUILayout.Height(24)))
        {
            tm.CreateTimer(timerId, Mathf.Max(0f, timerDur), timerRepeat, timerCountUp,
                onComplete: () => Debug.Log($"[Time] Timer '{timerId}' completed."),
                onTick: _ => { /* optional live UI */ });
        }
        if (GUILayout.Button("Start", GUILayout.Height(24)))
        {
            tm.StartTimer(timerId);
        }
        if (GUILayout.Button("Stop", GUILayout.Height(24)))
        {
            tm.StopTimer(timerId);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Quick View", EditorStyles.miniBoldLabel);
        float tp = tm.TimerProgress01(timerId);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"[{timerId}] Progress:", GUILayout.Width(140));
        DrawProgressBar(tp, $"{tp * 100f:0}%");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawSchedulesSection()
    {
        showSchedules = EditorGUILayout.Foldout(showSchedules, "Schedules (Daily/Weekly/Hourly/Once)", true, headerStyle);
        if (!showSchedules) return;

        EditorGUILayout.BeginVertical(boxStyle);

        // Daily
        EditorGUILayout.LabelField("Daily", EditorStyles.miniBoldLabel);
        dailyId = EditorGUILayout.TextField("Id", dailyId);
        EditorGUILayout.BeginHorizontal();
        dailyHour = EditorGUILayout.IntField("Hour (UTC)", dailyHour);
        dailyMinute = EditorGUILayout.IntField("Minute", dailyMinute);
        if (GUILayout.Button("Schedule Daily", GUILayout.Width(140)))
        {
            tm.ScheduleDaily(dailyId, "Daily Event", Mathf.Clamp(dailyHour,0,23), Mathf.Clamp(dailyMinute,0,59),
                () => Debug.Log($"[Time] Daily '{dailyId}' triggered @ {DateTime.UtcNow:HH:mm:ss} UTC"));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Hourly
        EditorGUILayout.LabelField("Hourly", EditorStyles.miniBoldLabel);
        hourlyId = EditorGUILayout.TextField("Id", hourlyId);
        EditorGUILayout.BeginHorizontal();
        hourlyMinute = EditorGUILayout.IntField("Minute", hourlyMinute);
        if (GUILayout.Button("Schedule Hourly", GUILayout.Width(140)))
        {
            tm.ScheduleHourly(hourlyId, "Hourly Event", Mathf.Clamp(hourlyMinute,0,59),
                () => Debug.Log($"[Time] Hourly '{hourlyId}' triggered @ {DateTime.UtcNow:HH:mm:ss} UTC"));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Weekly
        EditorGUILayout.LabelField("Weekly", EditorStyles.miniBoldLabel);
        weeklyId = EditorGUILayout.TextField("Id", weeklyId);
        weeklyDay = (DayOfWeek)EditorGUILayout.EnumPopup("Day", weeklyDay);
        EditorGUILayout.BeginHorizontal();
        weeklyHour = EditorGUILayout.IntField("Hour (UTC)", weeklyHour);
        weeklyMinute = EditorGUILayout.IntField("Minute", weeklyMinute);
        if (GUILayout.Button("Schedule Weekly", GUILayout.Width(140)))
        {
            tm.ScheduleWeekly(weeklyId, "Weekly Event", weeklyDay,
                Mathf.Clamp(weeklyHour,0,23), Mathf.Clamp(weeklyMinute,0,59),
                () => Debug.Log($"[Time] Weekly '{weeklyId}' triggered @ {DateTime.UtcNow:HH:mm:ss} UTC"));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawOfflineSection()
    {
        showOffline = EditorGUILayout.Foldout(showOffline, "Offline (Save/Resume)", true, headerStyle);
        if (!showOffline) return;

        EditorGUILayout.BeginVertical(boxStyle);

        EditorGUILayout.LabelField("Offline Flow", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Last Quit (Now UTC)", GUILayout.Height(24)))
        {
            tm.SaveLastQuitUtc();
            Debug.Log($"[Time] Saved last quit UTC: {DateTime.UtcNow:o}");
        }
        if (GUILayout.Button("Get Offline Span", GUILayout.Height(24)))
        {
            var span = tm.GetOfflineSpan();
            Debug.Log($"[Time] Offline: {span.TotalMinutes:F2} minutes");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("Khi App pause/quit: gọi SaveLastQuitUtc(). Khi khởi động: GetOfflineSpan() để tính thưởng offline.", MessageType.None);

        EditorGUILayout.EndVertical();
    }

    private void DrawProgressBar(float p, string label)
    {
        Rect r = EditorGUILayout.GetControlRect(false, 16);
        EditorGUI.DrawRect(r, new Color(0.2f,0.2f,0.2f,1f));
        var fill = r; fill.width *= Mathf.Clamp01(p);
        Color c = p >= 1f ? new Color(0.5f,1f,0.5f,1f) : new Color(0.4f,0.8f,1f,1f);
        EditorGUI.DrawRect(fill, c);
        var style = new GUIStyle(EditorStyles.whiteMiniLabel){ alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        EditorGUI.LabelField(r, label, style);
    }
}
#endif
