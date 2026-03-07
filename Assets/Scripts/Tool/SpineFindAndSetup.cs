
#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using AYellowpaper.SerializedCollections;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class SpineFileInfo
{
    public string Name;           // Tên đầy đủ bao gồm suffix (vd: leizhenzi_0)
    public string BaseName;       // Tên gốc không có suffix (vd: leizhenzi)
    public string PathFileAtlas;
    public string PathFileSkel;
    public string PathFileImage;
    public bool IsMultiPack;      // Có phải multi-pack không
}

public enum SaveLocation
{
    InsideAssets,
    OutsideAssets
}

public enum FileOperation
{
    Copy,
    Move
}

public class SpineFindAndSetup : MonoBehaviour
{
    [SerializeField] private Object _folderTxtSpine;
    [SerializeField] private Object _folderImageSpine;

    [SerializeField] private SerializedDictionary<string, SpineFileInfo> _allSpineFiles = new();
    [SerializeField] private FileOperation _operationMode = FileOperation.Copy;
    private const string RootFolder = "Assets/SkeletonAsset";
    [SerializeField] private SaveLocation _saveLocation = SaveLocation.InsideAssets;

    [SerializeField]
    private string _outsideFolder;
    

    private static string ProjectRoot
    {
        get { return Path.GetDirectoryName(Application.dataPath); }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(_outsideFolder))
        {
            _outsideFolder = Path.Combine(ProjectRoot, "SpineOutput");
        }
#endif
    }

    [Button]
    public void Setup()
    {
        FindAllFileSpine();
        CreateFolderRoot();

        foreach (var kvp in _allSpineFiles)
        {
            var info = kvp.Value;
            string subFolder = CreateFolderSub(info.Name);

            // Copy từng file vào subFolder
            if (!string.IsNullOrEmpty(info.PathFileSkel))
                MoveFile(info.PathFileSkel, subFolder);

            if (!string.IsNullOrEmpty(info.PathFileAtlas))
                MoveFile(info.PathFileAtlas, subFolder);

            if (!string.IsNullOrEmpty(info.PathFileImage))
                MoveFile(info.PathFileImage, subFolder);
        }

        AssetDatabase.Refresh();
        Debug.Log("✅ Copy xong hết vào SkeletonAsset!");
    }

    public void FindAllFileSpine()
    {
#if UNITY_EDITOR
        _allSpineFiles.Clear();

        if (_folderTxtSpine == null || _folderImageSpine == null)
        {
            Debug.LogWarning("Chưa gán folder TxtSpine hoặc ImageSpine!");
            return;
        }

        string folderTxtPath = AssetDatabase.GetAssetPath(_folderTxtSpine);
        string folderImagePath = AssetDatabase.GetAssetPath(_folderImageSpine);

        if (!AssetDatabase.IsValidFolder(folderTxtPath) || !AssetDatabase.IsValidFolder(folderImagePath))
        {
            Debug.LogError("Object được gán không phải là folder!");
            return;
        }

        // 🔹 TỐI ƯU: Scan image 1 LẦN trước, lưu vào Dictionary
        Dictionary<string, string> imageCache = new();
        string[] imageGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderImagePath });

        foreach (string imgGuid in imageGuids)
        {
            string candidate = AssetDatabase.GUIDToAssetPath(imgGuid);
            string candidateName = Path.GetFileNameWithoutExtension(candidate);

            if (!imageCache.ContainsKey(candidateName))
            {
                imageCache[candidateName] = candidate;
            }
        }

        Debug.Log($"🔍 Đã cache {imageCache.Count} images");

        // Quét file .skel.bytes hoặc .json (Spine export có thể là .skel.bytes, .skel_0.bytes, hoặc .json)
        string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[] { folderTxtPath });

        foreach (string guid in guids)
        {
            string skelPath = AssetDatabase.GUIDToAssetPath(guid);

            // Bỏ qua .meta files và chỉ lấy skel/json files
            if (skelPath.EndsWith(".meta")) continue;
            if (!IsSpineSkeletonFile(skelPath)) continue;

            // Trích tên file đầy đủ và base name
            var (fullName, baseName, isMultiPack) = GetSpineFileNames(skelPath);

            // Skip nếu đã có base name này rồi (chỉ lấy cái đầu tiên)
            if (_allSpineFiles.ContainsKey(baseName)) continue;

            // Tim atlas - ho tro multi-pack (name_atlas_0.txt)
            string atlasPath = FindAtlasInDirectory(skelPath, fullName, baseName);

            // Tim image - ho tro multi-pack
            string imagePath = FindImageForSpine(imageCache, fullName, baseName, isMultiPack);

            var info = new SpineFileInfo()
            {
                Name = fullName,
                BaseName = baseName,
                IsMultiPack = isMultiPack,
                PathFileSkel = skelPath,
                PathFileAtlas = atlasPath,
                PathFileImage = imagePath
            };

            _allSpineFiles[baseName] = info;
        }

        Debug.Log($"Tìm thấy {_allSpineFiles.Count} Spine files!");
