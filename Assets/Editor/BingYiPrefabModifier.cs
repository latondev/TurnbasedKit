// This script is designed to be run in Unity Editor to modify the bing_yi prefab
// Place this in Assets/Editor/ folder and run from Unity

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

public class BingYiPrefabModifier
{
    [MenuItem("Tools/Modify Bing Yi Prefab")]
    public static void ModifyBingYiPrefab()
    {
        string prefabPath = "Assets/AssetGame/ArtWork/Prefab/Role/bing_yi.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"Could not load prefab at path: {prefabPath}");
            return;
        }

        // Open the prefab for editing
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        // Find the "model" child object
        Transform modelTransform = instance.transform.Find("model");

        if (modelTransform == null)
        {
            Debug.LogError("Could not find 'model' child object");
            Object.DestroyImmediate(instance);
            return;
        }

        GameObject modelObject = modelTransform.gameObject;

        Debug.Log($"Found model object: {modelObject.name}");

        // Remove MeshFilter
        MeshFilter meshFilter = modelObject.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            Object.DestroyImmediate(meshFilter);
            Debug.Log("Removed MeshFilter");
        }
        else
        {
            Debug.Log("No MeshFilter found to remove");
        }

        // Remove MeshRenderer
        MeshRenderer meshRenderer = modelObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            Object.DestroyImmediate(meshRenderer);
            Debug.Log("Removed MeshRenderer");
        }
        else
        {
            Debug.Log("No MeshRenderer found to remove");
        }

        // Add or ensure SkeletonAnimation component
        Spine.Unity.SkeletonAnimation skeletonAnimation = modelObject.GetComponent<Spine.Unity.SkeletonAnimation>();
        if (skeletonAnimation == null)
        {
            skeletonAnimation = modelObject.AddComponent<Spine.Unity.SkeletonAnimation>();
            Debug.Log("Added SkeletonAnimation component");
        }
        else
        {
            Debug.Log("SkeletonAnimation already exists");
        }

        // Set the skeletonDataAsset
        string skeletonDataPath = "Assets/SpineData/Battle/bingyi/bingyi_SkeletonData.asset";
        Spine.Unity.SkeletonDataAsset skeletonDataAsset = AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>(skeletonDataPath);

        if (skeletonDataAsset != null)
        {
            skeletonAnimation.skeletonDataAsset = skeletonDataAsset;
            Debug.Log($"Set skeletonDataAsset to: {skeletonDataPath}");
        }
        else
        {
            Debug.LogError($"Could not load SkeletonDataAsset at path: {skeletonDataPath}");
        }

        // Reset position to [0, 0, 0] but keep the scale
        Vector3 currentScale = modelTransform.localScale;
        modelTransform.localPosition = Vector3.zero;
        Debug.Log($"Reset position to {Vector3.zero}, kept scale: {currentScale}");

        // Save the prefab back
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Debug.Log($"Saved prefab to: {prefabPath}");

        // Clean up the instance
        Object.DestroyImmediate(instance);

        Debug.Log("Bing Yi prefab modification complete!");
    }
}
#endif
