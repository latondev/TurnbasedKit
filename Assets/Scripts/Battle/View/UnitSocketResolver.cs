using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Battle
{
    public enum UnitSocketPoint
    {
        None = 0,
        UIPos = 1,
        BuffTop = 2,
        BuffMiddle = 3,
        BuffBottom = 4,
        FlyStart = 5,
        PetPos = 6,
    }

    /// <summary>
    /// Caches named socket transforms on a battle unit prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnitSocketResolver : MonoBehaviour
    {
        [SerializeField] private Transform uiPos;
        [SerializeField] private Transform buffTop;
        [SerializeField] private Transform buffMiddle;
        [SerializeField] private Transform buffBottom;
        [SerializeField] private Transform flyStart;
        [SerializeField] private Transform petPos;
        [SerializeField] private bool autoRefresh = true;

        private readonly Dictionary<UnitSocketPoint, Transform> _sockets = new Dictionary<UnitSocketPoint, Transform>();

        public Transform UIPos => GetSocket(UnitSocketPoint.UIPos);
        public Transform BuffTop => GetSocket(UnitSocketPoint.BuffTop);
        public Transform BuffMiddle => GetSocket(UnitSocketPoint.BuffMiddle);
        public Transform BuffBottom => GetSocket(UnitSocketPoint.BuffBottom);
        public Transform FlyStart => GetSocket(UnitSocketPoint.FlyStart);
        public Transform PetPos => GetSocket(UnitSocketPoint.PetPos);

        private void Awake()
        {
            RefreshCache();
        }

        private void OnEnable()
        {
            if (autoRefresh)
            {
                RefreshCache();
            }
        }

        private void OnValidate()
        {
            if (autoRefresh)
            {
                RefreshCache();
            }
        }

        public void RefreshCache()
        {
            _sockets.Clear();
            CacheSocket(UnitSocketPoint.UIPos, ref uiPos, "UIPos");
            CacheSocket(UnitSocketPoint.BuffTop, ref buffTop, "BuffTop");
            CacheSocket(UnitSocketPoint.BuffMiddle, ref buffMiddle, "BuffMiddle");
            CacheSocket(UnitSocketPoint.BuffBottom, ref buffBottom, "BuffBottom");
            CacheSocket(UnitSocketPoint.FlyStart, ref flyStart, "FlyStart");
            CacheSocket(UnitSocketPoint.PetPos, ref petPos, "PetPos");
        }

        public bool TryGetSocket(UnitSocketPoint point, out Transform socket)
        {
            socket = GetSocket(point);
            return socket != null;
        }

        public Transform GetSocket(UnitSocketPoint point)
        {
            if (point == UnitSocketPoint.None)
            {
                return transform;
            }

            if (_sockets.TryGetValue(point, out var socket) && socket != null)
            {
                return socket;
            }

            RefreshCache();
            if (_sockets.TryGetValue(point, out socket) && socket != null)
            {
                return socket;
            }

            return transform;
        }

        public Vector3 GetSocketWorldPosition(UnitSocketPoint point)
        {
            var socket = GetSocket(point);
            return socket != null ? socket.position : transform.position;
        }

        private void CacheSocket(UnitSocketPoint point, ref Transform field, string socketName)
        {
            if (field == null)
            {
                field = FindChildRecursive(transform, socketName);
            }

            if (field != null)
            {
                _sockets[point] = field;
            }
        }

        private static Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            if (string.Equals(root.name, targetName, StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var found = FindChildRecursive(child, targetName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
