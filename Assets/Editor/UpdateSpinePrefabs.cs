using UnityEngine;
using UnityEditor;
using Spine.Unity;
using System.IO;
using System.Linq;

public class UpdateSpinePrefabs {
    [MenuItem("Tools/Update Spine Prefabs")]
    public static void UpdateAll() {
        string spineFolder = "Assets/SpineData/Battle";
        string prefabFolder = "Assets/AssetGame/ArtWork/Prefab/Role";
        
        if (!Directory.Exists(spineFolder)) {
            Debug.LogError($"Spine folder not found: {spineFolder}");
            return;
        }
        
        string[] spineDirs = Directory.GetDirectories(spineFolder);
        int count = 0;
        foreach (string dir in spineDirs) {
            string charName = Path.GetFileName(dir);
            // find skeleton data asset
            string skelPath = Directory.GetFiles(dir, "*_SkeletonData.asset", SearchOption.AllDirectories).FirstOrDefault();
            if (string.IsNullOrEmpty(skelPath)) {
                skelPath = Directory.GetFiles(dir, "*.asset", SearchOption.AllDirectories).FirstOrDefault(p => p.EndsWith("_SkeletonData.asset"));
            }
            if (string.IsNullOrEmpty(skelPath)) {
                Debug.LogWarning($"No SkeletonData found for {charName} in {dir}");
                continue;
            }
            
            SkeletonDataAsset skelData = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(skelPath);
            if (skelData == null) {
                Debug.LogWarning($"Failed to load SkeletonDataAsset at {skelPath}");
                continue;
            }
            
            // find matching prefab
            string[] allPrefabs = Directory.GetFiles(prefabFolder, "*.prefab", SearchOption.TopDirectoryOnly);
            string targetPrefabPath = null;
            
            string normalizedCharName = charName.Replace("_", "").ToLower();
            foreach (string pPath in allPrefabs) {
                string pName = Path.GetFileNameWithoutExtension(pPath);
                // skip avatars
                if (pName.StartsWith("avator_")) continue;
                
                string normalizedPName = pName.Replace("_", "").ToLower();
                if (normalizedPName == normalizedCharName) {
                    targetPrefabPath = pPath;
                    break;
                }
            }
            
            if (targetPrefabPath == null) {
                Debug.LogWarning($"No prefab found for {charName}");
                continue;
            }
            
            // Edit Prefab
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(targetPrefabPath);
            Transform modelTransform = prefabRoot.transform.Find("model");
            
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            Vector3 scale = Vector3.one * 0.63f; // Default from guide
            int siblingIndex = 0;
            
            if (modelTransform != null) {
                pos = modelTransform.localPosition;
                rot = modelTransform.localRotation;
                scale = modelTransform.localScale;
                siblingIndex = modelTransform.GetSiblingIndex();
                GameObject.DestroyImmediate(modelTransform.gameObject);
            }
            
            GameObject newModel = new GameObject("model");
            newModel.transform.SetParent(prefabRoot.transform, false);
            newModel.transform.localPosition = pos;
            newModel.transform.localRotation = rot;
            newModel.transform.localScale = scale;
            newModel.transform.SetSiblingIndex(siblingIndex);
            
            SkeletonAnimation skelAnim = newModel.AddComponent<SkeletonAnimation>();
            skelAnim.skeletonDataAsset = skelData;
            skelAnim.Initialize(true);
            
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, targetPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            count++;
            Debug.Log($"Updated prefab: {targetPrefabPath} with {skelData.name}");
        }
        
        Debug.Log($"UpdateSpinePrefabs complete. Updated {count} prefabs.");
    }
}