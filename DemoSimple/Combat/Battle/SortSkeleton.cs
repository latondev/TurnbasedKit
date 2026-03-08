using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

public class SortSkeleton : MonoBehaviour
{
    private SkeletonAnimation skeletonAnimation;
    MeshRenderer meshRenderer;
    
    private void Awake()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        meshRenderer = skeletonAnimation.gameObject.GetComponent<MeshRenderer>();

    }
    public void SetSortingOrder(int order){
        meshRenderer.sortingOrder = order;
    }
    private void Update()
    {
        // if (skeletonAnimation != null)
        // {
        //     int sortingOrder = -Mathf.RoundToInt(transform.position.y * 10);
        //     meshRenderer.sortingOrder = sortingOrder;
        // }
    }
}
