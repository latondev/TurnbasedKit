using System;
using System.Collections;
using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// Action Base - abstract base for Attack/Skill behaviors
    /// </summary>
    public abstract class ActionBase : MonoBehaviour
    {
        [Header("Settings")]
        public float dirType = 0;
        public Action<int, bool> OnEndStepAction;
        public Action OnEndAction;

        [Header("Animation")]
        [SerializeField] protected AnimationController animationHandle;
        [SerializeField] protected string moveGo = "run";
        [SerializeField] protected string moveBack = "run";

        [Header("Attack Settings")]
        [SerializeField] protected AttackRange attackRange;
        [SerializeField] protected Vector2 offsetExcute;
        [SerializeField] protected Vector2 targetPosition;
        [SerializeField] protected float speed = 1f;
        [SerializeField] protected Vector2 posOrigin;
        [SerializeField] protected ParticleSystem fxHit;
        [SerializeField] protected bool isHitEffect = false;

        protected const float duration = 0.4f;

        protected virtual void OnValidate()
        {
            TryGetComponent(out animationHandle);
        }

        public virtual void SetSpeed(float speed)
        {
            this.speed = speed;
        }

        protected virtual void Start()
        {
            posOrigin = transform.position;
        }

        protected void MoveToRoot(Action callback = null)
        {
            StartCoroutine(MoveToRootCoroutine(callback));
        }

        private IEnumerator MoveToRootCoroutine(Action callback)
        {
            float elapsed = 0;
            Vector3 start = transform.position;
            Vector3 end = posOrigin;
            float time = duration / speed;

            while (elapsed < time)
            {
                transform.position = Vector3.Lerp(start, end, elapsed / time);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = end;
            callback?.Invoke();
        }

        public void MoveToTarget(Vector2 targetPos, Action callback = null)
        {
            StartCoroutine(MoveToTargetCoroutine(targetPos, callback));
        }

        private IEnumerator MoveToTargetCoroutine(Vector2 targetPos, Action callback)
        {
            Vector3 target = targetPos + (Vector2)offsetExcute;
            float elapsed = 0;
            Vector3 start = transform.position;
            float time = duration / speed;

            while (elapsed < time)
            {
                transform.position = Vector3.Lerp(start, target, elapsed / time);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = target;
            callback?.Invoke();
        }

        public void SetTargetPosition(Vector2 targetPosition)
        {
            this.targetPosition = targetPosition;
        }

        public void ChangeSpeed(float spd)
        {
            this.speed = spd;
        }

        protected Vector2 GetTargetPosFinal()
        {
            Vector2 finalPosition = transform.position.x > targetPosition.x ? offsetExcute : -offsetExcute;
            targetPosition += finalPosition;
            return targetPosition;
        }

        public void ExcuteAction()
        {
            switch (attackRange)
            {
                case AttackRange.Melee:
                    OnMeleeAttack();
                    break;
                case AttackRange.Ranged:
                    OnRangedAttack();
                    break;
            }
        }

        public abstract void OnMeleeAttack();
        public abstract void OnRangedAttack();
    }

    public enum AttackRange
    {
        Melee,
        Ranged
    }
}
