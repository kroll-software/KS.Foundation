using System;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Linq;

namespace KS.Foundation
{
	/// <summary>
	/// A classic linked list.
	/// </summary>
	public class ClassicLinkedList<T> : IEnumerable<T>
	{			
		public object SyncObject { get; private set; }
		public IComparer<T> Comparer { get; protected set ; }			

		[OnDeserializing]
		protected virtual void OnDeserializing()
		{
			SyncObject = new object ();
		}

		[OnDeserialized]
		protected virtual void OnDeserialized()
		{
		}

		public sealed class Node : IEnumerable<T>
		{						
			internal Node m_Next;
			public Node Next
			{
				get {					
					return m_Next;
				}
				set {					
					System.Diagnostics.Debug.Assert (value != this);
					//Concurrency.LockFreeUpdate (ref m_Next, value);
					m_Next = value; // Direct, atomic reference write
				}
			}

			public T Value { get; set; }

			public Node() {}
			public Node(T value)
			{
				Value = value;
				m_Next = null;
			}

			public Node(T value, Node next)
			{
				Value = value;
				m_Next = next;
			}				
			
			public Node Clone()
			{
				return new Node (Value, Next);
			}				

			public override string ToString ()
			{
				return string.Format ("[Node: {0}]", Value);
			}				

			/*** ***/
			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public IEnumerator<T> GetEnumerator()
			{
				var node = this;
				while(node != null)
				{
					yield return node.Value;
					node = node.Next;
				}
			}
		}

		protected int m_Count;
		public int Count 
		{ 
			get => m_Count; 
			protected set => m_Count = value; 
		}

		private Node m_Head;
		public Node Head 
		{ 
			get {
				return m_Head;
			}
			set {
				Concurrency.LockFreeUpdate (ref m_Head, value);
			}
		}

		private Node m_Current;
		public Node Current 
		{ 
			get {
				return m_Current;
			}
			set {
				Concurrency.LockFreeUpdate (ref m_Current, value);
			}
		}

		public ClassicLinkedList() 
		{
			SyncObject = new object ();
		}

		public ClassicLinkedList (IComparer<T> comparer)
			: this()
		{						
			Comparer = comparer;
		}

		public ClassicLinkedList (IEnumerable<T> source) : this (source, null) {}
		public ClassicLinkedList (Node node) : this (node, null) {}

		public ClassicLinkedList (IEnumerable<T> source, IComparer<T> comparer) 
			: this (comparer) 
		{
			if (source != null) {
				foreach (T value in source)
					AddLast (value);
			}

			if (comparer != null)
				Sort ();			
		}

		public ClassicLinkedList (Node node, IComparer<T> comparer) 
			: this (comparer) 
		{
			if (node != null) 
			{				
				m_Head = node;
				Current = Head;			
				while (Current.Next != null) 
				{
					Current = Current.Next;
					Count++;
				}
			}
		}

		public virtual void OnInsert(T value)
		{
		}

		public virtual void OnRemove(T value)
		{
		}

		// *** Retreiving Items ***

		public T First
		{
			get
			{
				if (Head == null)
					return default(T);
				return Head.Value;
			}
		}

		public T Last
		{
			get
			{
				if (Current == null && Head != null)
					Current = Head;

				if (Current == null) 
					return default(T);

				return Current.Value;
			}
		}			

		public Node Retrieve(int position)
		{
			Node tempNode = Head;
			Node retNode = null;
			int count = 0; 
			while (tempNode != null)
			{
				if (count == position)
				{
					retNode = tempNode;
					break;
				}
				count++;
				tempNode = tempNode.Next;
			} 
			return retNode;
		}

		// Zero-Based
		public T this[int index]
		{
			get
			{
				Node node = Retrieve (index);
				if (node == null)
					return default(T);
				return node.Value;
			}
			set
			{
				Node node = Retrieve (index);
				if (node != null)					
					node.Value = value;
			}
		}

		public Node Search(T value)
		{
			Node curr = Head;
			while (curr != null) 
			{
				if (curr.Value.Equals (value))
					return curr;
				curr = curr.Next;
			}
			return null;
		}

