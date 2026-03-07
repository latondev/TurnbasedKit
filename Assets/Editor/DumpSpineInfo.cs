using UnityEngine;
using UnityEditor;
using Spine.Unity;
using Spine;
using System.IO;
using System.Text;
using System.Linq;

public class DumpSpineInfo {
    [MenuItem("Tools/Dump Spine Info")]
    public static void Dump() {
        string spineFolder = "Assets/SpineData/Battle";
        string[] skelAssets = Directory.GetFiles(spineFolder, "*_SkeletonData.asset", SearchOption.AllDirectories);
        if (skelAssets.Length == 0) {
            skelAssets = Directory.GetFiles(spineFolder, "*.asset", SearchOption.AllDirectories).Where(p => p.EndsWith("_SkeletonData.asset")).ToArray();
        }
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# Spine Animations and Events\n");
        
        foreach (string skelPath in skelAssets) {
            SkeletonDataAsset skelDataAsset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(skelPath);
            if (skelDataAsset == null) continue;
            
            SkeletonData skelData = skelDataAsset.GetSkeletonData(true);
            if (skelData == null) continue;
            
            sb.AppendLine($"## Character: {skelDataAsset.name.Replace("_SkeletonData", "")}");
            sb.AppendLine("### Animations:");
            foreach (var anim in skelData.Animations) {
                sb.AppendLine($"- `{anim.Name}`");
            }
            
            sb.AppendLine("### Events:");
            if (skelData.Events.Count == 0) {
                sb.AppendLine("- *(None)*");
            } else {
                foreach (var evt in skelData.Events) {
                    sb.AppendLine($"- `{evt.Name}`");
                }
            }
            sb.AppendLine();
        }
        
        string outputPath = "Assets/SpineInfo.md";
        File.WriteAllText(outputPath, sb.ToString());
        Debug.Log($"Dumped Spine info to {outputPath}");
    }
}