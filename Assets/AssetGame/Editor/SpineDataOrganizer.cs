using UnityEngine;
using UnityEditor;
using System.IO;

public class SpineDataOrganizer
{
    private const string TargetFolder = "Assets/SpineData/Battle";

    // Helper: Lấy đúng đường dẫn thư mục đang được click chuột phải
    private static string GetSelectedFolderPath()
    {
        // Lấy các asset đang được chọn qua GUID để chính xác nhất trong Project Window
        string[] guids = Selection.assetGUIDs;
        if (guids.Length == 0) return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        
        // Kiểm tra xem path có đúng là folder không
        if (AssetDatabase.IsValidFolder(path))
        {
            return path;
        }

        return null;
    }

    /// <summary>
    /// Move selected folder to Battle folder - appears in context menu
    /// </summary>
    [MenuItem("Assets/Move to Battle", true)]
    private static bool MoveToBattleValidation()
    {
        string path = GetSelectedFolderPath();
        
        if (string.IsNullOrEmpty(path))
            return false;

        // Must not already be in target folder
        if (path.StartsWith(TargetFolder))
            return false;

        return true;
    }

    [MenuItem("Assets/Move to Battle", false, 18)] // Thêm priority để nhóm menu đẹp hơn
    private static void MoveToBattle()
    {
        string sourcePath = GetSelectedFolderPath();
        if (string.IsNullOrEmpty(sourcePath)) return;

        MoveFolderToBattle(sourcePath);
    }

    /// <summary>
    /// Copy selected folder to Battle folder (keep original)
    /// </summary>
    [MenuItem("Assets/Copy to Battle", true)]
    private static bool CopyToBattleValidation()
    {
        string path = GetSelectedFolderPath();
        
        if (string.IsNullOrEmpty(path))
            return false;

        if (path.StartsWith(TargetFolder))
            return false;

        return true;
    }

    //[MenuItem("Assets/Copy to Battle", false, 19)]
    private static void CopyToBattle()
    {
        string sourcePath = GetSelectedFolderPath();
        if (string.IsNullOrEmpty(sourcePath)) return;

        CopyFolderToBattle(sourcePath);
    }

    private static void MoveFolderToBattle(string sourcePath)
    {
        string folderName = Path.GetFileName(sourcePath);
        string targetPath = Path.Combine(TargetFolder, folderName).Replace("\\", "/");

        if (AssetDatabase.IsValidFolder(targetPath))
        {
            EditorUtility.DisplayDialog("Error", $"Folder '{folderName}' already exists in {TargetFolder}!", "OK");
            return;
        }

        EnsureFolderExists(TargetFolder);

        string result = AssetDatabase.MoveAsset(sourcePath, targetPath);
        if (string.IsNullOrEmpty(result))
        {
            Debug.Log($"Moved '{folderName}' to {TargetFolder}");
        }
        else
        {
            Debug.LogError($"Failed to move folder: {result}");
        }
    }

    private static void CopyFolderToBattle(string sourcePath)
    {
        string folderName = Path.GetFileName(sourcePath);
        string targetPath = Path.Combine(TargetFolder, folderName).Replace("\\", "/");

        if (AssetDatabase.IsValidFolder(targetPath))
        {
            EditorUtility.DisplayDialog("Error", $"Folder '{folderName}' already exists in {TargetFolder}!", "OK");
            return;
        }

        EnsureFolderExists(TargetFolder);

        bool success = AssetDatabase.CopyAsset(sourcePath, targetPath);
        if (success)
        {
            Debug.Log($"Copied '{folderName}' to {TargetFolder}");
        }
        else
        {
            Debug.LogError($"Failed to copy folder");
        }
    }

    private static void EnsureFolderExists(string targetDir)
    {
        if (AssetDatabase.IsValidFolder(targetDir))
            return;

        string[] parts = targetDir.Split('/');
        string currentPath = "";
        foreach (string part in parts)
        {
            string nextPath = string.IsNullOrEmpty(currentPath) ? part : currentPath + "/" + part;
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath == "" ? "Assets" : currentPath, part);
            }
            currentPath = nextPath;
        }
    }
}