		/// <summary>
		/// Find previous node in sorted items
		/// returns null for AddFirst, or Current for Addlast
		/// </summary>
		/// <returns>The previous node</returns>
		/// <param name="value">Value.</param>
		public Node FindPrev(T value)
		{           
			if (Head == null)
				return null;

			CheckComparer();
			CheckCurrent();

			// Lokalen Schnappschuss von Current ziehen!
			Node currentSnapshot = Current;

			// Fast return: Nur vergleichen, wenn currentSnapshot NICHT null ist
			if (currentSnapshot != null && Comparer.Compare(value, currentSnapshot.Value) >= 0)
				return currentSnapshot;

			Node p = Head;
			Node prev = null;

			while (p != null)
			{
				// Wert vor dem Vergleich sichern
				T pVal = p.Value;
				if (Comparer.Compare(pVal, value) > 0)
				{
					break;
				}

				prev = p;
				p = p.Next; // Wandern zum nächsten Knoten
			}

			return prev;
		}

		/// <summary>
		/// Find the specified value in a sorted list
		/// </summary>
		/// <param name="value">Value.</param>
		public Node Find(T value)
		{			
			if (Head == null)
				return null;
			CheckComparer ();
			Node p = Head;
			while (Comparer.Compare (p.Value, value) < 0) {				
				p = p.Next;
				if (p == null)
					return null;
			}
			if (Comparer.Compare (p.Value, value) == 0)
				return p;
			return p;
		}

		// *** Adding Items ***

		public void AddFirst(T value)
		{
			lock (SyncObject)
			{
				// Einfache, direkte Zeiger-Zuweisung – das Lock garantiert die Exklusivität unter Writern
				Head = new Node(value, Head);

				if (Current == null)
					Current = Head;

				Interlocked.Increment(ref m_Count);
				
				// Exakt in der Reihenfolge des Einfügens feuern
				OnInsert(value);
			}
		}

		public void AddLast(T value)
		{
			lock (SyncObject)
			{
				Node newNode = new Node(value);

				if (Head == null)
				{
					Head = newNode;
					Current = newNode;
					m_Count = 1;
				}
				else
				{
					// Wenn Current nicht am Ende steht, einmal schnell ans Ende springen
					if (Current == null || Current.Next != null)
					{
						CheckCurrent();
					}

					// O(1) Anfügen direkt über Current/Tail!
					Current.Next = newNode;
					Current = newNode;
					Interlocked.Increment(ref m_Count);
				}

				OnInsert(value);
			}
		}

		public void InsertAt(int index, T value)
		{
			lock (SyncObject)
			{
				if (Head == null || index <= 0)
				{
					AddFirst(value);
					return;
				}

				if (index >= Count)
				{
					AddLast(value);
					return;
				}

				// Ziel-Vorgänger (index - 1) ansteuern
				Node prev = Head;
				for (int i = 0; i < index - 1; i++)
				{
					if (prev.Next == null)
						break;
					prev = prev.Next;
				}

				// Erst den Nachfolger im neuen Knoten verknüpfen, dann den Vorgänger umhängen
				Node next = prev.Next;
				Node curr = new Node(value, next); 
				prev.Next = curr;

				if (prev == Current)
					Current = curr;

				Interlocked.Increment(ref m_Count);
				OnInsert(value);
			}
		}

		public bool InsertItem(T prevValue, T value)
		{
			lock (SyncObject)
			{
				Node prev = Search(prevValue);
				if (prev == null)
					return false;

				Node next = prev.Next;
				Node curr = new Node(value, next); // Vollständig initialisiert
				prev.Next = curr;

				if (prev == Current)
					Current = curr;

				Interlocked.Increment(ref m_Count);
				OnInsert(value);
				return true;
			}
		}

		public void InsertBefore(Node curr, T value)
		{
			lock (SyncObject)
			{
				if (curr == null)
				{
					AddLast(value);
				}
				else if (curr == Head)
				{
					AddFirst(value);
				}
				else
				{
					Node prev = Head;
					while (prev.Next != null && prev.Next != curr)
						prev = prev.Next;

					Node next = prev.Next;
					Node newNode = new Node(value, next); // Vollständig initialisiert
					prev.Next = newNode;

					if (prev == Current)
						Current = newNode;

					Interlocked.Increment(ref m_Count);
					OnInsert(value);
				}
			}
		}

		public void InsertAfter(Node prev, T value)
		{
			lock (SyncObject)
			{
				if (prev == null)
				{
					AddLast(value);
					return;
				}

				Node next = prev.Next;
				Node curr = new Node(value, next); // Vollständig initialisiert
				prev.Next = curr;

				if (prev == Current)
					Current = curr;

				Interlocked.Increment(ref m_Count);
				OnInsert(value);
			}
		}



