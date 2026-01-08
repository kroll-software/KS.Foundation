/*
{*******************************************************************}
{                                                                   }
{          KS-Foundation Library                                    }
{          Build rock solid DotNet applications                     }
{          on a threadsafe foundation without the hassle            }
{                                                                   }
{          Copyright (c) 2014 - 2018 by Kroll-Software,             }
{          Altdorf, Switzerland, All Rights Reserved                }
{          www.kroll-software.ch                                    }
{                                                                   }
{   Licensed under the MIT license                                  }
{   Please see LICENSE.txt for details                              }
{                                                                   }
{*******************************************************************}
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;

namespace KS.Foundation
{
	/// <summary>
	/// Reguläres struct für ReadLock (funktioniert mit C# 12 / .NET 8)
	/// </summary>
	public struct ReadLock : IDisposable
	{
		private readonly ReaderWriterLockSlim _rwLock;
		public ReadLock(ReaderWriterLockSlim rwLock)
		{
			_rwLock = rwLock;
			_rwLock.EnterReadLock();
		}
		public void Dispose() => _rwLock.ExitReadLock();
	}

	/// <summary>
	/// Reguläres struct für WriteLock (funktioniert mit C# 12 / .NET 8)
	/// </summary>
	public struct WriteLock : IDisposable
	{
		private readonly ReaderWriterLockSlim _rwLock;
		public WriteLock(ReaderWriterLockSlim rwLock)
		{
			_rwLock = rwLock;
			_rwLock.EnterWriteLock();
		}
		public void Dispose() => _rwLock.ExitWriteLock();
	}

    public class BinarySortedList<T> : List<T>, IEnumerable<T>
    {
        // Verwendung von ReadOnly Property-Definitionen
        public ReaderWriterLockSlim RWLock { get; } = new(LockRecursionPolicy.SupportsRecursion);

        [OnDeserializing]
        protected virtual void OnDeserializing(StreamingContext context)
        {
            // Initialisierung im Konstruktor ist besser, aber für Deserialisierung beibehalten.
            // Beachten: Die Signatur für OnDeserializing/OnDeserialized erfordert 'StreamingContext'.
            // Die RWLock-Initialisierung im Konstruktor überschreibt dies (siehe unten).
            // Da wir RWLock bereits initialisiert haben, ist dieser Hook nur bei einer 
            // tatsächlichen Deserialisierung von Bedeutung.
            // RWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        [OnDeserialized]
        protected virtual void OnDeserialized(StreamingContext context)
        {
            // Die Basisklasse (List<T>) hat keine OnDeserialized-Methode aufzurufen.
            // Wir ersetzen den ursprünglichen Aufruf durch die Logik von SetDefault/EnsureComparer.
            EnsureComparer();
        }        

        public IComparer<T> Comparer { get; protected set; } = Comparer<T>.Default;

        // Bessere Konvention: DefaultCapacity als const
        protected const int DefaultCapacity = 31;

        // Konstruktoren vereinfacht
        public BinarySortedList() : base(DefaultCapacity) { }
        public BinarySortedList(int capacity) : base(capacity) { }
        public BinarySortedList(IComparer<T> comparer) : this(DefaultCapacity, comparer) { }
        public BinarySortedList(int capacity, IComparer<T> comparer) : base(capacity)
        {
            Comparer = comparer ?? Comparer<T>.Default;
        }

        public BinarySortedList(IEnumerable<T> collection) : this(collection, null) { }
        public BinarySortedList(IEnumerable<T> collection, IComparer<T> comparer) : this()
        {
            Comparer = comparer ?? Comparer<T>.Default;
            if (collection != null)
            {
                base.AddRange(collection);
                NaturalMergeSort();
            }
        }

        // Getter vereinfacht mit Expression-Bodied Member und using-Deklaration
        public new int Count
        {
            get
            {
                using var readLock = new ReadLock(RWLock);
                return base.Count;
            }
        }

        public T UnlockedItemByIndex(int index) => base[index];

        public new T this[int index]
        {
            get
            {
                using var readLock = new ReadLock(RWLock);
                return base[index];
            }
        }

        public T First
        {
            get
            {
                using var readLock = new ReadLock(RWLock);
                return base.Count > 0 ? base[0] : default; // default anstelle von default(T)
            }
        }

        public T Last
        {
            get
            {
                using var readLock = new ReadLock(RWLock);
                return base.Count > 0 ? base[base.Count - 1] : default;
            }
        }

        public void RemoveFirst()
        {
            using var writeLock = new WriteLock(RWLock);

            if (base.Count > 0)
            {
                T item = base[0]; // Zugriff auf base[0] innerhalb des WriteLock
                base.RemoveAt(0);
                OnRemove(item);
            }
        }

        public void RemoveLast()
        {
            using var writeLock = new WriteLock(RWLock);

            if (base.Count > 0)
            {
                T item = base[base.Count - 1]; // Zugriff auf base[Count-1] innerhalb des WriteLock
                base.RemoveAt(base.Count - 1);
                OnRemove(item);
            }
        }

        public new bool Remove(T item)
        {
            using var writeLock = new WriteLock(RWLock);

            if (base.Remove(item))
            {
                OnRemove(item);
                return true;
            }
            return false;
        }

        public new void RemoveAt(int index)
        {
            using var writeLock = new WriteLock(RWLock);

            T item = base[index]; // Zugriff auf base[index] innerhalb des WriteLock
            base.RemoveAt(index);
            OnRemove(item);
        }

        // Methoden wie FindSafe, Find, FindValue, InternalIndexOf usw.
        // verwenden nun ebenfalls die 'using var readLock = new ReadLock(RWLock);' Syntax.

        /// <summary>
        /// This always returns a value, no bounds checks required
        /// Except when the list is empty, returns default(T)
        /// </summary>
        public T FindSafe(T search)
        {
            using var readLock = new ReadLock(RWLock);

            if (base.Count == 0)
                return default;

            EnsureComparer();

            // ... (Rest der Logik von FindSafe)
            int a = 0;
            int b = base.Count;
            while (b - a > 1)
            {
                int mid = (a + b) / 2;
                if (Comparer.Compare(base[mid], search) > 0)
                    b = mid;
                else
                    a = mid;
            }

            return base[a];
        }

        /// <summary>
        /// This always returns a value, no bounds checks required
        /// Except when the list is empty, returns default(T)
        /// </summary>
        public T Find(T elem)
        {
            using var readLock = new ReadLock(RWLock);

            int i = InternalIndexOf(elem);
            if (i >= 0 && i < base.Count)
                return base[i];

            return default;
        }

        public T FindValue(Func<T, int> CompareResult)
        {
            using var readLock = new ReadLock(RWLock);
            int a = 0;
            int b = base.Count;
            while (true)
            {
                if (a == b)
                    return default;

                int mid = a + ((b - a) / 2);
                switch (CompareResult(base[mid]))
                {
                    case -1:
                        a = mid + 1;
                        break;
                    case 1:
                        b = mid;
                        break;
                    case 0:
                        return base[mid];
                }
            }
        }

        public int AddOrUpdate(T elem)
        {
            using var writeLock = new WriteLock(RWLock);

            int index = InternalIndexOf(elem);
            if (index < 0)
                index = ~index;

            if (index >= base.Count)
                base.Add(elem);
            else
                base[index] = elem;

            OnInsert(elem);
            return index;
        }

        public virtual void OnInsert(T elem) { }
        public virtual void OnRemove(T elem) { }

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
            using var writeLock = new WriteLock(RWLock);
            base.Add(elem);
            OnInsert(elem);
        }

        public new void Add(T elem)
        {
            using var writeLock = new WriteLock(RWLock);
            int index = InternalIndexOf(elem);
            if (index < 0)
                index = ~index;

            if (index >= base.Count)
                base.Add(elem);
            else
                base.Insert(index, elem);

            OnInsert(elem);
        }

        public new void AddRange(IEnumerable<T> collection)
        {
            using var writeLock = new WriteLock(RWLock);
            if (collection == null)
                return;

            foreach (var elem in collection) // Ersetze .ForEach durch foreach
            {
                base.Add(elem);
                OnInsert(elem);
            }

            InternalNaturalMergeSort(Comparer);
        }

        public void AddRangeUnsorted(IEnumerable<T> source)
        {
            using var writeLock = new WriteLock(RWLock);
            if (source == null)
                return;

            foreach (var elem in source) // Ersetze .ForEach durch foreach
            {
                base.Add(elem);
                OnInsert(elem);
            }
        }

        public new void Insert(int index, T elem)
        {
            using var writeLock = new WriteLock(RWLock);

            if (elem == null)
                return;
            base.Insert(index, elem);
            OnInsert(elem);
        }

        // ... (Methoden wie InternalIndexOfElementOrPredecessor, IndexOfElementOrPredecessor, etc.
        // sind in ihrem Kern unverändert, verwenden aber ReadLock/WriteLock).
        
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
            using var readLock = new ReadLock(RWLock);
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
            using var readLock = new ReadLock(RWLock);
            return InternalIndexOfElementOrSuccessor(item);
        }

        public T FindElementOrPredecessor(T item)
        {
            using var readLock = new ReadLock(RWLock);
            int index = InternalIndexOfElementOrPredecessor(item);
            return index < 0 ? default : base[index];
        }

        public T FindElementOrSuccessor(T item)
        {
            using var readLock = new ReadLock(RWLock);
            int index = InternalIndexOfElementOrSuccessor(item);
            return index >= base.Count ? default : base[index];
        }

        // Entfernt: Auskommentierte Code-Teile in InternalIndexOf.
        private int InternalIndexOf(T item)
        {
            // Keine Sperre erforderlich, da dies eine interne Methode ist, die
            // innerhalb eines WriteLock oder ReadLock aufgerufen wird.

            EnsureComparer();

            int a = 0;
            int b = base.Count;
            while (true)
            {
                if (a == b)
                    return ~a;

                int mid = a + ((b - a) / 2);
                switch (Comparer.Compare(base[mid], item))
                {
                    case -1:
                        a = mid + 1;
                        break;
                    case 1:
                        b = mid;
                        break;
                    case 0:
                        return mid;
                }
            }
        }

        /// <summary>
        /// returns IndexOf or -1
        /// </summary>
        public new int IndexOf(T item)
        {
            using var readLock = new ReadLock(RWLock);
            int idx = InternalIndexOf(item);
            return (idx < 0 || idx >= base.Count) ? -1 : idx;
        }

        private void EnsureComparer()
        {
            // Diese Methode sollte innerhalb eines Locks aufgerufen werden, wenn der Comparer geändert werden könnte.
            // Da 'Comparer' eine protected Property ist, ist die Verwendung nur in den Read/Write Locks der 
            // BinarySortedList selbst oder in abgeleiteten Klassen 'sicher'.
            if (Comparer == null)
                Comparer = Comparer<T>.Default;
        }

        private int InternalFirstIndexOf(T item)
        {
            // Keine Sperre erforderlich
            EnsureComparer();

            int a = 0;
            int b = base.Count - 1;
            while (a <= b)
            {
                int mid = a + ((b - a) / 2);
                switch (Comparer.Compare(base[mid], item))
                {
                    case -1:
                        a = mid + 1;
                        break;
                    default:
                        b = mid - 1;
                        break;
                }
            }

            if (b < 0)
                return ~(b + 1);
            else if (a >= base.Count)
                return ~a;

            return b + 1;
        }

        public int FirstIndexOf(T item)
        {
            using var readLock = new ReadLock(RWLock);
            return InternalFirstIndexOf(item);
        }

        public new int LastIndexOf(T item)
        {
            using var readLock = new ReadLock(RWLock);
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

        public virtual new void Clear()
        {
            using var writeLock = new WriteLock(RWLock);
            base.Clear();
        }

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

            // NaturalMergeSorter wird jetzt mit Komposition verwendet.
            var sorter = new NaturalMergeSorter<T>();
            T[] res = base.ToArray();
            sorter.Sort(ref res, comp);

            // Kopiert das sortierte Array zurück in die Basisliste
            for (int i = 0; i < base.Count; i++)
                base[i] = res[i];
        }

        public new T[] ToArray()
        {
            using var readLock = new ReadLock(RWLock);
            return base.ToArray();
        }

        [DebuggerStepThrough]
        public new void ForEach(Action<T> action)
        {
            // Ersetze die fehlerhafte Enumerator-Logik durch eine einfache Enumeration
            // des threadsicheren Arrays (wie im Original beabsichtigt).
            if (action == null)
                return;
                
            foreach (var item in ToArray()) 
            {
                action(item);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public new IEnumerator<T> GetEnumerator()
        {
            // Gibt einen Enumerator über eine threadsichere Kopie des Arrays zurück
            return new ThreadsafeEnumerator<T>(ToArray());
        }
    }

    // *** NaturalMergeSorter und ThreadsafeEnumerator sind in Ordnung,
    // können aber ebenfalls mit 'default' und '??' modernisiert werden ***    

    public class NaturalMergeSorter<T>
	{
		private T[] a;
		private T[] b;    // Hilfsarray
		private int n;

		// Verwendung des Null-Coalescing-Operators ?? im Setter ist moderner
		private IComparer<T> comp = Comparer<T>.Default;

		public void Sort(ref T[] a, IComparer<T> comparer)
		{
			this.a = a;
			n = a.Length;
			// Das Hilfsarray muss dieselbe Größe haben wie das zu sortierende Array
			b = new T[n];

			// Verwende Null-Coalescing-Operator '??'
			comp = comparer ?? Comparer<T>.Default;

			naturalmergesort();
		}

		// FEHLENDE METHODE 1: Die Haupt-Merge-Sort-Logik
		private void naturalmergesort()
		{
			// Abwechselnd von a nach b und von b nach a verschmelzen
			// Die Schleife stoppt, wenn mergeruns() true zurückgibt (Liste ist sortiert)
			while (!mergeruns(ref a, ref b) & !mergeruns(ref b, ref a)) { }
		}

		// FEHLENDE METHODE 2: Verschiebt Runs (Teillisten) und führt das Merge durch
		private bool mergeruns(ref T[] a, ref T[] b)
		{
			int i = 0, k = 0;
			bool asc = true;
			T x;

			while (i < n)
			{
				k = i;                
				do
					x = a [i++];
				while (i < n && comp.Compare (x, a [i]) <= 0);  // Aufsteigender Teil
				
				// Dies behandelt den absteigenden Teil (Merge Sort ist 'natural', da es bereits sortierte Runs nutzt)
				while (i < n && comp.Compare(x, a[i]) >= 0) 
					x = a[i++]; 
					
				merge(ref a, ref b, k, i - 1, asc);
				asc = !asc; // Wechsel der Richtung für die nächste Zusammenführung
			}
			return k == 0; // k==0 bedeutet, dass es nur einen Run gab (Liste ist sortiert)
		}

		// FEHLENDE METHODE 3: Führt zwei Runs zusammen
		private void merge(ref T[] a, ref T[] b, int lo, int hi, bool asc)
		{
			int k = asc ? lo : hi;
			int c = asc ? 1 : -1; // Richtung, in die im Hilfsarray geschrieben wird
			int i = lo, j = hi;

			// Verschiebt das nächstgrößte/nächstkleinste Element in das Hilfsarray b
			while (i <= j)
			{                
				if (comp.Compare(a[i], a[j]) <= 0)
					b[k] = a[i++];
				else
					b[k] = a[j--];
				k += c;
			}
		}
	}
}
