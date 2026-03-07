using UnityEngine;
using UnityEditor;
using System.IO;
using Spine.Unity;

public class ReplaceModelWithSpine : EditorWindow
{
    private static readonly string[] battlePrefabNames = new string[]
    {
        "bing_yi", "cao_yao", "chu_chu", "dao_ba_tu", "dong_huang_tai_yi",
        "fei_yu", "gou_mang", "hu_po", "hua_yao", "jian_ling",
        "jiang_hu_ke", "jing_wei", "lei_zhen_zi", "lei_zhen_zi_shou_ling",
        "mo_jian_shi", "pi_xiu", "pi_xiu_long_zi", "qi_hun", "shu_yao",
        "tao_tie", "tian_bing", "tian_jiang", "tian_lang_yao", "wu_sheng",
        "xiao_hun_dun", "xing_tian", "yang_jian", "yang_jian_shou_ling",
        "ying_long", "zhang_ma_zi"
    };

    private static readonly string prefabFolder = "Assets/AssetGame/ArtWork/Prefab/Role";
    private static readonly string spineDataFolder = "Assets/SpineData/Battle";

    [MenuItem("Tools/Replace Model With Spine")]
    public static void ShowWindow() => GetWindow<ReplaceModelWithSpine>("Replace Model");

    void OnGUI()
    {
        GUILayout.Label("Replace Model with SkeletonAnimation", EditorStyles.boldLabel);
        GUILayout.Space(10);
        if (GUILayout.Button("Execute Replace")) ExecuteReplace();
    }

    private void ExecuteReplace()
    {
        AssetDatabase.Refresh();
        int success = 0, fail = 0;

        foreach (string name in battlePrefabNames)
        {
            string prefabPath = $"{prefabFolder}/{name}.prefab";
            string spineName = name.Replace("_", "").ToLower();
            string skeletonPath = $"{spineDataFolder}/{spineName}/{spineName}_SkeletonData.asset";

            if (!AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(skeletonPath))
                skeletonPath = $"{spineDataFolder}/zhujue_{spineName}/zhujue_{spineName}_SkeletonData.asset";

            if (ProcessPrefab(prefabPath, skeletonPath))
            {
                success++;
                Debug.Log($"Success: {name}");
            }
            else
            {
                fail++;
                Debug.LogError($"Failed: {name}");
            }
        }

        Debug.Log($"Done! Success: {success}, Failed: {fail}");
    }

    private bool ProcessPrefab(string prefabPath, string skeletonPath)
    {
        var skeletonAsset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(skeletonPath);
        if (skeletonAsset == null)
        {
            Debug.LogError($"SkeletonDataAsset not found: {skeletonPath}");
            return false;
        }

        // Load prefab contents
        var instance = PrefabUtility.LoadPrefabContents(prefabPath);
        if (instance == null) return false;

        var model = instance.transform.Find("model");
        if (model == null) { PrefabUtility.UnloadPrefabContents(instance); return false; }

        // Get position and scale
        Vector3 pos = model.localPosition;
        Vector3 scale = model.localScale;

        // Remove existing components except Transform
        foreach (var c in model.GetComponents<Component>())
        {
            if (c != null && !(c is Transform)) DestroyImmediate(c, true);
        }

        // Add SkeletonAnimation
        var skel = model.gameObject.AddComponent<SkeletonAnimation>();
        skel.skeletonDataAsset = skeletonAsset;

        // Restore transform
        model.localPosition = pos;
        model.localScale = scale;

        // Save
        bool result = SavePrefab(instance, prefabPath);
        PrefabUtility.UnloadPrefabContents(instance);
        return result;
    }

    private bool SavePrefab(GameObject instance, string path)
    {
        string backup = path + ".bak";
        if (File.Exists(path)) File.Copy(path, backup, true);

        AssetDatabase.DeleteAsset(path);
        var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);

        if (prefab == null && File.Exists(backup))
        {
            File.Copy(backup, path, true);
            File.Delete(backup);
            return false;
        }

        if (File.Exists(backup)) File.Delete(backup);
        return prefab != null;
    }
}