		/// <summary>
		/// Adds an item sorted.
		/// WARNING: SLOW with more than 30.000 items
		/// </summary>
		/// <returns><c>true</c>, if sorted was added, <c>false</c> otherwise.</returns>
		/// <param name="value">Value.</param>
		/// <param name="allowDupes">If set to <c>true</c> allow dupes.</param>
		public bool AddSorted(T value, bool allowDupes)
		{   
			lock (SyncObject)
    		{
				if (value == null) {
					return false;
				}
				if (Head == null) {
					AddLast (value);
					return true;
				} 

				CheckComparer ();

				Node prev = FindPrev (value);
				if (prev == null) {
					AddFirst (value);
					return true;
				}
				if (prev.Next == null) {
					AddLast (value);
					return true;
				} 
				if (Comparer.Compare (prev.Next.Value, value) == 0) {
					if (allowDupes) {
						while (prev.Next != null && Comparer.Compare (prev.Next.Value, value) == 0)
							prev = prev.Next;
						InsertAfter (prev, value);
						return true;
					}
					return false;
				}
					
				InsertAfter (prev, value);
				return true;
			}
		}
			
		public void AddOrUpdate(T value)
		{	
			lock (SyncObject)
    		{
				Node node = FindPrev (value);
				if (node == null) {
					AddFirst (value);
				} else if (Comparer.Compare (node.Value, value) == 0) {
					node.Value = value;
				} else {
					InsertAfter (node, value);
				}
			}
		}			

		private void CheckComparer()
		{
			if (Comparer == null)
				Comparer = Comparer<T>.Default;
		}


		/* Function to swap Nodes containing values x and y in linked list by changing links */
		public void SwapValues(T x, T y)
		{
			CheckComparer();

			// Nothing to do if x and y are same
			if (x == null || y == null || Comparer.Compare(x, y) == 0)
				return;

			lock (SyncObject)
			{
				if (Head == null)
					return;

				// Search for x (keep track of prevX and currX)
				Node prevX = null, currX = Head;
				while (currX != null && Comparer.Compare(currX.Value, x) != 0)
				{
					prevX = currX;
					currX = currX.Next;
				}

				// Search for y (keep track of prevY and currY)
				Node prevY = null, currY = Head;
				while (currY != null && Comparer.Compare(currY.Value, y) != 0)
				{
					prevY = currY;
					currY = currY.Next;
				}

				// If either x or y is not present, nothing to do
				if (currX == null || currY == null)
					return;

				// Special Case: x and y are the exact same node instance
				if (currX == currY)
					return;

				// Spezialfall: Direkt benachbarte Knoten!
				// (Wichtig, damit prevY.Next = currX keine Selbst-Referenz erzeugt)
				if (currX.Next == currY)
				{
					// currX steht DIREKT VOR currY
					currX.Next = currY.Next;
					currY.Next = currX;

					if (prevX != null)
						prevX.Next = currY;
					else
						Head = currY;
				}
				else if (currY.Next == currX)
				{
					// currY steht DIREKT VOR currX
					currY.Next = currX.Next;
					currX.Next = currY;

					if (prevY != null)
						prevY.Next = currX;
					else
						Head = currX;
				}
				else
				{
					// Standardfall: Knoten liegen weiter auseinander
					Node tempNext = currX.Next;
					currX.Next = currY.Next;
					currY.Next = tempNext;

					if (prevX != null)
						prevX.Next = currY;
					else
						Head = currY;

					if (prevY != null)
						prevY.Next = currX;
					else
						Head = currX;
				}
			}
		}

		// *** Removing Items ***

		public bool Remove(T value)
		{
			lock (SyncObject)
    		{
				if (value == null || Head == null)
					return false;

				// Spezialfall: Head enthält den gesuchten Wert
				if (Head.Value != null && Head.Value.Equals(value))
				{
					RemoveFirst();
					return true;
				}

				Node curr = Head;

				// Suche den Vorgänger des zu löschenden Knotens
				while (curr.Next != null && (curr.Next.Value == null || !curr.Next.Value.Equals(value)))
				{
					curr = curr.Next;
				}

				// Knoten gefunden
				if (curr.Next != null)
				{
					Node prev = curr;
					Node target = curr.Next;

					// Atomarer Switch: Vorgänger überspringt den Zielknoten
					prev.Next = target.Next;

					// Falls der gelöschte Knoten das Ende der Liste war (oder Current)
					if (target == Current || prev.Next == null)
						Current = prev;

					Interlocked.Decrement(ref m_Count);
					OnRemove(value);
					return true;
				}

				return false;
			}
		}

