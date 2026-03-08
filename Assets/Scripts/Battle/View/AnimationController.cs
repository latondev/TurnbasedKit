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
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        public SkeletonAnimation SkeletonAnimation => skeletonAnimation;

        private bool initialize = false;

        private void Awake()
        {
            TryGetComponent(out animationHandle);
            TryGetComponent(out skeletonAnimation);

            if (animationHandle != null && !initialize)
            {
                initialize = true;
                animationHandle.Initialize();

                if (skeletonAnimation != null)
                {
                    skeletonAnimation.AnimationState.End += EndAnimation;
                    skeletonAnimation.AnimationState.Event += EventAnimation;
                }
            }
        }

        private void OnValidate()
        {
            TryGetComponent(out animationHandle);
            TryGetComponent(out skeletonAnimation);
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
            if (skeletonAnimation != null)
            {
                var currentTrackEntry = skeletonAnimation.AnimationState.GetCurrent(trackIndex);
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

    /// <summary>
    /// Animation Handle - wrapper for skeleton animation setup
    /// </summary>
    public class AnimationHandle : MonoBehaviour
    {
        [SerializeField] public SkeletonAnimation skeletonAnimation;
        [SerializeField] private int sortingOrder = 0;
        [SerializeField] private string sortingLayerName = "Default";

        public void Initialize()
        {
            if (skeletonAnimation == null)
            {
                TryGetComponent(out skeletonAnimation);
            }
        }

        public void PlayAnimation(string name, float mix, int layer, bool loop, bool isLast = false)
        {
            if (skeletonAnimation != null && !string.IsNullOrEmpty(name))
            {
                var animation = skeletonAnimation.Skeleton.Data.FindAnimation(name);
                if (animation != null)
                {
                    skeletonAnimation.AnimationState.SetAnimation(layer, animation, loop);
                }
            }
        }

        public void SetSortingOrder(int order, string layer = "Unit")
        {
            sortingOrder = order;
            sortingLayerName = layer;

            if (skeletonAnimation != null)
            {
                var meshRenderer = skeletonAnimation.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.sortingOrder = order;
                }
            }
        }

        public void ResetSortingOrder()
        {
            SetSortingOrder(2 - (int)transform.position.y);
        }

        public void SetFlipX(bool flip)
        {
            if (skeletonAnimation != null)
            {
                skeletonAnimation.skeleton.ScaleX = flip ? -1f : 1f;
            }
        }

        public void SetSpeed(float speed)
        {
            if (skeletonAnimation != null)
            {
                skeletonAnimation.timeScale = speed;
            }
        }
    }
}
