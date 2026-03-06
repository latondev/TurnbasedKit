using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems
{
    public class TimeManager : MonoBehaviour
    {
        [Header("Settings")]
        [Range(0f,10f)] public float timeScale = 1f; // speed control
        public bool paused = false;

        [Header("Runtime")]
        [SerializeField] private List<CooldownData> cooldowns = new();
        [SerializeField] private List<TimerData> timers = new();
        [SerializeField] private List<ScheduledEvent> schedules = new();

        // Offline
        private const string LastQuitKey = "TIME_LAST_QUIT_UTC";

        // Events
        public event Action OnPaused;
        public event Action OnResumed;
        public event Action<float> OnTimeScaleChanged;

        // Singleton (optional)
        public static TimeManager Instance { get; private set; }

        void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); }
        }

        void Update()
        {
            if (paused) return;

            float dt = Time.deltaTime * timeScale; // pause bằng paused + scale, thay vì chỉ dựa vào Time.timeScale [web:237]
            // Update timers
            for (int i = 0; i < timers.Count; i++) timers[i].Update(dt);

            // Check schedules
            DateTime nowUtc = DateTime.UtcNow;
            for (int i = 0; i < schedules.Count; i++)
            {
                if (schedules[i].ShouldTrigger(nowUtc))
                    schedules[i].Trigger();
            }
        }

        #region Cooldowns
        public void StartCooldown(string id, float duration)
        {
            var cd = cooldowns.Find(c => c.Id == id);
            if (cd == null)
            {
                cd = new CooldownData(id, duration);
                cooldowns.Add(cd);
            }
            cd.Reset(duration);
        }

        public bool CooldownReady(string id)
        {
            var cd = cooldowns.Find(c => c.Id == id);
            return cd == null || cd.Ready();
        }

        public float CooldownRemaining(string id)
        {
            var cd = cooldowns.Find(c => c.Id == id);
            return cd == null ? 0f : cd.Remaining();
        }

        public float CooldownProgress01(string id)
        {
            var cd = cooldowns.Find(c => c.Id == id);
            return cd == null ? 1f : cd.Progress01();
        }
        #endregion

        #region Timers
        public TimerData CreateTimer(string id, float duration, bool repeat=false, bool countUp=false, Action onComplete=null, Action<float> onTick=null)
        {
            var t = timers.Find(x => x.Id == id);
            if (t == null)
            {
                t = new TimerData(id, duration, repeat, countUp);
                timers.Add(t);
            }
            else
            {
                // reconfigure
                t.Stop();
                t = new TimerData(id, duration, repeat, countUp);
                timers.RemoveAll(x => x.Id == id);
                timers.Add(t);
            }
            if (onComplete != null) t.OnComplete += onComplete;
            if (onTick != null) t.OnTick += onTick;
            return t;
        }

        public void StartTimer(string id)
        {
            var t = timers.Find(x => x.Id == id);
            t?.Start();
        }

        public void StopTimer(string id)
        {
            var t = timers.Find(x => x.Id == id);
            t?.Stop();
        }

        public float TimerProgress01(string id)
        {
            var t = timers.Find(x => x.Id == id);
            return t == null ? 0f : t.Progress01();
        }
        #endregion

        #region Schedules
        public ScheduledEvent ScheduleDaily(string id, string name, int hour, int minute, Action callback)
        {
            var s = new ScheduledEvent(id, name, ScheduleType.Daily, ScheduledEvent.NextDailyUtc(hour, minute), DayOfWeek.Monday, hour, minute);
            if (callback != null) s.OnTriggered += callback;
            schedules.Add(s);
            return s;
        }

        public ScheduledEvent ScheduleWeekly(string id, string name, DayOfWeek day, int hour, int minute, Action callback)
        {
            var s = new ScheduledEvent(id, name, ScheduleType.Weekly, ScheduledEvent.NextWeeklyUtc(day, hour, minute), day, hour, minute);
            if (callback != null) s.OnTriggered += callback;
            schedules.Add(s);
            return s;
        }

        public ScheduledEvent ScheduleHourly(string id, string name, int minute, Action callback)
        {
            var now = DateTime.UtcNow;
            var next = new DateTime(now.Year, now.Month, now.Day, now.Hour, minute, 0, DateTimeKind.Utc);
            if (next <= now) next = next.AddHours(1);
            var s = new ScheduledEvent(id, name, ScheduleType.Hourly, next);
            if (callback != null) s.OnTriggered += callback;
            schedules.Add(s);
            return s;
        }

        public ScheduledEvent ScheduleOnceUtc(string id, string name, DateTime utcTime, Action callback)
        {
            var s = new ScheduledEvent(id, name, ScheduleType.Once, utcTime);
            if (callback != null) s.OnTriggered += callback;
            schedules.Add(s);
            return s;
        }
        #endregion

        #region Pause / Scale
        public void SetPaused(bool p)
        {
            if (paused == p) return;
            paused = p;
            if (paused) OnPaused?.Invoke();
            else OnResumed?.Invoke();
        }

        public void SetTimeScale(float scale)
        {
            timeScale = Mathf.Clamp(scale, 0f, 10f);
            OnTimeScaleChanged?.Invoke(timeScale);
        }
        #endregion

        #region Offline Hooks
        public void SaveLastQuitUtc()
        {
            PlayerPrefs.SetString(LastQuitKey, DateTime.UtcNow.ToString("o"));
            PlayerPrefs.Save();
        }

        public TimeSpan GetOfflineSpan()
        {
            string iso = PlayerPrefs.GetString(LastQuitKey, "");
            if (string.IsNullOrEmpty(iso)) return TimeSpan.Zero;
            if (DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var last))
            {
                var diff = DateTime.UtcNow - last;
                return diff < TimeSpan.Zero ? TimeSpan.Zero : diff;
            }
            return TimeSpan.Zero;
        }
        #endregion
    }
}
