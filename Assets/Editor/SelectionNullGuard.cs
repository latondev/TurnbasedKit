#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GameSystems.Battle.Editor
{
    [InitializeOnLoad]
    public static class SelectionNullGuard
    {
        static SelectionNullGuard()
        {
            Selection.selectionChanged += SanitizeSelection;
            EditorApplication.delayCall += SanitizeSelection;
            EditorApplication.update += SanitizeSelection;
        }

        private static void SanitizeSelection()
        {
            int[] ids = Selection.instanceIDs;
            if (ids == null || ids.Length == 0)
            {
                return;
            }

            int validCount = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                var obj = EditorUtility.InstanceIDToObject(ids[i]);
                if (obj != null)
                {
                    ids[validCount++] = ids[i];
                }
            }

            if (validCount == ids.Length)
            {
                return;
            }

            if (validCount == 0)
            {
                Selection.instanceIDs = new int[0];
                Selection.activeObject = null;
                return;
            }

            var trimmed = new int[validCount];
            for (int i = 0; i < validCount; i++)
            {
                trimmed[i] = ids[i];
            }

            Selection.instanceIDs = trimmed;
            if (Selection.activeObject == null)
            {
                Selection.activeObject = EditorUtility.InstanceIDToObject(trimmed[0]);
            }
        }
    }
}

#endif
