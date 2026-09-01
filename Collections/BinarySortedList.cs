using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;

namespace KS.Foundation
{
    public readonly struct ReadLock : IDisposable
    {
        private readonly ReaderWriterLockSlim _rwLock;

        public ReadLock(ReaderWriterLockSlim rwLock)
        {
            _rwLock = rwLock;
            _rwLock?.EnterReadLock();
        }

        public void Dispose() => _rwLock?.ExitReadLock();
    }

    public readonly struct WriteLock : IDisposable
    {
        private readonly ReaderWriterLockSlim _rwLock;

        public WriteLock(ReaderWriterLockSlim rwLock)
        {
            _rwLock = rwLock;
            _rwLock?.EnterWriteLock();
        }

        public void Dispose() => _rwLock?.ExitWriteLock();
    }

    public class BinarySortedList<T> : List<T>, IEnumerable<T>
    {
        [NonSerialized]
        private ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Greift thread-sicher auf das Lock zu und initialisiert es nach einer Deserialisierung automatisch neu.
        /// </summary>
        private ReaderWriterLockSlim Lock => _lock ??= new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public ReaderWriterLockSlim RWLock => Lock;

        public IComparer<T> Comparer { get; protected set; } = Comparer<T>.Default;

        protected const int DefaultCapacity = 31;

        public BinarySortedList() : base(DefaultCapacity) { }
        public BinarySortedList(int capacity) : base(capacity) { }
        public BinarySortedList(IComparer<T> comparer) : this(DefaultCapacity, comparer) { }
        public BinarySortedList(int capacity, IComparer<T> comparer) : base(capacity)
        {
            Comparer = comparer ?? Comparer<T>.Default;
        }

        public BinarySortedList(IEnumerable<T> collection, IComparer<T> comparer = null) : this()
        {
            Comparer = comparer ?? Comparer<T>.Default;
            if (collection != null)
            {
                base.AddRange(collection);
                InternalNaturalMergeSort(Comparer);
            }
        }

        [OnDeserializing]
        protected virtual void OnDeserializing(StreamingContext context)
        {
        }

        [OnDeserialized]
        protected virtual void OnDeserialized(StreamingContext context)
        {
            EnsureComparer();
        }

        public new int Count
        {
            get
            {
                using var readLock = new ReadLock(Lock);
                return base.Count;
            }
        }

        // Für den internen Gebrauch, wenn bereits ein Lock gehalten wird:
        public int UnlockedCount => base.Count;
        public T UnlockedItemByIndex(int index) => base[index];

        public new T this[int index]
        {
            get
            {
                using var readLock = new ReadLock(Lock);
                return base[index];
            }
            set
            {
                using (var writeLock = new WriteLock(Lock))
                {
                    base[index] = value;
                }
                OnInsert(value);
            }
        }

        public T First
        {
            get
            {
                using var readLock = new ReadLock(Lock);
                return base.Count > 0 ? base[0] : default;
            }
        }

        public T Last
        {
            get
            {
                using var readLock = new ReadLock(Lock);
                return base.Count > 0 ? base[base.Count - 1] : default;
            }
        }

        public new void Add(T elem)
        {
            using (var writeLock = new WriteLock(Lock))
            {
                int index = InternalIndexOf(elem);
                if (index < 0)
                    index = ~index;

                if (index >= base.Count)
                    base.Add(elem);
                else
                    base.Insert(index, elem);
            }

            OnInsert(elem);
        }

        public void AddLastUnlocked(T elem)
        {
            base.Add(elem);
            OnInsert(elem);
        }

        public void AddFirst(T elem) => Insert(0, elem);

        /// <summary>
        /// You can't find or delete after this
        /// </summary>
        public void AddLast(T elem)
        {
            using (var writeLock = new WriteLock(Lock))
            {
                base.Add(elem);
            }

            OnInsert(elem);
        }

        public new void AddRange(IEnumerable<T> collection)
        {
            if (collection == null)
                return;

            List<T> insertedItems = new List<T>();

            using (var writeLock = new WriteLock(Lock))
            {
                foreach (var elem in collection)
                {
                    base.Add(elem);
                    insertedItems.Add(elem);
                }

                InternalNaturalMergeSort(Comparer);
            }

            foreach (var item in insertedItems)
            {
                OnInsert(item);
            }
        }

