using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace KS.Foundation
{	
	// Allows duplicate entries. Therefore it has "AddLeft, AddRight, .."
	// which means: Add-in-Front, Add-at-Last..
	// Node-Values are immutable, this tree never swaps values !

	public class BalancedOrderStatisticTree<T> : IEnumerable<T> where T : IComparable<T>
	{
		[NonSerialized]
		public readonly object SyncObject = new object ();

		public IComparer<T> Comparer { get; protected set ; }

		public Node Root { get; private set; }
		public Node Head { get; private set; }
		public Node Tail { get; private set; }

		public BalancedOrderStatisticTree()
		{
			Comparer = Comparer<T>.Default;
		}

		public BalancedOrderStatisticTree(IComparer<T> comparer)
		{
			if (comparer == null)
				comparer = Comparer<T>.Default;
			Comparer = comparer;
		}

		public BalancedOrderStatisticTree(IEnumerable<T> source) : this(source, Comparer<T>.Default) {}
		public BalancedOrderStatisticTree(IEnumerable<T> source, IComparer<T> comparer)
			: this(comparer)
		{			
			if (source != null) {
				foreach (T item in source)
					AddRight (item);
			}
		}			

		public Node Add(T item) 
		{
			if (Root == null || Comparer.Compare(item, Root.Value) < 0)
				return AddLeft(item);
			else
				return AddRight(item);
		}

		public Node AddLeft(T item) 
		{
			Node node = new Node(item);
			if (Root == null) {
				Root = Head = Tail = node;
			} else {
				Root.InsertLeft(this, node);
				if (Comparer.Compare(Head.Value, item) >= 0) 
					Head = node;
				else if (Comparer.Compare(Tail.Value, item) < 0) 
					Tail = node;
			}
			return node;
		}

		public Node AddRight(T item) {
			Node node = new Node(item);
			if (Root == null) {
				Root = Head = Tail = node;
			} else {
				Root.InsertRight(this, node);
				if (Comparer.Compare(Head.Value, item) > 0) 
					Head = node;
				else if (Comparer.Compare(Tail.Value, item) <= 0) 
					Tail = node;
			}
			return node;
		}

		public T PeekHead ()
		{
			if (Head == null) 
				return default(T);
			return Head.Value;
		}

		public T PopHead () 
		{
			if (Head == null)
				return default(T);
			Node n = Head;
			RemoveNode(Head);
			return n.Value;
		}

		public bool Remove(T item) 
		{			
			if (item == null)
				return false;

			return RemoveRight(item);
		}

		bool Remove(T item, bool useRight) 
		{
			if (Root == null || item == null)
				return false;			
			Node node = Find(Root, item, useRight);
			if (node == null) 
				return false;
			return RemoveNode(node);
		}

		public bool RemoveLeft(T item) 
		{
			return Remove(item, false);
		}

		public bool RemoveRight(T item) 
		{
			return Remove(item, true);
		}

		public bool RemoveAt(int index)
		{
			Node node = Root.Select(index);
			if (node == null)
				return false;
			RemoveNode (node);
			return true;
		}

		public bool RemoveNode(Node n) 
		{
			// Can't remove nothing...
			if (n == null) 
				return false;
			else if (n == Head) 
				Head = Head.Next();
			else if (n == Tail) 
				Tail = Tail.Previous();

			if (n.LeftChild == null) {
				// Decrement left/right counts
				Node tmp = n;
				while (tmp.Parent != null) {
					if (tmp.Parent.LeftChild == tmp) {
						tmp.Parent.Left--;
					} else {
						//Debug.Assert (tmp.Parent.RightChild == tmp);
						tmp.Parent.Right--;
					}
					tmp = tmp.Parent;
				}

				// if we have a child, update its parentage
				if (n.RightChild != null) {
					n.RightChild.Parent = n.Parent;
				}

				// if we're the root node, update that
				if (n.Parent == null) {
					Root = n.RightChild;
				} else {
					// update the parent's children
					if (n.Parent.LeftChild == n) {
						n.Parent.LeftChild = n.RightChild;
					} else {
						//Debug.Assert (n.Parent.RightChild == n);
						n.Parent.RightChild = n.RightChild;
					}
				}

				for (Node p = n.Parent; p != null; p = p.Parent) {
					if (!p.UpdateHeight(this)) 
						break;
				}
			} else if (n.RightChild == null) {
				Node tmp = n;
				while (tmp.Parent != null) {
					if (tmp.Parent.LeftChild == tmp) {
						tmp.Parent.Left--;
					} else {
						//Debug.Assert (tmp.Parent.RightChild == tmp);
						tmp.Parent.Right--;
					}
					tmp = tmp.Parent;
				}

				n.LeftChild.Parent = n.Parent;

				if (n.Parent == null) {
					Root = n.LeftChild;
				} else {
					if (n.Parent.LeftChild == n) {
						n.Parent.LeftChild = n.LeftChild;
					} else {
						//Debug.Assert (n.Parent.RightChild == n);
						n.Parent.RightChild = n.LeftChild;
					}
				}

				for (Node p = n.Parent; p != null; p = p.Parent) {
					if (!p.UpdateHeight(this)) 
						break;
				}
			} else {
				Node p = n.Previous();
				// Couple of assertions. If these fail, prev() is broken; there should be a node between p and n
				//Debug.Assert((p.Parent == n && n.LeftChild == p) || (p == p.Parent.RightChild));
				//Debug.Assert(p.RightChild == null);

				// First, disconnect p from the existing tree
				bool wasLChild = false;

				if (p.Parent == n) {
					wasLChild = true;
					n.LeftChild = p.LeftChild;
					if (p.LeftChild != null) {
						p.LeftChild.Parent = n;
					}
				} else {
					p.Parent.RightChild = p.LeftChild;
					if (p.LeftChild != null) {
						p.LeftChild.Parent = p.Parent;
					}
				}

				for (Node tmp = p; tmp.Parent != null; tmp = tmp.Parent) {
					if (((wasLChild) && (tmp == p)) || (tmp.Parent.LeftChild == tmp)) {
						tmp.Parent.Left--;
					} else {
						Debug.Assert(((!wasLChild) && (tmp == p)) || (tmp.Parent.RightChild == tmp));
						tmp.Parent.Right--;
					}
				}

				for (Node tmp = p; tmp.Parent != null; tmp = tmp.Parent) {
					if (!tmp.Parent.UpdateHeight(this)) 
						break;
				}

				// Then, insert p where n was
				p.Parent = n.Parent;
				p.RightChild = n.RightChild;
				p.LeftChild = n.LeftChild;
				p.Left = n.Left;
				p.Right = n.Right;
				p.Height = n.Height;

				// And clean up what used to point to n
				if (p.RightChild != null) {
					p.RightChild.Parent = p;
				}

				if (p.LeftChild != null) {
					p.LeftChild.Parent = p;
				}

				if (n.Parent == null) {
					Root = p;
				} else {
					if (n.Parent.LeftChild == n) {
						p.Parent.LeftChild = p;
					} else {
						//Debug.Assert(n.Parent.RightChild == n);
						p.Parent.RightChild = p;
					}
				}
			}

			//Debug.Assert(Root == null || Root.Verify());
			return true;
		}

		private int Rank(T item, bool useRight) 
		{
			if (Root == null || item == null) 
				return -1;
			Node n = Find(Root, item, useRight);
			if (n == null) 
				return -1;
			return n.Rank();
		}

		public int RankLeft(T item) {
			return Rank(item, false);
		}

		public int RankRight(T item) {
			return Rank(item, true);
		}

		public T this [int index]
		{
			get{
				return Select(index);
			}
		}

		public T Select(int index) 
		{			
			if (Root == null || index < 0 || index >= Count)	// nicht >= ???
				return default(T);
			Node n = Root.Select(index);
			if (n == null)
				return default(T);
			return n.Value;
		}

		// das ist der aktuelle Rank, 
		// Left und Right sind Left.Rank und Right.Rank, oder nicht ??
		public int Count 
		{
			get{
				if (Root == null)
					return 0;			
				// Summe des linken Teilbaums 
				// + Summe des rechten Teilbaums 
				// + das Rootelement.
				return Root.Left + 1 + Root.Right;

				// Und was ist dann noch Height ?
				// Rank des Elements in der Mitte ?
				// mir kommt's so vor, als hätten wir 3 mal Rank gespeichert,
				// aber der Rank wird dann noch mühsam ermittelt
			}
		}

		// Dieser Baum steht also definitiv auf dem Kopf.
		// Das sind immer die Bäume, die ich nicht verstehe..

		// wird gar nicht gebraucht.. wie dumm
		/**** ***/
		public int Height
		{
			get{
				if (Root == null)
					return 0;			
				return Root.Height;

				// Wird Height nun aufaddiert oder nicht ?

				// Height dient wohl nur dem Ausballancieren, 
				// was sonst auch 'Balance' genannt wird.
			}
		}

		public void Clear()
		{
			Root = null;
			Head = null;
			Tail = null;
		}


		public Node Find(T val, bool useRight = false)
		{
			return Find (Root, val, useRight);
		}

		/// <summary>
		/// Find the node under this one with the given value.
		/// </summary>
		/// <param name="val">the value we're searching for</param>
		/// <param name="useRight">whether to use the right-most matching node, or the left-most</param>
		public Node Find(Node node, T val, bool useRight = false) 
		{				
			//int cmp = Value.CompareTo(val);
			int cmp = Comparer.Compare(node.Value, val);
			if (cmp < 0 || (cmp == 0 && useRight)) { 
				// too early; search right side
				Node res = null;

				if (node.RightChild != null) 
					res = Find(node.RightChild, val, useRight);

				if (res == null && cmp == 0) 
					return node;

				return res;
			} else { 
				// too late; search left side
				Node res = null;

				if (node.LeftChild != null) 
					res = Find(node.LeftChild, val, useRight);

				if (res == null && cmp == 0) 
					return node;

				return res;
			}
		}

		public Node FindLeft(Node node, T val) 
		{
			return Find(node, val, false);
		}

		public Node FindRight(Node node, T val) 
		{
			return Find(node, val, true);
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<T> GetEnumerator()
		{
			var node = Head;
			while(node != null)
			{
				yield return node.Value;
				node = node.Next();
			}
		}			

		// *********** NODE ***********

		public class Node 
		{
			public T Value { get; private set; }
			public Node Parent { get; internal set; }
			public Node LeftChild { get; internal set; }
			public Node RightChild { get; internal set; }
			public int Left { get; internal set; }
			public int Right { get; internal set; }
			public int Height { get; internal set; }

			public override string ToString ()
			{
				return string.Format ("[Node: Value={0}, Parent={1}, LeftChild={2}, RightChild={3}, Left={4}, Right={5}, Height={6}]", Value, Parent, LeftChild, RightChild, Left, Right, Height);
			}

			public Node(T item) 
			{
				Value = item;

				// genau hier müsste Height auf das gesetzt werden,
				// was vorher 'Count' ist: links + rechts + 1
				// Dann würde er mitzählen und müsste nicht ständig suchen..

				Height = 1;
			}

			public Node Head() 
			{
				Node tmp = this;
				while (tmp.LeftChild != null) 
					tmp = tmp.LeftChild;
				return tmp;
			}

			public Node Tail() 
			{
				Node tmp = this;
				while (tmp.RightChild != null)
					tmp = tmp.RightChild;
				return tmp;
			}				

			int HeightLeft()
			{
				int h = 0;
				if (LeftChild != null) 
					h = LeftChild.Height;				
				return h;
			}

			int HeightRight() 
			{
				int h = 0;
				if (RightChild != null) 
					h = RightChild.Height;
				return h;
			}

			void RotateLeftUp(int rightHeight, BalancedOrderStatisticTree<T> tree) 
			{
				//Debug.Assert (LeftChild != null);

				Node child = LeftChild;
				int hLeft = child.HeightLeft ();
				int hRight = child.HeightRight();

				LeftChild = child.RightChild;
				child.RightChild = this;

				child.Parent = this.Parent;
				this.Parent = child;

				if (LeftChild != null) 
					LeftChild.Parent = this;

				if (child.Parent == null) {					
					tree.Root = child;
				} else {
					if (child.Parent.LeftChild == this) {
						child.Parent.LeftChild = child;
					} else {
						//Debug.Assert (child.Parent.RightChild == this);
						child.Parent.RightChild = child;
					}
				}

				Left = child.Right;
				child.Right = Left + 1 + Right;

				Height = Math.Max (hRight, rightHeight) + 1;
				child.Height = Math.Max (hLeft, Height) + 1;
			}

			void RotateRightUp(int leftHeight, BalancedOrderStatisticTree<T> tree) 
			{				
				Node child = RightChild;

				//Debug.Assert (RightChild != null);
				//Debug.Assert (child.Verify ());

				int hLeft = child.HeightLeft(), hRight = child.HeightRight();

				RightChild = child.LeftChild;
				child.LeftChild = this;

				child.Parent = this.Parent;
				this.Parent = child;
				if (RightChild != null) RightChild.Parent = this;
				if (child.Parent == null) {
					tree.Root = child;
				} else {
					if (child.Parent.LeftChild == this) {
						child.Parent.LeftChild = child;
					} else {
						//Debug.Assert (child.Parent.RightChild == this);
						child.Parent.RightChild = child;
					}
				}

				Right = child.Left;
				child.Left = Left + 1 + Right;

				Height = Math.Max (hLeft, leftHeight) + 1;
				child.Height = Math.Max (hRight, Height) + 1;
			}

			internal bool UpdateHeight(BalancedOrderStatisticTree<T> tree)
			{
				int hLeft = HeightLeft(), hRight = HeightRight();
				if (hLeft > hRight + 1) 
				{
					//Debug.Assert (LeftChild != null);
					int hLLeft = LeftChild.HeightLeft(), hLRight = LeftChild.HeightRight();
					if (hLLeft < hLRight) {
						LeftChild.RotateRightUp(hLLeft, tree);
					}
					RotateLeftUp(hRight, tree);
					Parent.Height = 0; // Force height of parent (who we just rotated up there) to update
					return true;
				} else if (hRight > hLeft + 1) {
					//Debug.Assert (RightChild != null);
					int hRLeft = RightChild.HeightLeft(), hRRight = RightChild.HeightRight();
					if (hRLeft > hRRight) 
					{
						RightChild.RotateLeftUp(hRRight, tree);
					}
					RotateRightUp(hLeft, tree);
					Parent.Height = 0; // Force height of parent (who we just rotated up there) to update
					return true;
				}

				if (hLeft > hRight)  {
					if (Height != hLeft + 1) {
						Height = hLeft + 1;
						return true;
					} 
					return false;
				} else {
					if (Height != hRight + 1) {
						Height = hRight + 1;
						return true;
					} 
					return false;
				}
			}

			public int Rank() 
			{
				int c = Left;
				Node tmp = this;
				while (tmp.Parent != null) {
					if (tmp.Parent.LeftChild != tmp) {
						c += tmp.Parent.Left + 1;
					}
					tmp = tmp.Parent;
				}

				Console.WriteLine ("Height: {0}, Rank: {1}", Height, c);

				return c;
			}				

			public Node Select(int index) 
			{
				if (index == Left) 
					return this;
				if (index < Left)
					return LeftChild.Select(index);
				return RightChild.Select(index - Left - 1);
			}

			void Insert(BalancedOrderStatisticTree<T> tree, Node n, bool useRight) 
			{
				int cmp = tree.Comparer.Compare (n.Value, Value);
				if (cmp == 0) {
					if (useRight) 
						cmp = 1;
					else
						cmp = -1;					
				}

				if (cmp < 0) { // n.value < this.value
					Left++;
					if (LeftChild == null) {
						n.Parent = this;
						LeftChild = n;
						if (RightChild == null) {
							Height++;
							for (Node p = Parent; p != null; p = p.Parent) {
								if (!p.UpdateHeight(tree)) 
									break;
							}
						}
					} else {
						LeftChild.Insert(tree, n, useRight);
					}
				} else { // n.value > this.value
					Right++;
					if (RightChild == null) {
						n.Parent = this;
						RightChild = n;
						if (LeftChild == null) {
							Height++;
							for (Node p = Parent; p != null; p = p.Parent) {
								if (!p.UpdateHeight(tree)) 
									break;
							}
						}
					} else {
						RightChild.Insert(tree, n, useRight);
					}
				}
			}

			public void InsertRight(BalancedOrderStatisticTree<T> tree, Node n) 
			{
				Insert(tree, n, true);
			}

			public void InsertLeft (BalancedOrderStatisticTree<T> tree, Node n) 
			{
				Insert(tree, n, false);
			}

			public Node Next() 
			{				
				if (RightChild != null) 							
					return RightChild.Head();				

				if (Parent == null) 
					return null;

				if (Parent.LeftChild == this) 
					return Parent;

				Node tmp = this;
				while (tmp.Parent != null && tmp.Parent.RightChild == tmp)
					tmp = tmp.Parent;
				return tmp.Parent;
			}

			public Node Previous() 
			{
				if (LeftChild != null)
					return LeftChild.Tail();

				if (Parent == null)
					return null;

				if (Parent.RightChild == this)
					return Parent;

				Node tmp = this;
				while (tmp.Parent != null && tmp.Parent.LeftChild == tmp)
					tmp = tmp.Parent;
				return tmp.Parent;
			}

			/***
			/// <summary>
			/// Find the node under this one with the given value.
			/// </summary>
			/// <param name="val">the value we're searching for</param>
			/// <param name="useRight">whether to use the right-most matching node, or the left-most</param>
			public Node Find(BalancedOrderStatisticTree<T> tree, T val, bool useRight = false) 
			{				
				//int cmp = Value.CompareTo(val);
				int cmp = tree.Comparer.Compare(Value, val);
				if (cmp < 0 || (cmp == 0 && useRight)) { 
					// too early; search right side
					Node res = null;

					if (RightChild != null) 
						res = RightChild.Find(tree, val, useRight);

					if (res == null && cmp == 0) 
						return this;

					return res;
				} else { 
					// too late; search left side
					Node res = null;

					if (LeftChild != null) 
						res = LeftChild.Find(tree, val, useRight);

					if (res == null && cmp == 0) 
						return this;

					return res;
				}
			}

			public Node FindLeft(BalancedOrderStatisticTree<T> tree, T val) 
			{
				return Find(tree, val, false);
			}

			public Node FindRight(BalancedOrderStatisticTree<T> tree, T val) 
			{
				return Find(tree, val, true);
			}
			***/
				
			#if DEBUG
			public bool Verify() 
			{				
				int hLeft  = 0, hRight = 0;
				if (Parent != null)
					Debug.Assert (Parent.LeftChild == this || Parent.RightChild == this, "Parent of " + Value.ToString() + " only has children " + (Parent.LeftChild == null ? "(null)" : Parent.LeftChild.Value.ToString()) + " and " + (Parent.RightChild == null ? "(null)" : Parent.RightChild.Value.ToString()) + " (parent = " + Parent.Value.ToString() + ")");

				if (LeftChild != null) 
				{
					Debug.Assert (LeftChild.Parent == this, "Left child (" + LeftChild.Value + ") has parent " + LeftChild.Parent.Value + ", not " + Value);
					LeftChild.Verify();
					Debug.Assert (Left == LeftChild.Left + 1 + LeftChild.Right, "Left count at node " + Value + " should be " + (LeftChild.Left + 1 + LeftChild.Right) + ", not " + Left);
					hLeft = LeftChild.Height;
				} 
				else 
				{
					Debug.Assert (Left == 0);
				}

				if (RightChild != null) 
				{
					Debug.Assert (RightChild.Parent == this, "Right child (" + RightChild.Value + ") has parent " + RightChild.Parent.Value + ", not " + Value);
					RightChild.Verify();
					Debug.Assert(Right == RightChild.Left + 1 + RightChild.Right, "Right count at node " + Value + " should be " + (RightChild.Left + 1 + RightChild.Right) + ", not " + Right);
					hRight = RightChild.Height;
				} 
				else 
				{
					Debug.Assert (Right == 0);
				}

				if (hLeft > hRight) 
				{
					Debug.Assert (Height == hLeft + 1, "Height at node " + Value + " should be " + (hLeft + 1) + ", not " + Height);
				} 
				else 
				{
					Debug.Assert (Height == hRight + 1, "Height at node " + Value + " should be " + (hRight + 1) + ", not " + Height);
				}

				return true; // "false" would be if one of the assertions actually failed, in which case an exception is raised
			}
			#endif
		}			
	}		
}

