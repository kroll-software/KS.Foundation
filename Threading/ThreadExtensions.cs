using System;
using System.Threading;

namespace KS.Foundation
{
	public static class ThreadingExtensions
	{
		public static void Write(this ReaderWriterLockSlim rwlock, Action action)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			rwlock.EnterWriteLock();

			try
			{
				action();
			}
			finally
			{
				rwlock.ExitWriteLock();
			}
		}

		public static void Read(this ReaderWriterLockSlim rwlock, Action action)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			rwlock.EnterReadLock();

			try
			{
				action();
			}
			finally
			{
				rwlock.ExitReadLock();
			}
		}

		// Unfortunately< this is too slow
		public static T ReadReturn<T>(this ReaderWriterLockSlim rwlock, Func<T> func)
		{
			if (func == null)
			{
				throw new ArgumentNullException("func");
			}

			rwlock.EnterReadLock();

			try
			{
				return func();
			}
			finally
			{				
				rwlock.ExitReadLock();
			}
		}			

		public static void ReadUpgradable(this ReaderWriterLockSlim rwlock, Action action)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			rwlock.EnterUpgradeableReadLock();

			try
			{
				action();
			}
			finally
			{
				rwlock.ExitUpgradeableReadLock();
			}
		}
	}
}

