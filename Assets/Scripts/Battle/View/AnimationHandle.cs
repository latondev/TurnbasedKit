using Spine.Unity;
using UnityEngine;

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