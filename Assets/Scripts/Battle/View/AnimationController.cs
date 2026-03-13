using System;
using UnityEngine;
using Spine;
using Spine.Unity;

namespace GameSystems.Battle
{
    /// <summary>
    /// Animation Controller - handles Spine animations
    /// </summary>
    public class AnimationController : MonoBehaviour
    {
        public event Action<string, string> OnEventAnimation;
        public event Action<string> OnEndAnimation;

        [SerializeField] private AnimationHandle animationHandle;

        public SkeletonAnimation SkeletonAnimation => animationHandle?.skeletonAnimation;

        private bool initialize = false;

        private void Awake()
        {
            TryGetComponent(out animationHandle);
  

            if (animationHandle != null && !initialize)
            {
                initialize = true;
                animationHandle.Initialize();

                if (animationHandle.skeletonAnimation != null)
                {
                    animationHandle.skeletonAnimation.AnimationState.End += EndAnimation;
                    animationHandle.skeletonAnimation.AnimationState.Event += EventAnimation;
                }
            }
        }

        private void OnValidate()
        {
            TryGetComponent(out animationHandle);
        }

        void EventAnimation(TrackEntry trackentry, Spine.Event e)
        {
            OnEventAnimation?.Invoke(trackentry.Animation.Name, e.Data.Name);
        }

        void EndAnimation(TrackEntry trackentry)
        {
            OnEndAnimation?.Invoke(trackentry.Animation.Name);
        }

        public void SetSortingOrder(int order, string layer = "Unit")
        {
            if (animationHandle != null)
            {
                animationHandle.SetSortingOrder(order, layer);
            }
        }

        public void PlayAnimation(string nameAnimation, float mix, int layer, bool loop, bool isLast = false)
        {
            if (animationHandle != null && !string.IsNullOrEmpty(nameAnimation))
            {
                animationHandle.PlayAnimation(nameAnimation, mix, layer, loop, isLast);
            }
        }

        public string GetCurrentAnimationName(int trackIndex = 0)
        {
            if (animationHandle != null && animationHandle.skeletonAnimation != null)
            {
                var currentTrackEntry = animationHandle.skeletonAnimation.AnimationState.GetCurrent(trackIndex);
                if (currentTrackEntry != null)
                {
                    return currentTrackEntry.Animation.Name;
                }
            }
            return string.Empty;
        }

        public void ResetSortingOrder()
        {
            if (animationHandle != null)
            {
                animationHandle.ResetSortingOrder();
            }
        }

        public void SetFlipX(bool flip)
        {
            if (animationHandle != null)
            {
                animationHandle.SetFlipX(flip);
            }
        }

        public void SetSpeed(float speed)
        {
            if (animationHandle != null)
            {
                animationHandle.SetSpeed(speed);
            }
        }
    }

   
}