#endif
    }

    // 🔹 Tách hàm riêng để tái sử dụng và dùng LINQ tối ưu

    // Kiểm tra xem file có phải là Spine skeleton file không
    private bool IsSpineSkeletonFile(string path)
    {
        string lower = path.ToLower();
        // Hỗ trợ .skel.bytes, .skel_0.bytes, .skel_1.bytes, ... (bất kỳ số nào)
        // Hỗ trợ .json, .json_0, .json_1, ... (multi-pack JSON)
        return lower.EndsWith(".skel.bytes") ||
               System.Text.RegularExpressions.Regex.IsMatch(lower, @"\.skel_\d+\.bytes$") ||
               lower.EndsWith(".json") ||
               System.Text.RegularExpressions.Regex.IsMatch(lower, @"_\d+\.json$");
    }

    // Tra ve: (ten day du, ten base, co phai multi-pack hay khong)
    private (string fullName, string baseName, bool isMultiPack) GetSpineFileNames(string path)
    {
        string fileName = Path.GetFileName(path);
        string lower = fileName.ToLower();

        // Kiem tra multi-pack: .skel_0.bytes, .skel_1.bytes, ...
        var matchSkel = System.Text.RegularExpressions.Regex.Match(lower, @"^(.+)\.skel_(\d+)\.bytes$");
        if (matchSkel.Success)
        {
            string baseName = matchSkel.Groups[1].Value;
            string suffix = "_" + matchSkel.Groups[2].Value;
            return (baseName + suffix, baseName, true);
        }

        // .skel.bytes (khong co so)
        if (lower.EndsWith(".skel.bytes"))
        {
            string baseName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path));
            return (baseName, baseName, false);
        }

        // JSON multi-pack: .json_0, .json_1, ...
        var matchJson = System.Text.RegularExpressions.Regex.Match(lower, @"^(.+)_(\d+)\.json$");
        if (matchJson.Success)
        {
            string baseName = matchJson.Groups[1].Value;
            string suffix = "_" + matchJson.Groups[2].Value;
            return (baseName + suffix, baseName, true);
        }

        // .json (packed format, khong co so)
        if (lower.EndsWith(".json"))
        {
            string baseName = Path.GetFileNameWithoutExtension(path);
            return (baseName, baseName, false);
        }

        // Default
        return (fileName, fileName, false);
    }

    // Tim atlas - ho tro multi-pack (name_0.atlas.txt, name.atlas.txt)
    private string FindAtlasInDirectory(string skelPath, string fullName, string baseName)
    {
        string dirPath = Path.GetDirectoryName(skelPath);
        if (string.IsNullOrEmpty(dirPath)) return null;

        string fullDirPath = Path.Combine(Directory.GetCurrentDirectory(), dirPath);
        if (!Directory.Exists(fullDirPath)) return null;

        // Thu tim theo fullName truoc (multi-pack: leizhenzi_0.atlas.txt)
        var atlasFile = Directory.GetFiles(fullDirPath)
            .FirstOrDefault(f =>
            {
                string lower = f.ToLower();
                // Kiem tra: leizhenzi_0.atlas hoac leizhenzi_0.atlas.txt
                string nameNoExt = Path.GetFileNameWithoutExtension(f);
                return (nameNoExt == fullName || nameNoExt == fullName + ".atlas") &&
                       (lower.EndsWith(".atlas") || lower.EndsWith(".atlas.txt") || lower.EndsWith(".atlas.json"));
            });

        // Neu khong tim thay, tim theo baseName (leizhenzi.atlas)
        if (atlasFile == null)
        {
            atlasFile = Directory.GetFiles(fullDirPath)
                .FirstOrDefault(f =>
                {
                    string lower = f.ToLower();
                    string nameNoExt = Path.GetFileNameWithoutExtension(f);
                    return (nameNoExt == baseName || nameNoExt == baseName + ".atlas") &&
                           (lower.EndsWith(".atlas") || lower.EndsWith(".atlas.txt") || lower.EndsWith(".atlas.json"));
                });
        }

        return atlasFile != null
            ? atlasFile.Replace("\\", "/").Replace(Application.dataPath, "Assets")
            : null;
    }

    // Tim image - image luon la baseName (khong co _0)
    private string FindImageForSpine(Dictionary<string, string> imageCache, string fullName, string baseName, bool isMultiPack)
    {
        // Luon tim theo baseName (bo qua _0): leizhenzi.png
        if (imageCache.TryGetValue(baseName, out var img1))
        {
            return img1;
        }

        // Thu tim baseName + _Atlas: leizhenzi_Atlas
        if (imageCache.TryGetValue(baseName + "_Atlas", out var img2))
        {
            return img2;
        }

        return null;
    }

    public void CreateFolderRoot()
    {
#if UNITY_EDITOR
        if (!AssetDatabase.IsValidFolder(RootFolder))
        {
            AssetDatabase.CreateFolder("Assets", "SkeletonAsset");
        }
#endif
    }

    public string CreateFolderSub(string subFolderName)
    {
#if UNITY_EDITOR
        string subFolderPath = $"{RootFolder}/{subFolderName}";
        if (!AssetDatabase.IsValidFolder(subFolderPath))
        {
            AssetDatabase.CreateFolder(RootFolder, subFolderName);
        }

        return subFolderPath;
#else
        return null;
#endif
    }

    public void MoveFile(
        string from,
        string toFolder)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(toFolder)) return;

        string fileName = Path.GetFileName(from);
        string projectRoot = Path.GetDirectoryName(Application.dataPath); // ✅ gốc project

        if (_saveLocation == SaveLocation.InsideAssets)
        {
            // ---- Lưu vào trong Assets ----
            string toPath = $"{toFolder}/{fileName}";
            string absFrom = Path.Combine(projectRoot, from); // từ Assets/...
            string absTo = Path.Combine(projectRoot, toPath); // tới Assets/...

            if (_operationMode == FileOperation.Copy)
            {
                if (!File.Exists(absTo))
                {
                    File.Copy(absFrom, absTo, overwrite: true);
                    Debug.Log($"📂 Copied {from} ➝ {toPath}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ File {toPath} đã tồn tại, bỏ qua copy.");
                }
            }
            else if (_operationMode == FileOperation.Move)
            {
                if (File.Exists(absTo))
                {
                    Debug.LogWarning($"⚠️ File {toPath} đã tồn tại, bỏ qua move.");
                    return;
                }

                File.Move(absFrom, absTo);
                Debug.Log($"📂 Moved {from} ➝ {toPath}");
            }
        }
        else
        {
            // ---- Lưu ra ngoài Assets ----
            string absFrom = Path.Combine(projectRoot, from); // từ Assets/...
            string absTo = Path.Combine(toFolder, fileName); // toFolder đã là absolute

            if (_operationMode == FileOperation.Copy)
            {
                if (!File.Exists(absTo))
                {
                    File.Copy(absFrom, absTo, overwrite: true);
                    Debug.Log($"📂 Copied {from} ➝ {absTo}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ File {absTo} đã tồn tại, bỏ qua copy.");
                }
            }
            else if (_operationMode == FileOperation.Move)
            {
                if (File.Exists(absTo))
                {
                    Debug.LogWarning($"⚠️ File {absTo} đã tồn tại, bỏ qua move.");
                    return;
                }

                File.Move(absFrom, absTo);
                Debug.Log($"📂 Moved {from} ➝ {absTo}");
            }
        }
#endif
    }
}
#endif