		public T RemoveLast()
		{
			lock (SyncObject)
			{
				if (Head == null)
					return default;

				// Fall 1: Nur ein einziges Element in der Liste -> O(1)
				if (Head.Next == null)
				{
					T val = Head.Value;
					Head = null;
					Current = null;
					Interlocked.Exchange(ref m_Count, 0);
					OnRemove(val);
					return val;
				}

				// Fall 2: Mehrere Elemente -> Vorletzten Knoten finden
				Node prev = Head;
				while (prev.Next != null && prev.Next.Next != null)
				{
					prev = prev.Next;
				}

				// 'prev.Next' ist der zu löschende letzte Knoten
				Node lastNode = prev.Next;
				T lastValue = lastNode.Value;

				// Letzten Knoten abkoppeln
				prev.Next = null;
				Current = prev;

				Interlocked.Decrement(ref m_Count);
				OnRemove(lastValue);

				return lastValue;
			}
		}

		// *** Thread-Safe Removing Items ***

		public void Remove(Node node)
		{
			lock (SyncObject)
    		{
				if (node == null || Head == null)
					return;

				if (node == Head)
				{
					RemoveFirst();
					return;
				}

				Node curr = Head;
				Node prev = curr;

				while (curr != null)
				{
					if (curr == node)
					{
						// Atomarer Switch: Vorgänger zeigt sofort auf den Nachfolger
						prev.Next = curr.Next;

						// Falls der gelöschte Knoten das Listenende war, Current anpassen
						if (curr == Current || prev.Next == null)
							Current = prev;

						Interlocked.Decrement(ref m_Count);
						OnRemove(node.Value);
						return;
					}

					prev = curr;
					curr = curr.Next;
				}
			}
		}

		public T RemoveAt(int index)
		{
			lock (SyncObject)
    		{
				if (Head == null || index < 0)
					return default(T);

				if (index == 0)
				{
					return RemoveFirst();
				}

				Node curr = Head;
				Node prev = curr;
				int count = 0;

				while (curr != null)
				{
					if (count == index)
					{
						T val = curr.Value;

						// Atomarer Switch: Vorgänger überspringt curr
						prev.Next = curr.Next;

						// Falls der gelöschte Knoten am Ende stand, Current auf prev zurücksetzen
						if (curr == Current || prev.Next == null)
							Current = prev;

						Interlocked.Decrement(ref m_Count);
						OnRemove(val);
						return val;
					}

					count++;
					prev = curr;
					curr = curr.Next;
				}

				return default(T);
			}
		}

		public T RemoveFirst()
		{
			lock (SyncObject)
    		{
				if (Head == null)
					return default(T);

				Node oldHead = Head;
				T val = oldHead.Value;

				if (oldHead.Next == null)
				{
					Clear();
				}
				else
				{
					// Atomar den Head auf das nächste Element verschieben
					Head = oldHead.Next;
					Interlocked.Decrement(ref m_Count);
				}

				OnRemove(val);
				return val;
			}
		}

		public void RemoveSortedRange(T low, T high) 
		{
			lock (SyncObject)
			{
				if (Head == null)
					return;

				CheckComparer();

				// 1. Erst den Head bereinigen, falls er im Bereich liegt
				while (Head != null && Comparer.Compare(Head.Value, low) >= 0 && Comparer.Compare(Head.Value, high) <= 0)
				{
					Head = Head.Next;
					Interlocked.Decrement(ref m_Count);
					OnRemove(Head != null ? Head.Value : default);
				}

				if (Head == null)
				{
					Current = null;
					return;
				}

				// 2. Restliche Knoten filtern
				Node previousNode = Head;
				Node deleteNode = Head.Next;

				while (deleteNode != null) 
				{
					if (Comparer.Compare(deleteNode.Value, low) >= 0 && Comparer.Compare(deleteNode.Value, high) <= 0) 
					{
						previousNode.Next = deleteNode.Next;
						Interlocked.Decrement(ref m_Count);
						OnRemove(deleteNode.Value);
					} 
					else 
					{
						previousNode = deleteNode;
					}
					deleteNode = deleteNode.Next;
				}

				CheckCurrent(true);
			}
		}

