using System;
using Spine;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// Low-level Spine adapter used by battle views and prefabs.
/// Owns SkeletonAnimation and forwards Spine animation events.
/// </summary>
public class AnimationHandle : MonoBehaviour
{
    [SerializeField] public SkeletonAnimation skeletonAnimation;
    [SerializeField] private int sortingOrder = 0;
    [SerializeField] private string sortingLayerName = "Default";

    public event Action<string, string> OnEventAnimation;
    public event Action<string> OnEndAnimation;

    private bool _eventsBound;

    public void Initialize()
    {
        if (skeletonAnimation == null)
        {
            TryGetComponent(out skeletonAnimation);
        }

        if (skeletonAnimation == null || _eventsBound)
        {
            return;
        }

        skeletonAnimation.AnimationState.End += HandleEndAnimation;
        skeletonAnimation.AnimationState.Event += HandleEventAnimation;
        _eventsBound = true;
    }

    private void OnEnable()
    {
        Initialize();
    }

    public void PlayAnimation(string name, float mix, int layer, bool loop, bool isLast = false)
    {
        if (skeletonAnimation != null && !string.IsNullOrEmpty(name))
        {
            Initialize();

            var animation = skeletonAnimation.Skeleton.Data.FindAnimation(name);
            if (animation != null)
            {
                var trackEntry = skeletonAnimation.AnimationState.SetAnimation(layer, animation, loop);
                ApplyMixDuration(trackEntry, mix);
            }
        }
    }

    public bool TryPlayAnimation(string primaryName, string fallbackName, float mix, int layer, bool loop, bool isLast = false)
    {
        if (TryPlayAnimationInternal(primaryName, mix, layer, loop, isLast))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(fallbackName) &&
            !string.Equals(primaryName, fallbackName, StringComparison.OrdinalIgnoreCase))
        {
            return TryPlayAnimationInternal(fallbackName, mix, layer, loop, isLast);
        }

        return false;
    }

    private bool TryPlayAnimationInternal(string name, float mix, int layer, bool loop, bool isLast)
    {
        if (skeletonAnimation == null || string.IsNullOrEmpty(name))
        {
            return false;
        }

        Initialize();

        if (skeletonAnimation.Skeleton == null || skeletonAnimation.Skeleton.Data == null)
        {
            return false;
        }

        var animation = skeletonAnimation.Skeleton.Data.FindAnimation(name);
        if (animation == null)
        {
            return false;
        }

        var trackEntry = skeletonAnimation.AnimationState.SetAnimation(layer, animation, loop);
        ApplyMixDuration(trackEntry, mix);
        return true;
    }

    private static void ApplyMixDuration(TrackEntry trackEntry, float mix)
    {
        if (trackEntry == null)
        {
            return;
        }

        trackEntry.MixDuration = Mathf.Max(0f, mix);
    }

    public string GetCurrentAnimationName(int trackIndex = 0)
    {
        if (skeletonAnimation == null)
        {
            return string.Empty;
        }

        var currentTrackEntry = skeletonAnimation.AnimationState.GetCurrent(trackIndex);
        return currentTrackEntry != null ? currentTrackEntry.Animation.Name : string.Empty;
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

    public void ResetAnimationState()
    {
        if (skeletonAnimation == null)
        {
            return;
        }

        Initialize();
        if (skeletonAnimation.AnimationState != null)
        {
            skeletonAnimation.AnimationState.ClearTracks();
            if (skeletonAnimation.Skeleton != null)
            {
                skeletonAnimation.Skeleton.SetToSetupPose();
                skeletonAnimation.AnimationState.Apply(skeletonAnimation.Skeleton);
            }
        }
    }

    public void ClearTrack(int trackIndex)
    {
        if (skeletonAnimation == null)
        {
            return;
        }

        Initialize();
        if (skeletonAnimation.AnimationState != null)
        {
            skeletonAnimation.AnimationState.ClearTrack(trackIndex);
        }
    }

    private void HandleEventAnimation(TrackEntry trackentry, Spine.Event e)
    {
        OnEventAnimation?.Invoke(trackentry.Animation.Name, e.Data.Name);
    }

    private void HandleEndAnimation(TrackEntry trackentry)
    {
        OnEndAnimation?.Invoke(trackentry.Animation.Name);
    }

    private void OnDestroy()
    {
        if (skeletonAnimation != null && _eventsBound)
        {
            skeletonAnimation.AnimationState.End -= HandleEndAnimation;
            skeletonAnimation.AnimationState.Event -= HandleEventAnimation;
            _eventsBound = false;
        }
    }
}
