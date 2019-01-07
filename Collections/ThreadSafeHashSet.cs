using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Threading;

namespace KS.Foundation
{
	public class ThreadSafeHashSet<T> : HashSet<T>
	{		
		public ReaderWriterLockSlim RWLock { get; private set; }

		[OnDeserializing]
		protected virtual void OnDeserializing()
		{
			RWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		public ThreadSafeHashSet ()
			: base()
		{
			RWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		public ThreadSafeHashSet (IEnumerable<T> collection)
			: base(collection)
		{
			RWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		public new bool Contains (T item)
		{
			RWLock.EnterReadLock ();
			try {
				return base.Contains (item);	
			} finally {
				RWLock.ExitReadLock ();
			}
		}

		public new int Count
		{
			get{
				RWLock.EnterReadLock ();
				try {
					return base.Count;	
				} finally {
					RWLock.ExitReadLock ();
				}
			}
		}

		public new bool Add(T item)
		{			
			RWLock.EnterWriteLock ();
			try {
				return base.Add (item);
			} finally {
				RWLock.ExitWriteLock ();
			}
		}

		public new bool Remove(T item)
		{			
			RWLock.EnterWriteLock ();
			try {
				return base.Remove (item);
			} finally {
				RWLock.ExitWriteLock ();
			}
		}

		public new void Clear()
		{			
			RWLock.EnterWriteLock ();
			try {
				base.Clear();
			} finally {
				RWLock.ExitWriteLock ();
			}
		}

		public new ThreadsafeEnumerator<T> GetEnumerator()
		{
			ThreadsafeEnumerator<T> enu;
			RWLock.EnterReadLock ();
			try {
				enu = new ThreadsafeEnumerator<T>(this.ToArray(), Count);
			} finally {
				RWLock.ExitReadLock ();		
			}
			return enu;
		}
	}		
}

