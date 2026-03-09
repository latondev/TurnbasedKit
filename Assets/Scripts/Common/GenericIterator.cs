using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameSystems.Common
{
    /// <summary>
    /// Simple collection wrapper with index-based access.
    /// Replaces cumbersome Iterator Pattern for random-access lists.
    /// </summary>
    [Serializable]
    public class ItemCollection<T> where T : class
    {
        [SerializeField] protected List<T> items = new List<T>();
        [SerializeField] protected int currentIndex = -1;

        public List<T> Items => items;
        public int Count => items.Count;
        public int CurrentIndex
        {
            get => currentIndex;
            set => currentIndex = Mathf.Clamp(value, -1, items.Count - 1);
        }

        public T Current => currentIndex >= 0 && currentIndex < items.Count ? items[currentIndex] : null;
        public bool HasSelection => currentIndex >= 0 && currentIndex < items.Count;
        public bool IsEmpty => items.Count == 0;

        public ItemCollection()
        {
            items = new List<T>();
        }

        public virtual void Initialize()
        {
            if (items.Count > 0 && currentIndex < 0)
                currentIndex = 0;
        }

        public void Add(T item) => items.Add(item);
        public bool Remove(T item) => items.Remove(item);
        public void Clear() { items.Clear(); currentIndex = -1; }

        public T GetAt(int index)
        {
            return index >= 0 && index < items.Count ? items[index] : null;
        }

        public bool SetIndex(int index)
        {
            if (index >= 0 && index < items.Count)
            {
                currentIndex = index;
                return true;
            }
            return false;
        }

        public bool MoveNext()
        {
            if (currentIndex < items.Count - 1)
            {
                currentIndex++;
                return true;
            }
            return false;
        }

        public bool MovePrevious()
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                return true;
            }
            return false;
        }

        public bool MoveFirst()
        {
            if (items.Count > 0)
            {
                currentIndex = 0;
                return true;
            }
            return false;
        }

        public bool MoveLast()
        {
            if (items.Count > 0)
            {
                currentIndex = items.Count - 1;
                return true;
            }
            return false;
        }

        // Convenience methods returning T (for API compatibility)
        public T Next() => MoveNext() ? Current : null;
        public T Previous() => MovePrevious() ? Current : null;
        public T First() => MoveFirst() ? Current : null;
        public T Last() => MoveLast() ? Current : null;

        // Query methods
        public int GetTotalIterations() => items.Count;
        public bool HasNext() => currentIndex < items.Count - 1;
        public bool HasPrevious() => currentIndex > 0;
        public int GetCurrentIndex() => currentIndex;
    }
}
