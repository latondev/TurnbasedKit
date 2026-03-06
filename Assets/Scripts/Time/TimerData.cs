using System;
using UnityEngine;

namespace GameSystems
{
    [Serializable]
    public class TimerData
    {
        [SerializeField] private string id;
        [SerializeField] private float duration;
        [SerializeField] private float current; // đếm lên hoặc xuống
        [SerializeField] private bool running;
        [SerializeField] private bool repeat;
        [SerializeField] private bool countUp;

        public string Id => id;
        public float Duration => duration;
        public float Current => current;
        public bool Running => running;
        public bool Repeat => repeat;

        public event Action OnComplete;
        public event Action<float> OnTick;

        public TimerData(string id, float duration, bool repeat=false, bool countUp=false)
        {
            this.id=id; this.duration=duration; this.repeat=repeat; this.countUp=countUp;
            this.running=false; this.current=countUp?0f:duration;
        }

        public void Start()
        {
            running = true;
            current = countUp?0f:duration;
        }

        public void Stop()
        {
            running = false;
        }

        public void Update(float dt)
        {
            if (!running) return;

            if (countUp)
            {
                current += dt;
                OnTick?.Invoke(current);
                if (current >= duration)
                {
                    OnComplete?.Invoke();
                    if (repeat) current = 0f;
                    else running = false;
                }
            }
            else
            {
                current -= dt;
                OnTick?.Invoke(current);
                if (current <= 0f)
                {
                    OnComplete?.Invoke();
                    if (repeat) current = duration;
                    else { current = 0f; running = false; }
                }
            }
        }

        public float Progress01()
        {
            if (duration <= 0f) return 1f;
            return countUp ? Mathf.Clamp01(current/duration) : Mathf.Clamp01(1f - current/duration);
        }
    }
}
