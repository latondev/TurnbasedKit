#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class OpenInAntigravity
{
    private const string ANTIGRAVITY_EXECUTABLE = @"C:\Users\Tuanvu\AppData\Local\Programs\Antigravity\Antigravity.exe";

    [MenuItem("Assets/Open Project in Antigravity", false, 0)]
    private static void OpenProjectInAntigravity()
    {
        // Luôn lấy thư mục Assets gốc của project
        string assetsPath = Application.dataPath;

        Debug.Log($"[Antigravity] Opening Assets path: {assetsPath}");
        OpenInApp(assetsPath);
    }

    [MenuItem("Assets/Open Project in Antigravity", true)]
    private static bool ValidateOpenProjectInAntigravity()
    {
        return true;
    }

    private static void OpenInApp(string filePath)
    {
        if (!File.Exists(ANTIGRAVITY_EXECUTABLE))
        {
            EditorUtility.DisplayDialog(
                "Không tìm thấy Antigravity",
                $"Không tìm thấy file:\n{ANTIGRAVITY_EXECUTABLE}\n\nHãy kiểm tra lại đường dẫn trong script.",
                "OK"
            );
            return;
        }

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ANTIGRAVITY_EXECUTABLE,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(startInfo);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Antigravity] Lỗi khi mở: {e.Message}");
            EditorUtility.DisplayDialog(
                "Lỗi",
                $"Không thể mở Antigravity.\n\n{e.Message}",
                "OK"
            );
        }
    }
}
#endif