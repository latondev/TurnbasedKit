using System;
using UnityEngine;

namespace GameSystems
{
    public enum ScheduleType { Once, Hourly, Daily, Weekly }

    [Serializable]
    public class ScheduledEvent
    {
        [SerializeField] private string id;
        [SerializeField] private string name;
        [SerializeField] private ScheduleType type;
        [SerializeField] private DateTime nextUtc; // sử dụng UTC để ổn định
        [SerializeField] private DayOfWeek weeklyDay;
        [SerializeField] private int hour;
        [SerializeField] private int minute;
        [SerializeField] private bool active = true;

        public string Id => id;
        public string DisplayName => name;
        public DateTime NextUtc => nextUtc;
        public bool Active => active;

        public event Action OnTriggered;

        public ScheduledEvent(string id, string name, ScheduleType type, DateTime firstUtc, DayOfWeek weeklyDay=DayOfWeek.Monday, int hour=0, int minute=0)
        {
            this.id=id; this.name=name; this.type=type; this.nextUtc=firstUtc;
            this.weeklyDay=weeklyDay; this.hour=hour; this.minute=minute; this.active=true;
        }

        public bool ShouldTrigger(DateTime nowUtc)
        {
            return active && nowUtc >= nextUtc;
        }

        public void Trigger()
        {
            OnTriggered?.Invoke();
            switch (type)
            {
                case ScheduleType.Once: active=false; break;
                case ScheduleType.Hourly: nextUtc = nextUtc.AddHours(1); break;
                case ScheduleType.Daily: nextUtc = nextUtc.AddDays(1); break;
                case ScheduleType.Weekly: nextUtc = nextUtc.AddDays(7); break;
            }
        }

        public static DateTime NextDailyUtc(int hour, int minute)
        {
            var now = DateTime.UtcNow;
            var t = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0, DateTimeKind.Utc);
            if (t <= now) t = t.AddDays(1);
            return t;
        }

        public static DateTime NextWeeklyUtc(DayOfWeek day, int hour, int minute)
        {
            var now = DateTime.UtcNow;
            int days = ((int)day - (int)now.DayOfWeek + 7) % 7;
            var t = now.Date.AddDays(days).AddHours(hour).AddMinutes(minute);
            if (t <= now) t = t.AddDays(7);
            return t;
        }
    }
}
