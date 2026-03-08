using System;
using System.Collections;
using System.Collections.Generic;
using Spine;
using UnityEngine;
using Spine.Unity;

[DisallowMultipleComponent]
public class AnimationHandle : MonoBehaviour
{
    public SkeletonAnimation skeletonAnimation { get; protected set; }
    private Dictionary<int, string> layerAnimator;
    MeshRenderer meshRenderer;
    private int sortingOrder;

    private int originalSortingOrder;
    private string originalSortingLayer;

    private void OnValidate()
    {
        skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
    }

    public virtual void Initialize()
    {
        layerAnimator ??= new Dictionary<int, string>();
        skeletonAnimation ??= GetComponentInChildren<SkeletonAnimation>();
        skeletonAnimation.Initialize(true);
        // Store the original sorting order and layer
        skeletonAnimation.gameObject.TryGetComponent<MeshRenderer>(out meshRenderer);

        originalSortingOrder = meshRenderer.sortingOrder;
        originalSortingLayer = meshRenderer.sortingLayerName;
    }

    public void SetSortingOrder(int order, string layerName)
    {
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = order;

            // Set sorting layer by name
            if (!string.IsNullOrEmpty(layerName))
            {
                meshRenderer.sortingLayerName = layerName;
                Debug.Log("ActiveFadeView " + transform.name + " ->> " + layerName + " ->> " + order);
            }
        }
        else
        {
            Debug.LogWarning("MeshRenderer is not assigned or found!");
        }
    }

    public void ResetSortingOrder()
    {
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = originalSortingOrder;

            if (!string.IsNullOrEmpty(originalSortingLayer))
            {
                meshRenderer.sortingLayerName = originalSortingLayer;
            }
        }
        else
        {
            Debug.LogWarning("MeshRenderer is not assigned or found!");
        }
    }

    public virtual void ResetAnimator()
    {
        layerAnimator.Clear();
        skeletonAnimation.ClearState();
    }

    public void SetSkin(string str)
    {
        skeletonAnimation.skeleton.SetSkin(str);
        skeletonAnimation.skeleton.SetSlotsToSetupPose();
        skeletonAnimation.AnimationState.Apply(skeletonAnimation.skeleton);
    }

    public void AddAnimation(Spine.Animation animation, int layer, bool isLoop, float delay)
    {
        skeletonAnimation.AnimationState.AddAnimation(layer, animation, isLoop, delay);
    }

    public void AddAnimation(string stateName, int layer, bool isLoop, float delay)
    {
        skeletonAnimation.AnimationState.AddAnimation(layer, stateName, isLoop, delay);
    }

    public void AddEmptyAnimation(int layer, float mixDuration, float delay)
    {
        skeletonAnimation.AnimationState.AddEmptyAnimation(layer, mixDuration, delay);
    }

    public void PlayAnimation(Spine.Animation animation, float mixDuration, int layer, bool isLoop, bool isLast = false)
    {
        if (animation == null)
        {
            if (layerAnimator.ContainsKey(layer))
            {
                layerAnimator[layer] = "";
            }

            skeletonAnimation.AnimationState.SetEmptyAnimation(layer, 0);
            return;
        }

        if (layerAnimator.ContainsKey(layer))
        {
            if (layerAnimator[layer].Equals(animation.Name))
            {
                if (isLoop)
                {
                    return;
                }
            }
            else
            {
                layerAnimator[layer] = animation.Name;
            }
        }
        else
        {
            if (isLoop)
            {
                layerAnimator.Add(layer, animation.Name);
            }
        }

        var a = skeletonAnimation.AnimationState.SetAnimation(layer, animation, isLoop);
        a.MixDuration = mixDuration;
        if (!isLoop && !isLast)
        {
            skeletonAnimation.AnimationState.AddEmptyAnimation(layer, 0, animation.Duration);
        }

        return;
    }

    public void PlayAnimation(string stateName, float mixDuration, int layer, bool isLoop, bool isLast = false)
    {
        var ani = GetAnimation(stateName);
        if (ani == null)
        {
            Debug.Log($"<b><color=red>_Log Animation null </color></b> : " + stateName + " name = " + skeletonAnimation.skeletonDataAsset.name);
        }

        if (layerAnimator == null)
        {
            return;
        }

        if (stateName.Length == 0)
        {
            if (layerAnimator.ContainsKey(layer))
            {
                if (isLoop)
                {
                    layerAnimator[layer] = stateName;
                }
            }
            else
            {
                if (isLoop)
                {
                    layerAnimator.Add(layer, stateName);
                }
            }

            skeletonAnimation.AnimationState.SetEmptyAnimation(layer, 0);
            return;
        }

        if (layerAnimator.ContainsKey(layer))
        {
            if (layerAnimator[layer].Equals(stateName))
            {
                if (isLoop)
                {
                    return;
                }
            }
            else
            {
                layerAnimator[layer] = stateName;
            }
        }
        else
        {
            if (isLoop)
            {
                layerAnimator.Add(layer, stateName);
            }
        }

        var trackEntry = skeletonAnimation.AnimationState.SetAnimation(layer, stateName, isLoop);
        trackEntry.MixDuration = mixDuration;
        if (!isLoop && !isLast)
        {
            skeletonAnimation.AnimationState.AddEmptyAnimation(layer, 0, trackEntry.Animation.Duration);
        }
    }

    public Spine.Animation GetAnimation(string animationName)
    {
        return skeletonAnimation.AnimationState.Data.SkeletonData.FindAnimation(animationName);
    }

    public float GetDuration(string animationName)
    {
        return skeletonAnimation.AnimationState.Data.SkeletonData.FindAnimation(animationName).Duration;
    }

    public void SetOrderInLayer(float layer)
    {
    }

    public string GetAniamtionCurrent()
    {
        Spine.AnimationState state = skeletonAnimation.AnimationState;
        TrackEntry trackEntry = state.GetCurrent(0); // 0 là index của track (thường là mặc định)
        string currentAnimationName = "";
        // Kiểm tra xem có animation nào đang chạy không
        if (trackEntry != null)
        {
            currentAnimationName = trackEntry.Animation.Name;
        }


        return currentAnimationName;
    }

    public void SetAnimationSpeed(float speed, int layer = 0)
    {
        // Get the current TrackEntry for the specified layer (default layer is 0)
        TrackEntry trackEntry = skeletonAnimation.AnimationState.GetCurrent(layer);

        if (trackEntry != null)
        {
            // Set the speed of the animation
            trackEntry.TimeScale = speed;
        }
        else
        {
            Debug.LogWarning($"No animation found on layer {layer} to set speed.");
        }
    }

    public void SetSpeed(float speed)
    {
        skeletonAnimation.timeScale = speed;
    }

    public void SetFlipX(bool b)
    {
        skeletonAnimation.initialFlipX = b;

        // Đặt lại skeleton về pose ban đầu
        skeletonAnimation.skeleton.SetToSetupPose();

        // Đảm bảo trạng thái animation được áp dụng
        skeletonAnimation.skeleton.FlipX = b; // Đặt trực tiếp FlipX
        skeletonAnimation.skeleton.SetSlotsToSetupPose();
        skeletonAnimation.AnimationState.Apply(skeletonAnimation.skeleton);

        // Cập nhật lại skeleton
        skeletonAnimation.LateUpdate();
    }
}