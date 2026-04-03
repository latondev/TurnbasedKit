#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;

public class OpenInAntigravity
{
    private const string ANTIGRAVITY_PREF_KEY = "OpenInAntigravity.ExecutablePath";

    [MenuItem("Assets/Open Project in Antigravity", false, 0)]
    private static void OpenProjectInAntigravity()
    {
        // Lấy thư mục gốc của project thay vì chỉ Assets
        string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        Debug.Log($"[Antigravity] Opening project path: {projectPath}");
        OpenInApp(projectPath);
    }

    [MenuItem("Assets/Open Project in Antigravity", true)]
    private static bool ValidateOpenProjectInAntigravity()
    {
        return true;
    }

    [MenuItem("Assets/Antigravity/Set Executable Path...", false, 1)]
    private static void SetAntigravityExecutablePath()
    {
        string currentPath = EditorPrefs.GetString(ANTIGRAVITY_PREF_KEY, string.Empty);
        string selectedPath = EditorUtility.OpenFilePanel(
            "Chọn Antigravity.exe",
            string.IsNullOrWhiteSpace(currentPath)
                ? Environment.CurrentDirectory
                : Path.GetDirectoryName(currentPath),
            "exe"
        );

        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        if (!File.Exists(selectedPath))
        {
            EditorUtility.DisplayDialog("Lỗi", "File được chọn không tồn tại.", "OK");
            return;
        }

        EditorPrefs.SetString(ANTIGRAVITY_PREF_KEY, selectedPath);
        EditorUtility.DisplayDialog("Antigravity", "Đã lưu đường dẫn Antigravity.exe.", "OK");
    }

    private static void OpenInApp(string filePath)
    {
        string executablePath = ResolveAntigravityExecutable();
        if (string.IsNullOrEmpty(executablePath))
        {
            EditorUtility.DisplayDialog(
                "Không tìm thấy Antigravity",
                "Không tìm thấy Antigravity trên máy này.\n\n"
                    + "Hãy cài Antigravity hoặc dùng menu Assets/Antigravity/Set Executable Path... để trỏ đúng file Antigravity.exe.",
                "OK"
            );
            return;
        }

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(startInfo);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Antigravity] Lỗi khi mở: {e.Message}");
            EditorUtility.DisplayDialog("Lỗi", $"Không thể mở Antigravity.\n\n{e.Message}", "OK");
        }
    }

    private static string ResolveAntigravityExecutable()
    {
        string preferredPath = EditorPrefs.GetString(ANTIGRAVITY_PREF_KEY, string.Empty);
        if (IsValidExecutable(preferredPath))
        {
            return preferredPath;
        }

        string envPath = Environment.GetEnvironmentVariable("ANTIGRAVITY_EXECUTABLE");
        if (IsValidExecutable(envPath))
        {
            return envPath;
        }

        string localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData
        );
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string programFilesX86 = Environment.GetFolderPath(
            Environment.SpecialFolder.ProgramFilesX86
        );

        string[] candidatePaths =
        {
            Path.Combine(localAppData, "Programs", "Antigravity", "Antigravity.exe"),
            Path.Combine(programFiles, "Antigravity", "Antigravity.exe"),
            Path.Combine(programFilesX86, "Antigravity", "Antigravity.exe"),
        };

        for (int i = 0; i < candidatePaths.Length; i++)
        {
            if (IsValidExecutable(candidatePaths[i]))
            {
                return candidatePaths[i];
            }
        }

        return string.Empty;
    }

    private static bool IsValidExecutable(string path)
    {
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }
}
#endif
