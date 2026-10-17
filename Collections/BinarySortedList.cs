using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public class BinarySortedList<T> : List<T>, IList<T>, ICollection<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, IEnumerable<T>, IList, ICollection
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

        // =============================================================
        // Vollständige List<T>-API-Abdeckung (net10-Oberfläche),
        // jede Methode mit RW-Lock, damit kein Pfad ungesichert bleibt.
        // =============================================================

        public new int Capacity
        {
            get
            {
                using var readLock = new ReadLock(Lock);
                return base.Capacity;
            }
            set
            {
                using var writeLock = new WriteLock(Lock);
                base.Capacity = value;
            }
        }

        // Diese Collection ist schreibbar.
        public bool IsReadOnly => false;

        // Die Collection wird durch das RW-Lock synchronisiert.
        public bool IsSynchronized => true;

        // Synchronobjekt dieser Collection: das RW-Lock selbst.
        public object SyncRoot => Lock;

        public new void CopyTo(T[] array)
        {
            using var readLock = new ReadLock(Lock);
            base.CopyTo(array, 0);
        }

        // CopyTo mit Quellindex (net10-List<T>-Signatur: index zuerst, dann
        // Ziel-Array/-Index/-Anzahl), per RW-Lock abgesichert.
        public new void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            using var readLock = new ReadLock(Lock);
            base.CopyTo(index, array, arrayIndex, count);
        }

        // Index-Suchvarianten: net10-List<T>-Signatur hat das Element ERST
        // (IndexOf(T item, int index[, int count])) — nicht (int, T) wie in
        // früheren Frameworks. Hier per RW-Lock abgesichert.
        public new int IndexOf(T item, int startIndex)
        {
            using var readLock = new ReadLock(Lock);
            return base.IndexOf(item, startIndex);
        }

        public new int IndexOf(T item, int startIndex, int count)
        {
            using var readLock = new ReadLock(Lock);
            return base.IndexOf(item, startIndex, count);
        }

        public new int LastIndexOf(T item, int startIndex)
        {
            using var readLock = new ReadLock(Lock);
            return base.LastIndexOf(item, startIndex);
        }

        public new int LastIndexOf(T item, int startIndex, int count)
        {
            using var readLock = new ReadLock(Lock);
            return base.LastIndexOf(item, startIndex, count);
        }

        public new int EnsureCapacity(int capacity)
        {
            using var writeLock = new WriteLock(Lock);
            return base.EnsureCapacity(capacity);
        }

        public new void TrimExcess()
        {
            using var writeLock = new WriteLock(Lock);
            base.TrimExcess();
        }

        /// <summary>
        /// Binärsuche in der sortierten Liste (Basissemantik).
        /// </summary>
        public new int BinarySearch(T item)
        {
            using var readLock = new ReadLock(Lock);
            return base.BinarySearch(item);
        }

        public new int BinarySearch(T item, IComparer<T> comparer)
        {
            using var readLock = new ReadLock(Lock);
            return base.BinarySearch(item, comparer);
        }

        public new int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            using var readLock = new ReadLock(Lock);
            return base.BinarySearch(index, count, item, comparer);
        }

        public new void Sort()
        {
            using var writeLock = new WriteLock(Lock);
            base.Sort();
        }

        public new void Sort(int index, int count, IComparer<T> comparer)
        {
            using var writeLock = new WriteLock(Lock);
            base.Sort(index, count, comparer);
        }

        public new void Sort(Comparison<T> comparison)
        {
            using var writeLock = new WriteLock(Lock);
            base.Sort(comparison);
        }

        // Index-Suchvarianten: List<T> definiert dafür (abseits LastIndexOf(T))
        // keine Startindex-Überladungen; FindIndex/FindLastIndex (unten) sind
        // die entsprechenden, abgesicherten Suchpfade.

        public new int FindLastIndex(int startIndex, Predicate<T> match)
        {
            using var readLock = new ReadLock(Lock);
            return base.FindLastIndex(startIndex, match);
        }

        public new int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            using var readLock = new ReadLock(Lock);
            return base.FindLastIndex(startIndex, count, match);
        }

        /// <summary>
        /// Neue Liste mit einem Abschnitt dieser Liste (Schnappschuss, net10).
        /// </summary>
        public new List<T> Slice(int startIndex, int count)
        {
            using var readLock = new ReadLock(Lock);
            return base.Slice(startIndex, count);
        }

        /// <summary>
        /// Einfügen eines Abschnitts an der angegebenen Position (Basissemantik, nicht sortierend).
        /// </summary>
        public new void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            List<T> insertedItems = new List<T>();
            using (var writeLock = new WriteLock(Lock))
            {
                foreach (var elem in collection)
                {
                    base.Insert(index + insertedItems.Count, elem);
                    insertedItems.Add(elem);
                }
            }

            foreach (var item in insertedItems)
            {
                OnInsert(item);
            }
        }

        /// <summary>
        /// Lese-only-Ansicht basierend auf einem Schnappschuss (keine Live-Referenz
        /// auf den internen Speicher, dadurch thread-sicher).
        /// </summary>
        public new ReadOnlyCollection<T> AsReadOnly()
        {
            using var readLock = new ReadLock(Lock);
            return new ReadOnlyCollection<T>(base.ToArray());
        }

        private int InternalIndexOf(T item)
        {
            EnsureComparer();
            int a = 0;
            int b = base.Count - 1; // Unlocked Zugriff auf Basisklasse!

            while (a <= b)
            {
                int mid = a + ((b - a) >> 1);

                // WICHTIG: base[mid] nutzen, NICHT this[mid]!
                // this[mid] holt sonst ein neues ReadLock -> LockRecursionException!
                int cmp = Comparer.Compare(base[mid], item);

                if (cmp == 0)
                    return mid;
                if (cmp < 0)
                    a = mid + 1;
                else
                    b = mid - 1;
            }

            // Nicht gefunden: Einfeld-Konvention (~Einfügeposition) zurückgeben.
            // Alle Caller (Add, AddOrUpdate, IndexOf, Predecessor/Successor)
            // erwarten das Bit-Complement und rechnen damit: if (index < 0) index = ~index;
            return ~a;
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

            // Binärsuche nach dem RECHTS­TEN Element, das mit item vergleichsgleich ist.
            // Liefert den Index oder -1 (Doku-Kontrakt), unabhängig davon,
            // welche Werte (außer ±0) der Comparer zurückgibt.
            int lastFound = -1;
            int a = 0;
            int b = base.Count - 1;
            while (a <= b)
            {
                int mid = a + ((b - a) / 2);
                int cmp = Comparer.Compare(base[mid], item);
                if (cmp == 0)
                {
                    lastFound = mid;
                    a = mid + 1; // möglicherweise gibt es rechts noch gleiche
                }
                else if (cmp < 0)
                {
                    a = mid + 1;
                }
                else
                {
                    b = mid - 1;
                }
            }

            return lastFound;
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

        // =================================================================
        // =================================================================
        // Interface-Abdeckung (net10-Oberfläche)
        //
        // Generische Interfaces (IList<T>, ICollection<T>, IReadOnlyList<T>,
        // IReadOnlyCollection<T>, IEnumerable<T>): List<T> implementiert deren
        // Member implizit über öffentliche Methoden. Unsere eigenen public
        // locked-Member (this[index], Count, IndexOf, Insert, Remove, Clear,
        // Add, Contains, CopyTo, Sort, BinarySearch, ...) erfüllen die
        // Interface-Kontrakte dadurch automatisch und überschatten die
        // Basisversionen — jeder generische Interface-Aufruf läuft thus gesichert.
        //
        // Unspezifizierte IList / ICollection: List<T> implementiert deren
        // Member (Add(object), Remove(object), this[int], CopyTo(Array), ...,
        // GetEnumerator) explizit, und die in List<T> kompilierten Aufrufe
        // bleiben beim Basispfad — sie lassen sich von einer abgeleiteten
        // Klasse nicht umleiten (CS0539/CS0111, empirisch verifiziert).
        // BESCHRÄNKUNG: Aufrufe über unspezifizierte IList-/ICollection-
        // Referenzen sind nicht durch das RW-Lock abgesichert.
        // In multithreaded Kontexten bitte die konkrete Type oder IList<T>/
        // IReadOnlyList<T> verwenden — beide sind vollständig abgesichert.
        // Die folgenden public object-Overloads funktionieren bei direktem
        // Aufruf (konkret oder via Reflection) korrekt mit Lock.
        // =================================================================

        // ICollection<T>.Remove (bool) explizit verankern.
        bool ICollection<T>.Remove(T item) => Remove(item);

        // Object-Overloads (direkter Aufruf, mit Lock):
        public void Add(object value) => Add((T)value);
        public bool Contains(object value) => Contains((T)value);
        public int IndexOf(object value) => IndexOf((T)value);
        public int LastIndexOf(object value) => LastIndexOf((T)value);
        public void Insert(int index, object value) => Insert(index, (T)value);
        public void Remove(object value) => Remove((T)value);

        // ---- unspezifierte ICollection ----
        // CopyTo(Array) wird von List<T> implementiert und kann nicht explizit
        // neu implementiert werden (CS0539) — daher als public-Methode, die die
        // Interface-Anforderung per Signatur erfüllt.
        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank > 1)
                throw new ArgumentException("Array muss eindimensional sein", nameof(array));
            if (index < 0 || array.Length - index < Count)
                throw new ArgumentException("Index außerhalb des Ziels", nameof(index));

            T[] typedArray = array as T[];
            if (typedArray != null)
            {
                CopyTo(typedArray, index);
                return;
            }

            using var readLock = new ReadLock(Lock);
            for (int i = 0; i < base.Count; i++)
                array.SetValue(base[i], index + i);
        }

        public void CopyTo(Array array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank > 1)
                throw new ArgumentException("Array muss eindimensional sein", nameof(array));
            if (array.Length < Count)
                throw new ArgumentException("Array ist zu klein", nameof(array));

            T[] typedArray = array as T[];
            if (typedArray != null)
            {
                CopyTo(typedArray);
                return;
            }

            using var readLock = new ReadLock(Lock);
            for (int i = 0; i < base.Count; i++)
                array.SetValue(base[i], i);
        }

        bool ICollection.IsSynchronized => IsSynchronized;
        object ICollection.SyncRoot => SyncRoot;
    }

    public static class NaturalMergeSorter<T>
    {
        public static void SortArray(T[] a, IComparer<T> comparer)
        {
            if (a == null || a.Length <= 1) return;
            
            int n = a.Length;
            T[] b = new T[n];
            IComparer<T> comp = comparer ?? Comparer<T>.Default;

            T[] src = a;
            T[] dest = b;

            while (true)
            {
                int runCount = MergePass(src, dest, comp);
                
                // Wenn in dest nur noch 1 Run liegt, ist die Liste vollständig sortiert!
                if (runCount <= 1)
                {
                    // Falls das Endergebnis im temporären Puffer 'dest' (b) gelandet ist,
                    // kopieren wir es zurück in das Hauptarray 'a'
                    if (dest != a)
                    {
                        Array.Copy(dest, a, n);
                    }
                    break;
                }

                // Puffer tauschen (Ping-Pong)
                T[] temp = src;
                src = dest;
                dest = temp;
            }
        }

        private static int MergePass(T[] src, T[] dest, IComparer<T> comp)
        {
            int n = src.Length;
            int i = 0, destIdx = 0;
            int runCount = 0;

            while (i < n)
            {
                // 1. Ersten Run identifizieren
                int start1 = i;
                while (i < n - 1 && comp.Compare(src[i], src[i + 1]) <= 0) i++;
                i++;
                int end1 = i;

                // Wenn kein zweiter Run mehr folgt: Rest-Run übertragen
                if (i >= n)
                {
                    Array.Copy(src, start1, dest, destIdx, end1 - start1);
                    runCount++;
                    break;
                }

                // 2. Zweiten Run identifizieren
                int start2 = i;
                while (i < n - 1 && comp.Compare(src[i], src[i + 1]) <= 0) i++;
                i++;
                int end2 = i;

                // Beide Runs zusammenführen
                MergeRuns(src, dest, start1, end1, start2, end2, destIdx, comp);
                destIdx += (end1 - start1) + (end2 - start2);
                runCount++;
            }

            return runCount;
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