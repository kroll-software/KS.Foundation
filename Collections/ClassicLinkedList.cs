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
	public class ClassicLinkedList<T> : IEnumerable<T> where T : IComparable<T>
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
			Node m_Next;
			public Node Next
			{
				get {					
					return m_Next;
				}
				set {					
					System.Diagnostics.Debug.Assert (value != this);
					Concurrency.LockFreeUpdate (ref m_Next, value);
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

			~Node()
			{
  				// not sure, but I think, this might help the GC
				m_Next = null;
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

		public int Count { get; protected set; }

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
			CheckComparer ();
			CheckCurrent ();
			// fast return for the typical case where we add sorted values
			if (Comparer.Compare(value, Current.Value) >= 0)
				return Current;
			Node p = Head;
			Node prev = null;
			while (p != null && Comparer.Compare (p.Value, value) <= 0) {
				prev = p;
				p = p.Next;
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
			Node node = new Node(value);
			node.Next = Head;
			Concurrency.LockFreeUpdate (ref m_Head, node);
			if (Current == null)
				Current = node;
			Count++;
			OnInsert (value);
		}

		public void AddLast(T value)
		{	
			if (Head == null || Count == 0) 
			{				
				Head = new Node(value);
				Current = Head;
				Count = 1;
			} 
			else 
			{				
				CheckCurrent ();
				Node prev = Current;
				Current = new Node (value);
				prev.Next = Current;
				Count++;
			}
			OnInsert (value);
		}

		public void InsertAt(int index, T value)
		{
			if (Head == null || Count == 0 || index >= Count)
				AddLast (value);
			else if (index == 0)
				AddFirst (value);
			else 
			{
				Node prev = Head;
				int i = 0;
				while (prev.Next != null && i++ < index - 1)
					prev = prev.Next;

				Node next = prev.Next;
				Node curr = new Node (value);
				prev.Next = curr;
				curr.Next = next;
				if (prev == Current)
					Current = next;
				Count++;
				OnInsert (value);
			}
		}

		public bool InsertItem(T prevValue, T value)
		{
			Node prev = Search (prevValue);
			if (prev == null)
				return false;
			Node next = prev.Next;
			Node curr = new Node (value);
			prev.Next = curr;
			curr.Next = next;
			if (prev == Current)
				Current = next;
			Count++;
			OnInsert (value);
			return true;
		}

		public void InsertBefore(Node curr, T value)
		{
			if (curr == null) {
				AddLast (value);	// it never happens
			}
			else if (curr == Head) 
			{
				AddFirst (value);
			}
			else 
			{
				//Insert (curr, value);

				Node prev = Head;
				while (prev.Next != null && prev.Next != curr)
					prev = prev.Next;

				Node next = prev.Next;
				curr = new Node (value);
				prev.Next = curr;
				curr.Next = next;
				if (prev == Current)
					Current = next ?? curr;
				Count++;
				OnInsert (value);
				/*** ***/
			}
		}

		public void InsertAfter(Node prev, T value)
		{
			if (prev == null) 
			{
				AddLast (value);
				return;
			}

			Node next = prev.Next;
			Node curr = new Node (value);
			prev.Next = curr;
			curr.Next = next;
			if (prev == Current)
				Current = next ?? curr;
			Count++;
			OnInsert (value);
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
			
		public void AddOrUpdate(T value)
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

		private void CheckComparer()
		{
			if (Comparer == null)
				Comparer = Comparer<T>.Default;
		}


		/* Function to swap Nodes x and y in linked list by changing links */
		public void SwapValues(T x, T y)
		{
			CheckComparer ();	

			// Nothing to do if x and y are same
			int cmp = Comparer.Compare(x, y);
			if (cmp == 0) return;

			// Search for x (keep track of prevX and CurrX)
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

			// If x is not head of linked list
			if (prevX != null)
				prevX.Next = currY;
			else //make y the new head				
				Head = currY;

			// If y is not head of linked list
			if (prevY != null)
				prevY.Next = currX;
			else // make x the new head
				Head = currX;

			// Swap next pointers
			Node temp = currX.Next;
			currX.Next = currY.Next;
			currY.Next = temp;
		}

		// *** Removing Items ***

		public bool Remove(T value)
		{
			if (value == null || Head == null)
				return false;

			Node curr = Head;
			if (curr.Value.Equals (value)) 
			{				
				Head = curr.Next;
				Count--;
				OnRemove (value);
				return true;
			}

			while (curr.Next != null && !value.Equals (curr.Next.Value)) 
				curr = curr.Next;

			if (curr.Next != null) 
			{
				Node prev = curr;
				curr = curr.Next;
				prev.Next = curr.Next;
				// delete current
				if (curr == Current)
					Current = prev;
				Count--;
				OnRemove (value);
				return true;
			}

			return false;
		}

		public T RemoveLast()
		{			
			return RemoveAt (Count - 1);
		}

		public void Remove(Node node)
		{
			// Current ist danach Prev.

			if (node == null || Head == null)
				return;
			if (node == Head) {
				RemoveFirst ();
				return;
			}

			Node curr = Head; 
			Node prev = curr;

			while (curr != null)
			{
				if (curr == node) {
					prev.Next = curr.Next;
					if (prev.Next == null)
						Current = prev;
					Count--;
					OnRemove (node.Value);
					return;
				}

				prev = curr;
				curr = curr.Next;
			}
		}

		public T RemoveFirst()
		{
			if (Head == null)
				return default(T);

			T val = Head.Value;
			if (Head.Next == null)
				Clear ();
			else 
			{
				Head = Head.Next;
				Count--;
			}
			OnRemove (val);
			return val;
		}

		public T RemoveAt(int index)
		{
			if (Head == null)
				return default(T);

			if (index == 0)
			{
				return RemoveFirst ();
			}

			if (index >= 0 && index < Count)
			{
				Node curr = Head; 
				Node prev = curr;
				int count = 0;

				while (curr != null)
				{
					if (count == index)
					{
						prev.Next = curr.Next;
						Count--;
						//if (curr == Current)
						if (index == Count)
							Current = prev;
						OnRemove (curr.Value);
						return curr.Value;
					}

					count++; 
					prev = curr;
					curr = curr.Next;
				}
			}

			return default(T);
		}			


		public void RemoveSortedRange(T low, T high) 
		{
			Node previousNode = Head;
			Node deleteNode = previousNode.GetNext();
			while (deleteNode != null) {
				if (deleteNode.Value.CompareTo(low) >= 0 && deleteNode.Value.CompareTo(high) <= 0) {
					previousNode.SetNext(deleteNode.GetNext());
					Count--;
				} else {
					previousNode = previousNode.GetNext();
				}
				deleteNode = deleteNode.GetNext();
			}
		}

		public void AppendRange(IEnumerable<T> other)
		{
			foreach (var t in other)
				this.AddLast(t);			
		}

		public int RemoveRange(int start, int len) 
		{
			Node previousNode = Head;
			Node deleteNode = previousNode.GetNext();
			int i = 0;
			int k = 0;
			if (start == 0)
				k++;
			while (deleteNode != null && k < len) {				
				if (i >= start - 1) {
					previousNode.SetNext(deleteNode.GetNext());
					//Count--;
					k++;
				} else {
					previousNode = previousNode.GetNext();
					i++;
				}
				deleteNode = deleteNode.GetNext();
			}
			if (start == 0)
				Head = deleteNode;
			Count -= k;
			CheckCurrent (true);
			return k;
		}
			
		public void Clear()
		{
			Head = null;
			Current = null;
			Count = 0;
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
			Node curr = Head;
			int count = 1;
			if (curr != null) 
			{
				while (curr.Next != null) {
					curr = curr.Next;
					count++;
				}
			}
			Current = curr;
			Count = count;
		}

		public void CheckCurrent(bool reset = false)
		{
			if (Head == null) {
				Current = null;
			} else {
				if (Current == null || reset)
					Current = Head;
				while (Current.Next != null)
					Current = Current.Next;
			}
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
			if (comparer != Comparer) 
			{
				Comparer = comparer;
				Sort ();
			}
		}

		public void Sort()
		{
			if (Count == 0)
				return;

			if (Comparer == null)
				Comparer = Comparer<T>.Default;

			Head = MergeSort(Head);
			Current = null;
		}

		// MergeSort Linked List
		//The main function
		public Node MergeSort(Node head) 
		{
			if (head == null || head.Next == null) 
				return head;

			Node middle = GetMiddle(head);      //get the middle of the list
			Node sHalf = middle.Next; 
			middle.Next = null;   				//split the list into two halfs
			return Merge(MergeSort(head), MergeSort(sHalf));  //recurse on that
		}

		//Merge subroutine to merge two sorted lists
		public Node Merge(Node a, Node b) 
		{
			Node dummyHead, curr; 
			dummyHead = new Node(); 
			curr = dummyHead;
			IComparer<T> comparer = Comparer;

			while ( a != null && b != null ) 
			{				
				//if ( a.Value <= b.Value) 
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
		public Node GetMiddle(Node head)
		{
			if(head == null) 
				return head;

			Node slow, fast;
			slow = fast = head;
			while(fast.Next != null && fast.Next.Next != null) 
			{
				slow = slow.Next; 
				fast = fast.Next.Next;
			}
			return slow;
		}

		public Node GetMiddlePrevious(Node first, Node last)
		{
			if(first == null) 
				return null;
			if (first == last)
				return first;
			Node slow, fast, prev;
			slow = fast = first;

			prev = null;
			while(fast.Next != last && fast.Next.Next != last)
			{
				prev = slow;
				slow = slow.Next;
				fast = fast.Next.Next;
			}

			return prev;
		}
	}

	public static class LinkedListExtensions
	{		
		public static void DumpNodes<T>(this ClassicLinkedList<T> list) where T: IComparable<T>
		{
			if (list == null) 
			{
				Console.WriteLine("LinkedList<T> DumpNodes: list is null.");
			}

			ClassicLinkedList<T>.Node tempNode = list.Head; 
			while (tempNode != null)
			{
				Console.WriteLine(tempNode.Value);
				tempNode = tempNode.Next;
			}
		}

		[DebuggerStepThrough]
		public static void ForEach<T>(this ClassicLinkedList<T> list, Action<T> action) where T : IComparable<T>
		{
			ClassicLinkedList<T>.Node curr = list.Head;
			while (curr != null) 
			{
				action (curr.Value);
				curr = curr.Next;
			}
		}

		public static ClassicLinkedList<T>.Node Skip<T>(this ClassicLinkedList<T>.Node node, int count) where T : IComparable<T>
		{
			var nx = node;
			int i = 0;
			while (i++ < count && nx != null)
				nx = nx.Next;			
			return nx;
		}

		public static ClassicLinkedList<T>.Node Seek<T>(this ClassicLinkedList<T> list, int index) where T : IComparable<T>
		{
			if (index < 0 || index >= list.Count)
				return null;
			return list.Head.Skip (index);
		}

		public static ClassicLinkedList<T>.Node GetNext<T>(this ClassicLinkedList<T>.Node node) where T : IComparable<T>
		{
			if (node == null)
				return null;
			return node.Next;
		}

		public static void SetNext<T>(this ClassicLinkedList<T>.Node node, ClassicLinkedList<T>.Node target) where T : IComparable<T>
		{
			if (node != null)
				node.Next = target;
		}

		public static T GetValue<T>(this ClassicLinkedList<T>.Node node) where T : IComparable<T>
		{
			if (node == null)
				return default(T);
			return (T)node.Value;
		}
	}		
}

