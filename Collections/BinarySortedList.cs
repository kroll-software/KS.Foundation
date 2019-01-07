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
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Threading;

namespace KS.Foundation
{
    //public class BinarySortedList<T> : List<T> where T : IComparable<T>
	public class BinarySortedList<T> : List<T>, IEnumerable<T>
    {				
		public ReaderWriterLockSlim RWLock { get; private set; }

		[OnDeserializing]
		protected virtual void OnDeserializing()
		{
			RWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		[OnDeserialized]
		protected virtual void OnDeserialized()
		{
		}

		public IComparer<T> Comparer { get; protected set; }        

		// Avoid to create huge arrays for each element on creation
		protected const int DefaultCapacity = 31;

		public BinarySortedList() : base (DefaultCapacity) 
		{ 
			RWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		public BinarySortedList(int capacity) : base (capacity) 
		{ 
			Capacity = capacity;
			RWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}        

		public BinarySortedList(IComparer<T> comparer) : this (DefaultCapacity) 
        {
            Comparer = comparer;
        }

        public BinarySortedList(int capacity, IComparer<T> comparer) : this (capacity) 
        {
            Comparer = comparer;
        }

		public BinarySortedList(IEnumerable<T> collection) : this (collection, null) {}
		public BinarySortedList(IEnumerable<T> collection, IComparer<T> comparer) : this ()
        {			
            Comparer = comparer;
			if (collection != null) {
				base.AddRange (collection);					
				//base.Sort (comparer);
				NaturalMergeSort();
			}
        }

		public new int Count
		{
			get{
				RWLock.EnterReadLock();
				try {
					return base.Count;
				}
				finally {
					RWLock.ExitReadLock ();
				}
			}
		}

		public T UnlockedItemByIndex(int index)
		{
			return base[index];
		}

		public new T this[int index]
		{
			get{
				RWLock.EnterReadLock();
				try {
					return base[index];
				}
				finally {
					RWLock.ExitReadLock ();
				}
			}
		}

        public T First
        {
            get
            {
				RWLock.EnterReadLock();

				try
				{
					if (base.Count > 0)
						return base [0];
					else
						return default(T);
				}
				finally
				{				
					RWLock.ExitReadLock();
				}
            }
        }

		public T Last
		{
			get
			{	
				RWLock.EnterReadLock();

				try
				{
					if (base.Count > 0)
						return base [base.Count - 1];
					else
						return default(T);
				}
				finally
				{				
					RWLock.ExitReadLock();
				}
			}
		}

        public void RemoveFirst()
        {
			RWLock.EnterWriteLock ();

			try
			{
				if (base.Count > 0) {
					T item = this[0];
					base.RemoveAt (0);
					OnRemove(item);
				}
			}
			finally
			{				
				RWLock.ExitWriteLock();
			}
        }

        public void RemoveLast()
        {
			RWLock.EnterWriteLock ();

			try
			{
				if (base.Count > 0) {
					T item = this[base.Count - 1];
					base.RemoveAt (base.Count - 1);
					OnRemove(item);
				}
			}
			finally
			{				
				RWLock.ExitWriteLock();
			}
        }

		public new bool Remove(T item)
		{
			RWLock.EnterWriteLock ();

			try
			{				
				if (base.Remove (item)) {
					OnRemove(item);
					return true;
				}
				return false;
			}
			finally
			{				
				RWLock.ExitWriteLock();
			}
		}

		public new void RemoveAt(int index)
		{
			RWLock.EnterWriteLock ();

			try
			{	
				T item = this[index];
				base.RemoveAt (index);
				OnRemove(item);
			}
			finally
			{				
				RWLock.ExitWriteLock();
			}
		}

        /// <summary>
        /// This always returns a value, no bounds checks required
        /// Except when the list is empty, returns default(T)
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public T FindSafe(T search)
        {
			RWLock.EnterReadLock ();

			try
            {
				if (base.Count == 0)
                    return default(T);
                
				EnsureComparer ();

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
			finally {
				RWLock.ExitReadLock ();
			}
        }			

        /// <summary>
        /// This always returns a value, no bounds checks required
        /// Except when the list is empty, returns default(T)
        /// </summary>
		/// <param name="elem"></param>
        /// <returns></returns>
        public T Find(T elem)
        {
			RWLock.EnterReadLock ();

			try
            {
				int i = InternalIndexOf(elem);
				if (i >= 0 && i < base.Count)
					return base[i];

                return default(T);
            }
			finally {
				RWLock.ExitReadLock ();
			}
        }

		public T FindValue(Func<T, int> CompareResult) {			
			RWLock.EnterReadLock ();
			try
			{
				int a = 0;
				int b = base.Count;
				while (true)
				{
					if (a == b)
						return default(T);

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
			finally  {
				RWLock.ExitReadLock ();
			}
		}
        

        public int AddOrUpdate(T elem)
        {
			RWLock.EnterWriteLock ();

			try {
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
			finally {
				RWLock.ExitWriteLock ();
			}
        }


		public virtual void OnInsert(T elem)
		{
		}

		public virtual void OnRemove(T elem)
		{
		}

		public void AddLastUnlocked(T elem)
		{
			base.Add (elem);
			OnInsert (elem);
		}

		public void AddFirst (T elem)
		{
			Insert (0, elem);
		}

        /// <summary>
        /// You can't find or delete after this
        /// </summary>
        /// <param name="elem"></param>
        public void AddLast(T elem)
        {
			RWLock.EnterWriteLock ();
			try {
				base.Add(elem);
			} finally {
				OnInsert (elem);
				RWLock.ExitWriteLock ();
			}
        }

        public new void Add(T elem)
        {			
			RWLock.EnterWriteLock ();
			try {
				int index = InternalIndexOf(elem);
                if (index < 0)                
                    index = ~index;

				if (index >= base.Count)
                    base.Add(elem);
                else
                    base.Insert(index, elem);
			} finally {					
				OnInsert (elem);	
				RWLock.ExitWriteLock ();
			}            
        }			

		public new void AddRange(IEnumerable<T> collection)
		{
			RWLock.EnterWriteLock ();
			try {
				if (collection == null)
					return;

				collection.ForEach (elem => {
					base.Add(elem);
					OnInsert(elem);
				});
				InternalNaturalMergeSort (this.Comparer);
			}
			finally {
				RWLock.ExitWriteLock ();
			}
		}

		public void AddRangeUnsorted(IEnumerable<T> source)
		{
			RWLock.EnterWriteLock ();
			try {
				if (source == null)
					return;

				source.ForEach (elem => {
					base.Add(elem);
					OnInsert(elem);
				});
			}
			finally {
				RWLock.ExitWriteLock ();
			}
		}

		public new void Insert(int index, T elem)
		{
			RWLock.EnterWriteLock();
			try
			{
				if (elem == null)
					return;
				base.Insert(index, elem);
				OnInsert(elem);
			}
			finally
			{
				RWLock.ExitWriteLock();
			}
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
			RWLock.EnterReadLock ();
			try {
				return InternalIndexOfElementOrPredecessor(item);
            }
			finally {
				RWLock.ExitReadLock ();
			}
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
			RWLock.EnterReadLock ();
			try {
				return InternalIndexOfElementOrSuccessor(item);
            }
			finally {
				RWLock.ExitReadLock ();
			}
        }

        // The C# language is missing the most basic operators in math:
        // The predecessor and the successor operator !!
        // 
        // In mathematics, this is the fundamental precondition to count or compare elements.
        // Everything, that has a successor defined, can be counted and compared
        //
        // We would want to return the predecessor or the successor with the following functions,
        // if an element could not be found. Unfortunately we can't do that and have to return default(T), 
        // which is not the same and not what we want..

        public T FindElementOrPredecessor(T item)
        {
			RWLock.EnterReadLock ();
			try {
                int index = InternalIndexOfElementOrPredecessor(item);
                if (index < 0)
                    return default(T);
                else
					return base[index];
            }
			finally {
				RWLock.ExitReadLock ();
			}
        }

        public T FindElementOrSuccessor(T item)
        {
			RWLock.EnterReadLock ();
			try {
                int index = InternalIndexOfElementOrSuccessor(item);
				if (index >= base.Count)
                    return default(T);
                else
					return base[index];
            }
			finally {
				RWLock.ExitReadLock ();
			}
        }        			

        // Microsoft DotNet BinarySearch is times solwer
        //public void AddSorted(T newitem)        
        //{
        //    int binraySearchIndex = this.BinarySearch(newitem, m_Comparer);
        //    if (binraySearchIndex < 0)
        //        base.Insert(~binraySearchIndex, newitem);
        //    else
        //        base.Insert(binraySearchIndex, newitem);
        //}
		        
		private int InternalIndexOf(T item)
		{						
			//if (item == null)
			//	return -1;

			EnsureComparer ();

			// **Alt
			/*** ***/
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


			/*** NEU 
			int a = 0;
			int b = Count - 1;
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
			else if (a >= Count)
				return ~a;

			return b + 1;
			***/

			/*** 
			int idx = b + 1;
			try {
				while (idx < Count && !ReferenceEquals (base [idx], item))
					idx++;

				//if (idx >= Count)
				//	return -1;
			} catch (Exception ex) {
				ex.LogError ();
			}

			return idx;
			***/
		}

		/// <summary>
		/// returns IndexOf or -1
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
        public new int IndexOf(T item)
        {
			RWLock.EnterReadLock ();
			try {
				int idx = InternalIndexOf(item);
				if (idx < 0 || idx > base.Count)
					return -1;
				return idx;
            }
			finally {
				RWLock.ExitReadLock ();
			}
        }

		private void EnsureComparer()
		{
			if (Comparer == null)
				Comparer = Comparer<T>.Default;
		}

		/*** ***/
		private int InternalFirstIndexOf(T item)
		{
			EnsureComparer ();

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
			RWLock.EnterReadLock ();
			try {
				return InternalFirstIndexOf(item);
			}
			finally {
				RWLock.ExitReadLock ();
			}
		}


		public new int LastIndexOf(T item)
		{
			RWLock.EnterReadLock ();
			try {
				EnsureComparer ();

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
			finally {
				RWLock.ExitReadLock ();
			}
		}

		public virtual new void Clear()
		{
			RWLock.EnterWriteLock ();
			try {
				base.Clear();
			}
			finally {
				RWLock.ExitWriteLock ();
			}
		}
			
		public new void Sort(IComparer<T> comparer)
		{	
			base.Sort(comparer);
		}

		public void NaturalMergeSort()
		{
			NaturalMergeSort (this.Comparer);
		}

		public virtual void NaturalMergeSort(IComparer<T> comparer)
        {
			RWLock.EnterWriteLock ();
			try {
				InternalNaturalMergeSort(comparer);
			}
			finally {
				RWLock.ExitWriteLock ();
			}
        }

		private void InternalNaturalMergeSort(IComparer<T> comparer)
		{
			if (comparer == null)
				comparer = Comparer<T>.Default;

			NaturalMergeSorter<T> sorter = new NaturalMergeSorter<T>();
			T[] res = base.ToArray();
			sorter.Sort(ref res, comparer);
			for (int i = 0; i < base.Count; i++)
				base[i] = res[i];
		}

		/*** ***/
		public new T[] ToArray()
		{
			RWLock.EnterReadLock ();
			try {
				return base.ToArray();
			}
			finally {
				RWLock.ExitReadLock ();
			}
		}

		[DebuggerStepThrough]
		public new void ForEach(Action<T> action)
		{
			var enu = GetEnumerator ();
			// Da kann er meckern und Ratschläge geben, wie er will,
			// nur so ist es richtig.
			// default(T) von int ist 0, aber das ist auch ein Wert !
			if (enu == null)
				return;

			while (enu.MoveNext ()) {
				action(enu.Current);
			}				
		}
			
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public new IEnumerator<T> GetEnumerator()
		{								
			return new ThreadsafeEnumerator<T> (ToArray ());
		}
    }

	//public class NaturalMergeSorter<T> where T : IComparable<T>
	public class NaturalMergeSorter<T>
	{
		private T[] a;
		private T[] b;    // Hilfsarray
		private int n;

		IComparer<T> comp = null;

		public void Sort(ref T[] a, IComparer<T> comparer)
		{
			this.a = a;
			n = a.Length;
			b = new T[n];

			comp = comparer;
			if (comp == null)
				comp = Comparer<T>.Default;

			naturalmergesort();
		}

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
				while (i < n && comp.Compare (x, a [i]) <= 0);  // aufsteigender Teil
				while (i < n && comp.Compare(x, a[i]) >= 0) x = a[i++]; // absteigender Teil
				merge(ref a, ref b, k, i - 1, asc);
				asc = !asc;
			}
			return k == 0;
		}

		private void merge(ref T[] a, ref T[] b, int lo, int hi, bool asc)
		{
			int k = asc ? lo : hi;
			int c = asc ? 1 : -1;
			int i = lo, j = hi;

			// jeweils das nächstgrößte Element zurückkopieren,
			// bis i und j sich überkreuzen
			while (i <= j)
			{                
				if (comp.Compare(a[i], a[j]) <= 0)
					b[k] = a[i++];
				else
					b[k] = a[j--];
				k += c;
			}
		}

		private void naturalmergesort()
		{
			// abwechselnd von a nach b und von b nach a verschmelzen
			while (!mergeruns(ref a, ref b) & !mergeruns(ref b, ref a)) {}
		}
	}    // end class NaturalMergeSorter
}