        public void AddRangeUnsorted(IEnumerable<T> source)
        {
            if (source == null)
                return;

            List<T> insertedItems = new List<T>();

            using (var writeLock = new WriteLock(Lock))
            {
                foreach (var elem in source)
                {
                    base.Add(elem);
                    insertedItems.Add(elem);
                }
            }

            foreach (var item in insertedItems)
            {
                OnInsert(item);
            }
        }

        public new void Insert(int index, T elem)
        {
            if (elem == null)
                return;

            using (var writeLock = new WriteLock(Lock))
            {
                base.Insert(index, elem);
            }

            OnInsert(elem);
        }

        public int AddOrUpdate(T elem)
        {
            int index;
            using (var writeLock = new WriteLock(Lock))
            {
                index = InternalIndexOf(elem);
                if (index < 0)
                {
                    index = ~index;
                    if (index >= base.Count)
                        base.Add(elem);
                    else
                        base.Insert(index, elem);
                }
                else
                {
                    base[index] = elem;
                }
            }

            OnInsert(elem);
            return index;
        }

        public new bool Remove(T item)
        {
            bool removed;
            using (var writeLock = new WriteLock(Lock))
            {
                removed = base.Remove(item);
            }

            if (removed)
            {
                OnRemove(item);
            }

            return removed;
        }

        public new void RemoveAt(int index)
        {
            T item = default;
            bool removed = false;

            using (var writeLock = new WriteLock(Lock))
            {
                if (index >= 0 && index < base.Count)
                {
                    item = base[index];
                    base.RemoveAt(index);
                    removed = true;
                }
            }

            if (removed)
            {
                OnRemove(item);
            }
        }

        public void RemoveFirst()
        {
            T item = default;
            bool removed = false;

            using (var writeLock = new WriteLock(Lock))
            {
                if (base.Count > 0)
                {
                    item = base[0];
                    base.RemoveAt(0);
                    removed = true;
                }
            }

            if (removed)
            {
                OnRemove(item);
            }
        }

        public void RemoveLast()
        {
            T item = default;
            bool removed = false;

            using (var writeLock = new WriteLock(Lock))
            {
                if (base.Count > 0)
                {
                    int lastIndex = base.Count - 1;
                    item = base[lastIndex];
                    base.RemoveAt(lastIndex);
                    removed = true;
                }
            }

            if (removed)
            {
                OnRemove(item);
            }
        }

        public new int RemoveAll(Predicate<T> match)
        {
            if (match == null)
                return 0;

            List<T> removedItems = new List<T>();
            int count = 0;

            using (var writeLock = new WriteLock(Lock))
            {
                // Elemente sammeln, die entfernt werden, um OnRemove danach sicher aufzurufen
                for (int i = base.Count - 1; i >= 0; i--)
                {
                    if (match(base[i]))
                    {
                        removedItems.Add(base[i]);
                        base.RemoveAt(i);
                        count++;
                    }
                }
            }

            foreach (var item in removedItems)
            {
                OnRemove(item);
            }

            return count;
        }

        public new void RemoveRange(int index, int count)
        {
            List<T> removedItems = new List<T>();

            using (var writeLock = new WriteLock(Lock))
            {
                if (index < 0 || count < 0 || index + count > base.Count)
                    throw new ArgumentOutOfRangeException();

                for (int i = 0; i < count; i++)
                {
                    removedItems.Add(base[index + i]);
                }

                base.RemoveRange(index, count);
            }

            foreach (var item in removedItems)
            {
                OnRemove(item);
            }
        }

        public new void Reverse()
        {
            using var writeLock = new WriteLock(Lock);
            base.Reverse();
        }

        public new void Reverse(int index, int count)
        {
            using var writeLock = new WriteLock(Lock);
            base.Reverse(index, count);
        }

        public new bool Contains(T item)
        {
            using var readLock = new ReadLock(Lock);
            return base.Contains(item);
        }

