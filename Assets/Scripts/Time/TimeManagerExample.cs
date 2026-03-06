using UnityEngine;
using System;
using GameSystems;

public class TimeManagerExample : MonoBehaviour
{
    void Start()
    {
        var tm = TimeManager.Instance;

        // Cooldown
        tm.StartCooldown("skill_fireball", 5f);
        Debug.Log("Fireball ready? " + tm.CooldownReady("skill_fireball"));

        // Timer: countdown 10s, on complete log
        tm.CreateTimer("countdown10", 10f, repeat:false, countUp:false, 
            onComplete: ()=> Debug.Log("Timer 10s completed"),
            onTick: (t)=> { /* update UI if needed */ });
        tm.StartTimer("countdown10");

        // Schedule daily reset 00:00 UTC
        tm.ScheduleDaily("daily_reset", "Daily Reset", 0, 0, () => Debug.Log("Daily Reset!"));

        // Hourly reward at minute 0
        tm.ScheduleHourly("hourly_reward", "Hourly Reward", 0, () => Debug.Log("Hourly Reward!"));

        // Offline grant on startup
        var offline = tm.GetOfflineSpan();
        if (offline > TimeSpan.Zero)
        {
            double minutes = offline.TotalMinutes;
            double gold = Math.Floor(minutes * 10); // ví dụ: 10 vàng/phút
            Debug.Log($"Welcome back! Offline {minutes:F1}m → +{gold} gold");
            // TODO: add gold to player wallet
        }
    }

    void OnApplicationPause(bool pause)
    {
        if (pause) TimeManager.Instance.SaveLastQuitUtc();
    }

    void OnApplicationQuit()
    {
        TimeManager.Instance.SaveLastQuitUtc();
    }
}