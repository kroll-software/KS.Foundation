using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Pfz.Collections;

namespace KS.Foundation
{
	// Monohierarchy, in other words: A Tree.

	public interface IMonohierarchy<T, TKey>
	{
		TKey Key { get; }
		IMonohierarchy<T, TKey> Parent { get; }
		IMonohierarchy<T, TKey> Root { get; }

		TChild AddChild<TChild> (TChild child) where TChild : IMonohierarchy<T, TKey>;
		bool RemoveChild (IMonohierarchy<T, TKey> child);
		bool RemoveChild (TKey key);

		bool HasChildren { get; }
		int ChildCount { get; }
		bool ContainsChild (IMonohierarchy<T, TKey> child);
		bool ContainsChild (TKey key);

		bool GetChild (TKey key, out IMonohierarchy<T, TKey> child);
		IMonohierarchy<T, TKey> this [TKey key] { get; }

		IEnumerable<IMonohierarchy<T, TKey>> Children { get; }
		IEnumerable<TKey> ChildrenKeys { get; }
	}

	public abstract class Monohierarchy<T, TKey> : DisposableObject, IMonohierarchy<T, TKey>, IEnumerable<IMonohierarchy<T, TKey>>
	{
		public virtual IMonohierarchy<T, TKey> Parent { get; private set; }
		protected readonly ThreadSafeDictionary<TKey, IMonohierarchy<T, TKey>> DictChildren;

		protected Monohierarchy (IMonohierarchy<T, TKey> parent)
		{
			Parent = parent;
			DictChildren = new ThreadSafeDictionary<TKey, IMonohierarchy<T, TKey>> ();
		}

		public abstract TKey Key { get; }

		public virtual IMonohierarchy<T, TKey> Root
		{
			get{
				IMonohierarchy<T, TKey> p = this;
				while (p.Parent != null)
					p = p.Parent;
				return p;
			}
		}
			
		protected virtual void OnChildAdded(IMonohierarchy<T, TKey> child)
		{
			
		}
			
		protected virtual void OnChildRemoved(IMonohierarchy<T, TKey> child)
		{

		}

		public bool HasChildren
		{
			get{
				return DictChildren.Count > 0;
			}
		}

		public int ChildCount 
		{ 
			get {
				return DictChildren.Count;
			}
		}

		public virtual bool ContainsChild(IMonohierarchy<T, TKey> child)
		{
			return DictChildren.ContainsKey (child.Key);
		}

		public bool ContainsChild(TKey key)
		{
			return DictChildren.ContainsKey (key);
		}

		//public IMonohierarchy<T, TKey> AddChild(IMonohierarchy<T, TKey> child)
		public TChild AddChild<TChild>(TChild child) where TChild : IMonohierarchy<T, TKey>
		{
			if (child == null)
				throw new ArgumentNullException ("child");
			if (child.Parent != this)
				throw new ArgumentException ("child.Parent");

			if (!DictChildren.TryAdd (child.Key, child))
				return default(TChild);
			OnChildAdded (child);
			return child;
		}

		public bool RemoveChild(IMonohierarchy<T, TKey> child)
		{		
			if (child == null)
				throw new ArgumentNullException ("child");			
			if (!DictChildren.Remove (child.Key))
				return false;
			OnChildRemoved (child);
			return true;
		}

		public bool RemoveChild(TKey key)
		{			
			IMonohierarchy<T, TKey> child;
			if (DictChildren.TryGetValue (key, out child)) {
				if (!DictChildren.Remove (key))
					return false;
				OnChildRemoved (child);	
				return true;
			}
			return false;
		}
			
		public bool GetChild(TKey key, out IMonohierarchy<T, TKey> child)
		{
			if (DictChildren.TryGetValue (key, out child))
				return true;
			foreach (IMonohierarchy<T, TKey> c in DictChildren.Values) {
				if (c.GetChild (key, out child))
					return true;
			}
			child = null;
			return false;
		}

		public IMonohierarchy<T, TKey> this [TKey key]
		{
			get {
				IMonohierarchy<T, TKey> child;
				if (GetChild(key, out child))
					return child;
				return default(IMonohierarchy<T, TKey>);
			}
		}

		public IEnumerable<IMonohierarchy<T, TKey>> Children
		{
			get{
				return DictChildren.Values;
			}
		}

		public IEnumerable<TKey> ChildrenKeys
		{
			get{
				return DictChildren.Keys;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public IEnumerator<IMonohierarchy<T, TKey>> GetEnumerator()
		{
			return EnumerateItems(this).GetEnumerator();
		}
			
		IEnumerable<IMonohierarchy<T, TKey>> EnumerateItems(IMonohierarchy<T, TKey> root)
		{	
			var queue = new Queue<IMonohierarchy<T, TKey>>();
			queue.Enqueue(root);
			while(queue.Any())
			{
				var w = queue.Dequeue();
				yield return w;
				w.Children.ForEach (queue.Enqueue);
			}
		}

		protected override void CleanupManagedResources ()
		{
			Parent = null;
			DictChildren.Values.DisposeListObjects ();
			DictChildren.Clear ();
			base.CleanupManagedResources ();
		}
	}
}