		public void AppendRange(IEnumerable<T> other)
		{
			if (other == null)
				return;

			lock (SyncObject)
			{
				foreach (var t in other)
					AddLast(t);            
			}
		}

		public int RemoveRange(int start, int len) 
		{
			if (len <= 0)
				return 0;

			lock (SyncObject)
			{
				if (Head == null || start >= Count)
					return 0;

				// Fall 1: Löschen beginnt direkt beim Head (start == 0)
				if (start == 0)
				{
					int removed = 0;
					while (Head != null && removed < len)
					{
						T removedVal = Head.Value;
						Head = Head.Next;
						Interlocked.Decrement(ref m_Count);
						OnRemove(removedVal);
						removed++;
					}
					CheckCurrent(true);
					return removed;
				}

				// Fall 2: Löschen beginnt ab der Mitte/später
				Node prev = Head;
				for (int idx = 0; idx < start - 1 && prev.Next != null; idx++)
				{
					prev = prev.Next;
				}

				Node curr = prev.Next;
				int countRemoved = 0;

				while (curr != null && countRemoved < len)
				{
					T removedVal = curr.Value;
					curr = curr.Next;
					countRemoved++;
					Interlocked.Decrement(ref m_Count);
					OnRemove(removedVal);
				}

				// Ausgeklinkten Teil überspringen
				prev.Next = curr;

				CheckCurrent(true);
				return countRemoved;
			}
		}
			
		public void Clear()
		{
			lock (SyncObject)
			{
				Head = null;				
				Current = null;
				Interlocked.Exchange(ref m_Count, 0);
			}
		}

		public void ClearAndDisposeItems()
		{
			Node curr = Head;
			Clear ();
			while (curr != null) 
			{
				IDisposable del = curr.Value as IDisposable;
				curr = curr.Next;
				if (del != null)
					del.Dispose ();				
			}			
		}

		public void ResetCurrent()
		{
			lock (SyncObject)
			{
				Node curr = Head;
				int count = 0;

				if (curr != null)
				{
					count = 1;
					while (curr.Next != null)
					{
						curr = curr.Next;
						count++;
					}
				}

				Current = curr;
				Interlocked.Exchange(ref m_Count, count);
			}
		}

		public void CheckCurrent(bool reset = false)
		{
			// 1. Lokalen Schnappschuss von Head sichern
			Node head = Head;
			if (head is null) 
			{
				Current = null;
				return;
			}

			// 2. Lokalen Schnappschuss von Current sichern
			Node curr = reset ? head : Current;
			if (curr is null)
			{
				curr = head;
			}

			// 3. Lokale Schleife über den lokalisierten Zeiger
			while (curr is not null)
			{
				Node nextNode = curr.Next;
				if (nextNode is null)
				{
					// Am Ende der Kette angekommen
					Current = curr;
					return;
				}

				curr = nextNode;
			}

			Current = null;
		}


		// *** Enumerate Items ***

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<T> GetEnumerator()
		{			
			var node = Head;
			while(node != null)
			{
				//i++;
				yield return node.Value;
				node = node.Next;
			}
		}

		// *** Sorting ***
		public bool IsSorted
		{
			get
			{
				return Comparer != null;
			}
		}			

		public void Sort(IComparer<T> comparer)
		{
			lock (SyncObject)
			{
				if (comparer != Comparer) 
				{
					Comparer = comparer;
				}
				Sort();
			}
		}

		public void Sort()
		{
			lock (SyncObject)
			{
				if (Head == null || Head.Next == null)
					return;

				if (Comparer == null)
					Comparer = Comparer<T>.Default;

				// MergeSort baut die neue Kette auf. 
				// Erst wenn ALLES komplett sortiert ist, setzen wir Head in einem atomaren Schritt um!
				Node sortedHead = MergeSort(Head);

				// Atomares Zuweisen an die Instanz
				Head = sortedHead;

				// Current ans Ende der neuen Kette setzen
				CheckCurrent(reset: true);
			}
		}

		// MergeSort Linked List
		//The main function
		private Node MergeSort(Node head) 
		{
			if (head == null || head.Next == null) 
				return head;

			Node middle = GetMiddle(head);      // Mitte finden
			Node sHalf = middle.Next; 
			middle.Next = null;                 // In zwei Teillisten spalten

			// Rekursiv sortieren und zusammenfügen
			return Merge(MergeSort(head), MergeSort(sHalf));  
		}

