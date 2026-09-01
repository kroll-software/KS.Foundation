using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace KS.Foundation
{
    public class ConcurrentBalancedOrderStatisticTree<T> : IEnumerable<T> where T : IComparable<T>
    {
        private readonly BalancedOrderStatisticTree<T> _tree;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public ConcurrentBalancedOrderStatisticTree()
        {
            _tree = new BalancedOrderStatisticTree<T>();
        }

        public ConcurrentBalancedOrderStatisticTree(IComparer<T> comparer)
        {
            _tree = new BalancedOrderStatisticTree<T>(comparer);
        }

        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try { return _tree.Count; }
                finally { _lock.ExitReadLock(); }
            }
        }

        public T this[int index]
        {
            get
            {
                _lock.EnterReadLock();
                try { return _tree[index]; }
                finally { _lock.ExitReadLock(); }
            }
        }

        public void Add(T item)
        {
            _lock.EnterWriteLock();
            try { _tree.Add(item); }
            finally { _lock.ExitWriteLock(); }
        }

        public bool Remove(T item)
        {
            _lock.EnterWriteLock();
            try { return _tree.Remove(item); }
            finally { _lock.ExitWriteLock(); }
        }

        public bool RemoveAt(int index)
        {
            _lock.EnterWriteLock();
            try { return _tree.RemoveAt(index); }
            finally { _lock.ExitWriteLock(); }
        }

        public T PeekHead()
        {
            _lock.EnterReadLock();
            try { return _tree.PeekHead(); }
            finally { _lock.ExitReadLock(); }
        }

        public T PopHead()
        {
            _lock.EnterWriteLock();
            try { return _tree.PopHead(); }
            finally { _lock.ExitWriteLock(); }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try { _tree.Clear(); }
            finally { _lock.ExitWriteLock(); }
        }

        // Thread-sicherer Enumerator: Kopiert unter dem Read-Lock die Werte,
        // damit kein Thread während des Iterierens den Baum verändern kann.
        public IEnumerator<T> GetEnumerator()
        {
            List<T> snapshot;
            _lock.EnterReadLock();
            try
            {
                snapshot = new List<T>(_tree.Count);
                foreach (var item in _tree)
                {
                    snapshot.Add(item);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            return snapshot.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}