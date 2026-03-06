using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Common
{
    /// <summary>
    /// Generic Iterator for traversing collections
    /// </summary>
    [Serializable]
    public class GenericIterator<T> where T : class
    {
        private readonly IList<T> _collection;
        private int _currentIndex;

        public T Current => _currentIndex >= 0 && _currentIndex < _collection.Count
            ? _collection[_currentIndex]
            : default(T);

        public int CurrentIndex => _currentIndex;

        public GenericIterator(IList<T> collection, int startIndex = 0)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _currentIndex = startIndex;
        }

        public bool HasNext() => _currentIndex < _collection.Count - 1;
        public bool HasPrevious() => _currentIndex > 0;

        public T Next()
        {
            if (HasNext()) _currentIndex++;
            return Current;
        }

        public T Previous()
        {
            if (HasPrevious()) _currentIndex--;
            return Current;
        }

        public void Reset() => _currentIndex = 0;

        public void SetIndex(int index)
        {
            if (index >= 0 && index < _collection.Count)
                _currentIndex = index;
        }
    }

    /// <summary>
    /// Generic IteratorData base class for collections
    /// </summary>
    [Serializable]
    public class GenericIteratorData<T> where T : class
    {
        [SerializeField] protected List<T> collection = new List<T>();
        [SerializeField] protected GenericIterator<T> currentIterator;

        public List<T> Collection => collection;
        public GenericIterator<T> CurrentIterator => currentIterator;
        public T Current => currentIterator?.Current;

        public GenericIteratorData()
        {
            collection = new List<T>();
        }

        public virtual void Initialize()
        {
            if (collection.Count > 0)
                currentIterator = new GenericIterator<T>(collection, 0);
        }

        public void AddItem(T item) => collection.Add(item);
        public bool RemoveItem(T item) => collection.Remove(item);
        public void Clear() { collection.Clear(); currentIterator = null; }

        public virtual T Next()
        {
            if (currentIterator == null) Initialize();
            return currentIterator?.Next();
        }

        public virtual T Previous()
        {
            if (currentIterator == null) Initialize();
            return currentIterator?.Previous();
        }

        public virtual T First()
        {
            if (currentIterator == null) Initialize();
            currentIterator?.Reset();
            return Current;
        }

        public virtual T Last()
        {
            if (currentIterator == null) Initialize();
            if (collection.Count > 0) currentIterator?.SetIndex(collection.Count - 1);
            return Current;
        }

        public int GetCurrentIndex() => currentIterator?.CurrentIndex ?? -1;
        public int GetTotalIterations() => collection.Count;
        public bool IsEmpty() => collection.Count == 0;
        public bool HasNext() => currentIterator?.HasNext() ?? false;
        public bool HasPrevious() => currentIterator?.HasPrevious() ?? false;
    }
}
