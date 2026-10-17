using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;

namespace KS.Foundation
{
	/// <summary>
	/// Thread-sicherer <see cref="HashSet{T}"/>.
	///
	/// JEDER öffentlichen Methode/Property die die Basisklasse <see cref="HashSet{T}"/>
	/// öffentlich bereitstellt, wird hier eine eigene, mit dem <see cref="RWLock"/>
	/// gesicherte Version gegenübergestellt (new-Member):
	///   - lesende Operationen      → <see cref="ReaderWriterLockSlim.EnterReadLock"/>
	///   - mutierende Operationen   → <see cref="ReaderWriterLockSlim.EnterWriteLock"/>
	///
	/// Zusätzlich werden alle von der Basisklasse implementierten interfaces
	/// in dieser Klasse NEU implementiert (ICollection&lt;T&gt;, IReadOnlyCollection&lt;T&gt;,
	/// ISet&lt;T&gt;, IReadOnlySet&lt;T&gt;, IEnumerable, IEnumerable&lt;T&gt;), sodass auch
	/// Aufrufe über ein Interface — z. B. LINQ (<c>set.Where(...)</c>,
	/// <c>set.ToArray()</c>) oder <c>ISet&lt;T&gt;</c> — niemals den ungesicherten
	/// Code der Basisklasse erreichen können.
	///
	/// Der Enumerator gibt einen konsistenten Snapshot (Kopie) des Sets zurück;
	/// es kann während der laufenden Enumeration sicher aus anderen Threads
	/// gemutet werden.
	///
	/// Hinweise:
	/// - Read-Lock → Write-Lock innerhalb desselben Threads ist nicht möglich
	///   (Semantik von <see cref="ReaderWriterLockSlim"/>); wer beim Aufruf einen
	///   Read-Lock hält, darf keine mutierende Methode auf demselben Set aufrufen.
	/// - Set-Operationen zwischen ZWEI ThreadSafeHashSets (UnionWith, SetEquals …)
	///   halten den Write-Lock eines Sets und lesen das andere; aus verschiedenen
	///   Threads in umgekehrter Reihenfolge aufgerufen, kann das zu einer
	///   AB/BA-Lock-Ordnung — wie bei jeder Lock-basierten Struktur.
	/// - <see cref="GetAlternateLookup{TAlternate}"/> (neu in .NET 10) liefert einen
	///   LIVE-Proxy auf den internen Zustand des Sets. Der Aufruf selbst ist
	///   Read-Lock-gesichert; spätere Aufrufe auf dem Proxy sind es nicht —
	///   das ist der API selbst inhärent.
	/// </summary>
	public class ThreadSafeHashSet<T> : HashSet<T>,
		ICollection<T>, IReadOnlyCollection<T>, ISet<T>, IReadOnlySet<T>, IEnumerable<T>, IEnumerable
	{
		public ReaderWriterLockSlim RWLock { get; private set; }

		[OnDeserializing]
		protected virtual void OnDeserializing()
		{
			// Wird vom Serialisierungs-Mechanismus NACH dem
			// Deserialisierungs-Konstructor aufgerufen. Das Lock ist nicht
			// serialisierbar und muss hier neu angelegt werden.
			RWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		public ThreadSafeHashSet()
			: base()
		{
			RWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		public ThreadSafeHashSet(IEnumerable<T> collection)
			: base(collection)
		{
			RWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		// ================================================================
		// Intern: konsistenter Snapshot in EINEM ReadLock
		// ================================================================
		private T[] Snapshot()
		{
			using (new ReadLock(RWLock))
			{
				int n = base.Count;
				var result = new T[n];
				if (n > 0)
				{
					base.CopyTo(result, 0);
				}
				return result;
			}
		}

		// ================================================================
		// Properties
		// ================================================================
		public new int Count
		{
			get
			{
				using (new ReadLock(RWLock))
				{
					return base.Count;
				}
			}
		}

		public new int Capacity
		{
			get
			{
				using (new ReadLock(RWLock))
				{
					return base.Capacity;
				}
			}
		}

		public new IEqualityComparer<T> Comparer
		{
			get
			{
				using (new ReadLock(RWLock))
				{
					return base.Comparer;
				}
			}
		}

		// ================================================================
		// Mutierende Methoden (WriteLock)
		// ================================================================
		public new bool Add(T item)
		{
			using (new WriteLock(RWLock))
			{
				return base.Add(item);
			}
		}

		public new void Clear()
		{
			using (new WriteLock(RWLock))
			{
				base.Clear();
			}
		}

		public new bool Remove(T item)
		{
			using (new WriteLock(RWLock))
			{
				return base.Remove(item);
			}
		}

		public new int RemoveWhere(Predicate<T> match)
		{
			using (new WriteLock(RWLock))
			{
				return base.RemoveWhere(match);
			}
		}

		public new int EnsureCapacity(int capacity)
		{
			using (new WriteLock(RWLock))
			{
				return base.EnsureCapacity(capacity);
			}
		}

		public new void UnionWith(IEnumerable<T> other)
		{
			using (new WriteLock(RWLock))
			{
				base.UnionWith(other);
			}
		}

		public new void IntersectWith(IEnumerable<T> other)
		{
			using (new WriteLock(RWLock))
			{
				base.IntersectWith(other);
			}
		}

		public new void ExceptWith(IEnumerable<T> other)
		{
			using (new WriteLock(RWLock))
			{
				base.ExceptWith(other);
			}
		}

		public new void SymmetricExceptWith(IEnumerable<T> other)
		{
			using (new WriteLock(RWLock))
			{
				base.SymmetricExceptWith(other);
			}
		}

		public new void TrimExcess()
		{
			using (new WriteLock(RWLock))
			{
				base.TrimExcess();
			}
		}

		public new void TrimExcess(int capacity)
		{
			using (new WriteLock(RWLock))
			{
				base.TrimExcess(capacity);
			}
		}

		// ================================================================
		// Lesende Methoden (ReadLock)
		// ================================================================
		public new bool Contains(T item)
		{
			using (new ReadLock(RWLock))
			{
				return base.Contains(item);
			}
		}

		public new void CopyTo(T[] array)
		{
			using (new ReadLock(RWLock))
			{
				base.CopyTo(array);
			}
		}

		public new void CopyTo(T[] array, int arrayIndex)
		{
			using (new ReadLock(RWLock))
			{
				base.CopyTo(array, arrayIndex);
			}
		}

		public new void CopyTo(T[] array, int arrayIndex, int count)
		{
			using (new ReadLock(RWLock))
			{
				base.CopyTo(array, arrayIndex, count);
			}
		}

		public new bool Overlaps(IEnumerable<T> other)
		{
			using (new ReadLock(RWLock))
			{
				return base.Overlaps(other);
			}
		}

		public new bool SetEquals(IEnumerable<T> other)
		{
			using (new ReadLock(RWLock))
			{
				return base.SetEquals(other);
			}
		}

		public new bool IsSubsetOf(IEnumerable<T> other)
		{
			using (new ReadLock(RWLock))
			{
				return base.IsSubsetOf(other);
			}
		}

		public new bool IsProperSubsetOf(IEnumerable<T> other)
		{
			using (new ReadLock(RWLock))
			{
				return base.IsProperSubsetOf(other);
			}
		}

		public new bool IsSupersetOf(IEnumerable<T> other)
		{
			using (new ReadLock(RWLock))
			{
				return base.IsSupersetOf(other);
			}
		}

		public new bool IsProperSupersetOf(IEnumerable<T> other)
		{
			using (new ReadLock(RWLock))
			{
				return base.IsProperSupersetOf(other);
			}
		}

		public new bool TryGetValue(T equalValue, out T actualValue)
		{
			using (new ReadLock(RWLock))
			{
				return base.TryGetValue(equalValue, out actualValue);
			}
		}

		public new HashSet<T>.AlternateLookup<TAlternate> GetAlternateLookup<TAlternate>()
		{
			using (new ReadLock(RWLock))
			{
				return base.GetAlternateLookup<TAlternate>();
			}
		}

		public new bool TryGetAlternateLookup<TAlternate>(out HashSet<T>.AlternateLookup<TAlternate> lookup)
		{
			using (new ReadLock(RWLock))
			{
				return base.TryGetAlternateLookup<TAlternate>(out lookup);
			}
		}

		// ================================================================
		// Enumeration: Snapshot-Prinzip.
		// Der zurückgegebene Enumerator iteriert eine KOPIE des Sets, daher
		// darf das Set während der Enumeration aus einem anderen Thread
		// gemutet werden.
		// ================================================================
		public new ThreadsafeEnumerator<T> GetEnumerator()
		{
			return new ThreadsafeEnumerator<T>(Snapshot());
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return new ThreadsafeEnumerator<T>(Snapshot());
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new ThreadsafeEnumerator<T>(Snapshot());
		}

		// ================================================================
		// Interface-Mitglieder, die keine passende öffentliche Signatur
		// haben und daher EXPLIZIT gesichert implementiert werden müssen
		// (sonst würde die Basisklassen-Implementierung greifen)
		// ================================================================
		void ICollection<T>.Add(T item)
		{
			using (new WriteLock(RWLock))
			{
				base.Add(item);
			}
		}

		bool ICollection<T>.IsReadOnly => false;
	}
}
