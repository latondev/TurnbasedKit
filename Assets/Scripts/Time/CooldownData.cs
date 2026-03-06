using System;
using UnityEngine;

namespace GameSystems
{
    [Serializable]
    public class CooldownData
    {
        [SerializeField] private string id;
        [SerializeField] private float duration;
        [SerializeField] private float endTime; // Time.time mốc kết thúc
        [SerializeField] private bool active;

        public string Id => id;
        public float Duration => duration;
        public bool IsActive => active;

        public CooldownData(string id, float duration)
        {
            this.id = id;
            this.duration = duration;
            this.active = false;
            this.endTime = 0f;
        }

        public void StartNow()
        {
            active = true;
            endTime = Time.time + duration; // dùng mốc thời gian thay vì giảm từng frame [web:245]
        }

        public void Reset(float newDuration)
        {
            duration = newDuration;
            StartNow();
        }

        public float Remaining()
        {
            if (!active) return 0f;
            float remain = endTime - Time.time;
            return Mathf.Max(0f, remain);
        }

        public float Progress01()
        {
            if (!active || duration <= 0f) return 1f;
            float passed = duration - Remaining();
            return Mathf.Clamp01(passed / duration);
        }

        public bool Ready()
        {
            if (!active) return true;
            if (Time.time >= endTime)
            {
                active = false;
                return true;
            }
            return false;
        }
    }
}