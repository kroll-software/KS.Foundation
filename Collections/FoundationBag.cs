using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace KS.Foundation
{
	public class FoundationBag<T> : IEnumerable<T> where T : class
	{		
		private T[] Items;
		public int Count { get; private set; }

		public readonly object SyncObject = new object ();

		public FoundationBag () : this(0) {}
		public FoundationBag(int capacity)
		{
			Items = new T[Math.Max(capacity, 31)];
		}

		public T Remove(int index)
		{
			lock (SyncObject) {
				T item = Items [index];
				Items [index] = Items [--Count];
				Items [Count] = null;
				return item;
			}
		}

		public T RemoveLast()
		{
			lock (SyncObject) {
				if (Count > 0) {
					T item = Items [--Count];
					Items [Count] = null;
					return item;
				}
				return default(T);
			}
		}

		public bool Remove(T item)
		{	
			if (item == null)
				return false;
			lock (SyncObject) {
				for (int i = 0, count = Count; i < count; i++) {
					Object o1 = Items [i];
					if (item.Equals (o1)) {
						Items [i] = Items [--Count];
						Items [Count] = null;
						return true;
					}
				}
				return false;
			}
		}
			
		public bool Contains(T item)
		{
			if (item == null)
				return false;

			lock (SyncObject) {
				for (int i = 0, count = Count; count > i; i++) {
					if (item.Equals (Items [i])) {
						return true;
					}
				}
				return false;
			}
		}
			
		public bool RemoveAll(FoundationBag<T> bag)
		{
			if (bag == null || bag.Count == 0)
				return false;

			lock (SyncObject) {
				bool modified = false;
				for (int i = 0, count = bag.Count; i < count; i++) {
					Object o1 = bag.Get (i);
					for (int j = 0; j < Count; j++) {
						Object o2 = Items [j];
						if (o1 == o2) {
							Remove (j);
							j--;
							modified = true;
							break;
						}
					}
				}
				return modified;
			}
		}

		public bool RemoveAll(IEnumerable<T> items)
		{
			lock (SyncObject) {
				int count = Count;
				items.ForEach (item => Remove (item));
				return Count < count;
			}
		}
			
		public T Get(int index)
		{
			try
			{				
				lock (SyncObject) {			
					if (index < 0 || index >= Count)
						return default(T);
					return Items[index];
				}
			}
			catch (Exception ex)
			{
				ex.LogError ();
				return null;
			}
		}
			
		public int Capacity
		{
			get{
				return Items.Length;
			}
		}
			
		public bool IsEmpty()
		{
			lock (SyncObject) {
				return Count == 0;
			}
		}

		public void Add(T item)
		{			
			lock (SyncObject) {
				if (item == null)
					return;
				if (Count >= Items.Length - 1)
					Grow ();
				Items [Count++] = item;
			}
		}
			
		public void Set(int index, T item)
		{
			lock (SyncObject) {
				if (index >= Items.Length - 1)
					Grow ((index * 3) / 2 + 1);
				if (index >= Count)
					Count = index + 1;
				Items [index] = item;
			}
		}

		public T this[int index]
		{
			get
			{
				return Get(index);
			}
			set
			{
				Set(index, value);
			}
		}

		private void Grow()
		{
			int newCapacity = (Items.Length * 3) / 2 + 1;
			Grow(newCapacity);
		}

		private void Grow(int newCapacity)
		{			
			T[] oldItems = Items;
			T[] items = new T[Math.Max(newCapacity, 31)];
			Array.Copy(oldItems, 0, items, 0, oldItems.Length);
			Concurrency.LockFreeUpdate (ref Items, items);
		}

		public void Clear()
		{
			Count = 0;
			T[] items = new T[31];
			Concurrency.LockFreeUpdate (ref Items, items);

			//for (int i = 0, count = Count; i < count; i++)
			//	Items [i] = null;
			//Count = 0;
		}

		public void AddAll(FoundationBag<T> bag)
		{			
			lock (SyncObject) {
				for (int i = 0, count = bag.Count; i < count; i++) {
					Add (bag.Get (i));
				}
			}
		}

		public void AddAll(IEnumerable<T> items)
		{	
			lock (SyncObject) {
				items.ForEach (Add);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0, count = Count; i < count; i++)
			{
				T item = Get(i);	// be robust
				if (item != null)
					yield return item;
			}
		}
	}
}

