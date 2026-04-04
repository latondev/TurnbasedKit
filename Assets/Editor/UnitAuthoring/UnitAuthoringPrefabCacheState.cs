#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Battle.Editor
{
    [FilePath("UserSettings/UnitAuthoringPrefabCacheState.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class UnitAuthoringPrefabCacheState
        : ScriptableSingleton<UnitAuthoringPrefabCacheState>
    {
        [SerializeField] private List<string> cachedPrefabPaths = new List<string>();
        [SerializeField] private bool isDirty = true;
        [SerializeField] private bool lastScanUsedFullProject;
        [SerializeField] private string prefabSearchFolderPath;
        [SerializeField] private string characterDataSearchFolderPath;
        [SerializeField] private string skeletonDataSearchFolderPath;
        [SerializeField] private List<string> cachedSkeletonDataPaths = new List<string>();

        public bool IsDirty
        {
            get { return isDirty; }
        }

        public bool LastScanUsedFullProject
        {
            get { return lastScanUsedFullProject; }
        }

        public string PrefabSearchFolderPath
        {
            get { return prefabSearchFolderPath; }
        }

        public string CharacterDataSearchFolderPath
        {
            get { return characterDataSearchFolderPath; }
        }

        public string SkeletonDataSearchFolderPath
        {
            get { return skeletonDataSearchFolderPath; }
        }

        public IReadOnlyList<string> GetCachedPrefabPaths()
        {
            return cachedPrefabPaths;
        }

        public IReadOnlyList<string> GetCachedSkeletonDataPaths()
        {
            return cachedSkeletonDataPaths;
        }

        public void SaveCache(IReadOnlyList<string> prefabPaths, bool fullProjectScan)
        {
            cachedPrefabPaths.Clear();
            if (prefabPaths != null && prefabPaths.Count > 0)
            {
                var dedupe = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < prefabPaths.Count; i++)
                {
                    string path = prefabPaths[i];
                    if (string.IsNullOrEmpty(path) || !dedupe.Add(path))
                    {
                        continue;
                    }

                    cachedPrefabPaths.Add(path);
                }
            }

            lastScanUsedFullProject = fullProjectScan;
            isDirty = false;
            Save(true);
        }

        public void SaveSearchFolders(string prefabFolderPath, string soFolderPath, string skeletonFolderPath)
        {
            bool changed = !string.Equals(prefabSearchFolderPath, prefabFolderPath, StringComparison.Ordinal)
                || !string.Equals(characterDataSearchFolderPath, soFolderPath, StringComparison.Ordinal)
                || !string.Equals(skeletonDataSearchFolderPath, skeletonFolderPath, StringComparison.Ordinal);
            if (!changed)
            {
                return;
            }

            prefabSearchFolderPath = prefabFolderPath;
            characterDataSearchFolderPath = soFolderPath;
            skeletonDataSearchFolderPath = skeletonFolderPath;
            Save(true);
        }

        public void SaveSkeletonDataCache(IReadOnlyList<string> skeletonDataPaths)
        {
            cachedSkeletonDataPaths.Clear();
            if (skeletonDataPaths != null && skeletonDataPaths.Count > 0)
            {
                var dedupe = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < skeletonDataPaths.Count; i++)
                {
                    string path = skeletonDataPaths[i];
                    if (string.IsNullOrEmpty(path) || !dedupe.Add(path))
                    {
                        continue;
                    }

                    cachedSkeletonDataPaths.Add(path);
                }
            }

            Save(true);
        }

        public void MarkDirty()
        {
            if (isDirty)
            {
                return;
            }

            isDirty = true;
            Save(true);
        }
    }
}

#endif