        public new bool Exists(Predicate<T> match)
        {
            using var readLock = new ReadLock(Lock);
            return base.Exists(match);
        }

        public new T Find(Predicate<T> match)
        {
            using var readLock = new ReadLock(Lock);
            return base.Find(match);
        }

        public new List<T> FindAll(Predicate<T> match)
        {
            using var readLock = new ReadLock(Lock);
            return base.FindAll(match);
        }

        public new int FindIndex(Predicate<T> match)
        {
            using var readLock = new ReadLock(Lock);
            return base.FindIndex(match);
        }

        public new int FindIndex(int startIndex, Predicate<T> match)
        {
            using var readLock = new ReadLock(Lock);
            return base.FindIndex(startIndex, match);
        }

        public new int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            using var readLock = new ReadLock(Lock);
            return base.FindIndex(startIndex, count, match);
        }

        public new T FindLast(Predicate<T> match)
        {
            using var readLock = new ReadLock(Lock);
            return base.FindLast(match);
        }

        public new int FindLastIndex(Predicate<T> match)
        {
            using var readLock = new ReadLock(Lock);
            return base.FindLastIndex(match);
        }

        public new List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            using var readLock = new ReadLock(Lock);
            return base.ConvertAll(converter);
        }

        public new bool TrueForAll(Predicate<T> match)
        {
            using var readLock = new ReadLock(Lock);
            return base.TrueForAll(match);
        }

        public new void CopyTo(T[] array, int arrayIndex)
        {
            using var readLock = new ReadLock(Lock);
            base.CopyTo(array, arrayIndex);
        }

        public new List<T> GetRange(int index, int count)
        {
            using var readLock = new ReadLock(Lock);
            return base.GetRange(index, count);
        }

        public new void Clear()
        {
            using (var writeLock = new WriteLock(Lock))
            {
                base.Clear();
            }
        }

        private int InternalIndexOf(T item)
        {
            EnsureComparer();
            int a = 0;
            int b = base.Count - 1;

            while (a <= b)
            {
                int mid = a + ((b - a) >> 1);
                int cmp = Comparer.Compare(base[mid], item);

                if (cmp == 0)
                    return mid;
                if (cmp < 0)
                    a = mid + 1;
                else
                    b = mid - 1;
            }

            return a;
        }

        private int InternalFirstIndexOf(T item)
        {
            EnsureComparer();
            int a = 0;
            int b = base.Count - 1;

            while (a <= b)
            {
                int mid = a + ((b - a) >> 1);
                int cmp = Comparer.Compare(base[mid], item);

                if (cmp >= 0)
                    b = mid - 1;
                else
                    a = mid + 1;
            }

            if (a < base.Count && Comparer.Compare(base[a], item) == 0)
                return a;

            return -1;
        }

        private int InternalIndexOfElementOrPredecessor(T item)
        {
            int index = InternalIndexOf(item);
            if (index >= 0)
            {
                if (index < base.Count)
                    return index;

                return -1;
            }

            return ~index - 1;
        }

        public int IndexOfElementOrPredecessor(T item)
        {
            using var readLock = new ReadLock(Lock);
            return InternalIndexOfElementOrPredecessor(item);
        }

        private int InternalIndexOfElementOrSuccessor(T item)
        {
            int index = InternalIndexOf(item);
            if (index >= 0)
                return index;

            return ~index;
        }

        public int IndexOfElementOrSuccessor(T item)
        {
            using var readLock = new ReadLock(Lock);
            return InternalIndexOfElementOrSuccessor(item);
        }

        public T FindElementOrPredecessor(T item)
        {
            using var readLock = new ReadLock(Lock);
            int index = InternalIndexOfElementOrPredecessor(item);
            return index < 0 ? default : base[index];
        }

        public T FindElementOrSuccessor(T item)
        {
            using var readLock = new ReadLock(Lock);
            int index = InternalIndexOfElementOrSuccessor(item);
            return index >= base.Count ? default : base[index];
        }

        /// <summary>
        /// returns IndexOf or -1
        /// </summary>
        public new int IndexOf(T item)
        {
            using var readLock = new ReadLock(Lock);
            int idx = InternalIndexOf(item);
            return (idx < 0 || idx >= base.Count) ? -1 : idx;
        }

        public int FirstIndexOf(T item)
        {
            using var readLock = new ReadLock(Lock);
            return InternalFirstIndexOf(item);
        }

        public new int LastIndexOf(T item)
        {
            using var readLock = new ReadLock(Lock);
            EnsureComparer();

            int a = 0;
            int b = base.Count - 1;
            while (a <= b)
            {
                int mid = a + ((b - a) / 2);
                switch (Comparer.Compare(base[mid], item))
                {
                    case 1:
                        b = mid - 1;
                        break;
                    default:
                        a = mid + 1;
                        break;
                }
            }

            if (b < 0)
                return ~(b + 1);
            else if (a > b)
                return ~a;

            return b;
        }

        [DebuggerStepThrough]
        public new void ForEach(Action<T> action)
        {
            if (action == null)
                return;

            foreach (var item in ToArray())
            {
                action(item);
            }
        }

        private void EnsureComparer()
        {
            Comparer ??= Comparer<T>.Default;
        }

        public virtual void OnInsert(T elem) { }
        public virtual void OnRemove(T elem) { }

        public new void Sort(IComparer<T> comparer)
        {
            using var writeLock = new WriteLock(RWLock);
            base.Sort(comparer);
        }

        public void NaturalMergeSort() => NaturalMergeSort(Comparer);

        public virtual void NaturalMergeSort(IComparer<T> comparer)
        {
            using var writeLock = new WriteLock(RWLock);
            InternalNaturalMergeSort(comparer);
        }

        private void InternalNaturalMergeSort(IComparer<T> comparer)
        {
            IComparer<T> comp = comparer ?? Comparer<T>.Default;
            if (base.Count <= 1) return;

            T[] array = base.ToArray();
            NaturalMergeSorter<T>.SortArray(array, comp);

            for (int i = 0; i < base.Count; i++)
                base[i] = array[i];
        }

        public new T[] ToArray()
        {
            using var readLock = new ReadLock(Lock);
            return base.ToArray();
        }

        public new IEnumerator<T> GetEnumerator()
        {
            return new ThreadsafeEnumerator<T>(ToArray());
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }    

    public static class NaturalMergeSorter<T>
    {
        public static void SortArray(T[] a, IComparer<T> comparer)
        {
            if (a == null || a.Length <= 1) return;
            
            int n = a.Length;
            T[] b = new T[n];
            IComparer<T> comp = comparer ?? Comparer<T>.Default;

            bool srcInA = true;
            while (true)
            {
                bool sorted = srcInA ? MergePass(a, b, comp) : MergePass(b, a, comp);
                if (sorted) break;
                srcInA = !srcInA;
            }

            if (!srcInA)
                Array.Copy(b, a, n);
        }

        private static bool MergePass(T[] src, T[] dest, IComparer<T> comp)
        {
            int n = src.Length;
            int i = 0, destIdx = 0;
            int runCount = 0;

            while (i < n)
            {
                int start1 = i;
                while (i < n - 1 && comp.Compare(src[i], src[i + 1]) <= 0) i++;
                i++;
                int end1 = i;

                if (i >= n)
                {
                    Array.Copy(src, start1, dest, destIdx, end1 - start1);
                    runCount++;
                    break;
                }

                int start2 = i;
                while (i < n - 1 && comp.Compare(src[i], src[i + 1]) <= 0) i++;
                i++;
                int end2 = i;

                MergeRuns(src, dest, start1, end1, start2, end2, destIdx, comp);
                destIdx += (end1 - start1) + (end2 - start2);
                runCount++;
            }

            return runCount <= 1;
        }

        private static void MergeRuns(T[] src, T[] dest, int s1, int e1, int s2, int e2, int destIdx, IComparer<T> comp)
        {
            int i = s1, j = s2, k = destIdx;
            while (i < e1 && j < e2)
            {
                if (comp.Compare(src[i], src[j]) <= 0)
                    dest[k++] = src[i++];
                else
                    dest[k++] = src[j++];
            }
            while (i < e1) dest[k++] = src[i++];
            while (j < e2) dest[k++] = src[j++];
        }
    }
}