		//Merge subroutine to merge two sorted lists
		private Node Merge(Node a, Node b) 
		{
			// Dummy-Pointer ohne Neuinstanziierung von 'Node'
			Node dummyHead = new Node(default); 
			Node curr = dummyHead;
			IComparer<T> comparer = Comparer;

			while (a != null && b != null) 
			{               
				if (comparer.Compare(a.Value, b.Value) <= 0)
				{ 
					curr.Next = a; 
					a = a.Next; 
				}
				else 
				{ 
					curr.Next = b; 
					b = b.Next; 
				}
				curr = curr.Next;
			}

			curr.Next = a ?? b;
			return dummyHead.Next;
		}

		//Finding the middle element of the list for splitting
		private Node GetMiddle(Node head)
		{
			if (head == null) 
				return null;

			Node slow = head;
			Node fast = head;

			while (fast.Next != null && fast.Next.Next != null) 
			{
				slow = slow.Next; 
				fast = fast.Next.Next;
			}
			return slow;
		}

		/// <summary>
		/// Interne Hilfsmethode, um den aktuellen Sortierungszustand thread-sicher zu prüfen.
		/// </summary>
		public bool TrueIsSortedCheck()
		{
			Node curr = Head;
			if (curr == null) 
				return true;

			IComparer<T> comp = Comparer ?? Comparer<T>.Default;
			T prevVal = curr.Value;

			while (curr != null)
			{
				Node nextNode = curr.Next;
				if (nextNode == null)
					break;

				T currentVal = nextNode.Value;

				if (comp.Compare(prevVal, currentVal) > 0)
				{
					return false;
				}

				prevVal = currentVal;
				curr = nextNode;
			}

			return true;
		}
	}

	public static class LinkedListExtensions
	{       
		/// <summary>
		/// Gibt alle Werte der Liste auf der Konsole aus (Thread-sicher für Reader).
		/// </summary>
		public static void DumpNodes<T>(this ClassicLinkedList<T> list)
		{
			if (list == null) 
			{
				Console.WriteLine("ClassicLinkedList<T> DumpNodes: list is null.");
				return;
			}

			var curr = list.Head; 
			while (curr != null)
			{
				Console.WriteLine(curr.Value);
				curr = curr.Next;
			}
		}

		/// <summary>
		/// Führt eine Aktion für jedes Element der Liste aus (Lock-Free Reader Loop).
		/// </summary>
		[DebuggerStepThrough]
		public static void ForEach<T>(this ClassicLinkedList<T> list, Action<T> action)
		{
			if (list == null || action == null)
				return;

			var curr = list.Head;
			while (curr != null) 
			{
				// Lokalen Knoten-Referenz sichern
				var nextNode = curr.Next;
				action(curr.Value);
				curr = nextNode;
			}
		}

		/// <summary>
		/// Springt ausgehend von 'node' um 'count' Schritte nach vorne.
		/// </summary>
		public static ClassicLinkedList<T>.Node Skip<T>(this ClassicLinkedList<T>.Node node, int count)
		{
			var nx = node;
			int i = 0;
			while (i < count && nx != null)
			{
				nx = nx.Next;
				i++;
			}
			return nx;
		}

		/// <summary>
		/// Liefert den Knoten an einer bestimmten Index-Position.
		/// </summary>
		public static ClassicLinkedList<T>.Node Seek<T>(this ClassicLinkedList<T> list, int index)
		{
			if (list == null || index < 0 || index >= list.Count)
				return null;

			var head = list.Head;
			if (head == null)
				return null;

			return head.Skip(index);
		}

		/// <summary>
		/// Holt den Nachfolger eines Knotens mit Null-Absicherung.
		/// </summary>
		public static ClassicLinkedList<T>.Node GetNext<T>(this ClassicLinkedList<T>.Node node)
		{
			return node?.Next;
		}

		/// <summary>
		/// Setzt den Nachfolger eines Knotens mit Null-Absicherung.
		/// </summary>
		public static void SetNext<T>(this ClassicLinkedList<T>.Node node, ClassicLinkedList<T>.Node target)
		{
			if (node != null)
				node.Next = target;
		}

		/// <summary>
		/// Liest den Wert eines Knotens mit Null-Absicherung.
		/// </summary>
		public static T GetValue<T>(this ClassicLinkedList<T>.Node node)
		{
			if (node == null)
				return default;
			return node.Value;
		}
	}
}

