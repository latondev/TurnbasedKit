using System;
using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;
using Event = UnityEngine.Event;

namespace MythydRpg
{
    public class AnimationController : MonoBehaviour
    {
        public Action<string, string> OnEventAnimation;
        public Action<string> OnEndAnimation;
        
        [SerializeField] protected AnimationHandle animationHandle;
        public SkeletonAnimation SkeletonAnimation => animationHandle.skeletonAnimation;

        private void OnValidate()
        {
            transform.TryGetComponentInChildren(out animationHandle);
           // SetSortingOrder(2 - (int)transform.position.y);
        }

        private void Awake()
        {
            if (animationHandle == null)
            {
                transform.TryGetComponentInChildren(out animationHandle);
            }

            if (!initialize)
            {
                initialize = true;
                animationHandle.Initialize();
                animationHandle.skeletonAnimation.AnimationState.End += EndAnimation;
                animationHandle.skeletonAnimation.AnimationState.Event += EventAnimation;
            }
        }

        private bool initialize = false;
        

        protected void Start()
        {
         
        }

        void EventAnimation(TrackEntry trackentry, Spine.Event e)
        {
            OnEventAnimation?.Invoke(trackentry.Animation.Name,e.Data.Name);
        }

        void EndAnimation(TrackEntry trackentry)
        {
            OnEndAnimation?.Invoke(trackentry.Animation.Name);
        }

        public void SetSortingOrder(int order,string layer ="Unit")
        {
            animationHandle.SetSortingOrder(order,layer);

        }

        public void PlayAnimation(string nameAnimation, float mix, int layer, bool loop,bool isLast=false)
        {
            if (nameAnimation == string.Empty)
            {
                
            }
            else
            {
                animationHandle.PlayAnimation(nameAnimation, mix, layer, loop,isLast);
            }
        }
        public string GetCurrentAnimationName(int trackIndex = 0)
        {
            // Kiểm tra track đầu tiên (trackIndex = 0)
            var currentTrackEntry = SkeletonAnimation.AnimationState.GetCurrent(trackIndex);
            if (currentTrackEntry != null)
            {
                return currentTrackEntry.Animation.Name;
            }
            return "No animation is playing";
        }

        public void ResetSortingOrder()
        {
            animationHandle.ResetSortingOrder();
        }

        public void SetFlipX(bool b)
        {
            animationHandle.SetFlipX(b);
        }

        public void SetSpeed(float speed)
        {
            animationHandle.SetSpeed(speed);
        }
    }
}