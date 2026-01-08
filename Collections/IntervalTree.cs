//**************************************************
// File Name               : IntervalTree.cs
//      Created            : 06 8 2012   22:38
//      Author             : Costin S
//
//**************************************************
//
// Published unter MIT -license at
// https://code.google.com/p/intervaltree/
//
// Full MIT license at
// http://opensource.org/licenses/mit-license.php
//
// Modified by Kroll

using System;
using System.Linq;
using System.Collections.Generic;

namespace KS.Foundation
{   
	/***
	public interface IInterval<T, TVAL> : IEquatable<T> where TVAL : IComparable<TVAL>
	{
		TVAL Start { get; }
		TVAL End { get; }
	}
	public class IntervalTree<T, TVAL> where T : IInterval<T, TVAL> where TVAL : IComparable<TVAL>
	***/

	public interface IInterval<T> : IEquatable<T>
	{
		DateTime StartDate { get; }
		DateTime EndDate { get; }
	}
		
	public class IntervalTree<T> where T : IInterval<T>
	{
		public IntervalNode Root { get; protected set; }
		public int Count { get; protected set; }
		public delegate void VisitNodeHandler(IntervalNode node, int level);

		public IntervalTree()
		{
		}

		public IntervalTree(IEnumerable<T> elems, bool summarizeDurationDays = false)
		{
			SummarizeDurationDays = summarizeDurationDays;
			if (!elems.IsNullOrEmpty())
			{
				foreach (var elem in elems)
				{
					if (elem != null)
					{
						Add(elem);

						if (SummarizeDurationDays && elem.EndDate.IsDefined() && elem.StartDate.IsDefined())
							m_SummarizedDurationDays += (elem.EndDate - elem.StartDate).TotalDays;
					}
				}
			}

			m_MinMaxValuesDirty = true;
		}

		bool m_MinMaxValuesDirty = true;
		void GetMinMax()
		{
			if (m_MinMaxValuesDirty)
			{
				if (Root == null || Count == 0)
				{
					m_MinValue = DateTime.MaxValue;
					m_MaxValue = DateTime.MinValue;
				}
				else
				{
					IntervalNode node = Root;
					while (node.Left != null)
						node = node.Left;

					if (node == null)
						m_MinValue = DateTime.MaxValue;
					else
						m_MinValue = node.Value.StartDate;                    

					if (Root.Max.IsDefined())
						m_MaxValue = Root.Max;
					else
						m_MaxValue = DateTime.MinValue;
				}

				m_MinMaxValuesDirty = false;
			}
		}        

		private DateTime m_MinValue = DateTime.MaxValue;
		public DateTime MinValue
		{
			get
			{
				GetMinMax();
				return m_MinValue;
			}
		}

		private DateTime m_MaxValue = DateTime.MinValue;
		public DateTime MaxValue
		{
			get
			{
				GetMinMax();
				return m_MaxValue;                
			}
		}

		public bool SummarizeDurationDays { get; private set; }
		protected double m_SummarizedDurationDays = 0d;
		public double SummarizedDurationDays
		{
			get
			{
				return m_SummarizedDurationDays;
			}
		}

		public static int Compare(T r1, T r2)
		{			
			int ret = r1.StartDate.CompareTo (r2.StartDate);
			if (ret == 0) {
				ret = r2.EndDate.CompareTo (r1.EndDate);
				if (ret == 0) {
					ret = r1.GetHashCode ().CompareTo (r2.GetHashCode ());
				}
			}
			
			return ret;
		}

		public bool Equals(T r1, T r2)
		{
			return r1.Equals(r2);
		}

		static bool Intersect (T i1, T i2)
		{			
			return i1.EndDate.CompareTo(i2.StartDate) > 0 && i1.StartDate.CompareTo(i2.EndDate) < 0;
		}

		static bool Intersect (T i1, DateTime start, DateTime end)
		{
			return i1.EndDate.CompareTo(start) > 0 && i1.StartDate.CompareTo(end) < 0;
		}

		//public virtual bool Add(T value, out IntervalNode node)
		public virtual bool Add(T value)
		{
			bool wasAdded;
			bool wasSuccessful;

			Root = IntervalNode.Add(Root, value, out wasAdded, out wasSuccessful);
			if (Root != null) {
				IntervalNode.ComputeMax(Root);
			}

			if (wasSuccessful) {
				Count++;
				m_MinMaxValuesDirty = true;

				if (SummarizeDurationDays && value.EndDate.IsDefined() && value.StartDate.IsDefined())
					m_SummarizedDurationDays += (value.EndDate - value.StartDate).TotalDays;
			}

			return wasSuccessful;
		}

		public virtual bool Delete(T value)
		{
			if (Root != null) {
				bool wasDeleted;
				bool wasSuccessful;

				Root = IntervalNode.Delete(Root, value, out wasDeleted, out wasSuccessful);
				if (Root != null) {                    
					IntervalNode.ComputeMax(Root);
				}

				if (wasSuccessful) {
					Count--;
					m_MinMaxValuesDirty = true;

					if (SummarizeDurationDays && value.EndDate.IsDefined() && value.StartDate.IsDefined())
						m_SummarizedDurationDays -= (value.EndDate - value.StartDate).TotalDays;                    
				}
				return wasSuccessful;
			}
			return false;
		}

		public IEnumerable<T> GetIntervalsOverlappingWith(T toFind)
		{
			return (Root != null) ? Root.GetIntervalsOverlappingWith(toFind.StartDate, toFind.EndDate) : Enumerable.Empty<T>();
		}

		/*** Warning: ToDo: Not including Start !!
		public IEnumerable<T> GetIntervalsOverlappingWith(DateTime start)
		{
			return GetIntervalsOverlappingWith(start, start);
		}
		***/

		public IEnumerable<T> GetIntervalsOverlappingWith(DateTime start, DateTime end)
		{
			return (Root != null) ? Root.GetIntervalsOverlappingWith(start, end) : Enumerable.Empty<T>();
		}

		public IEnumerable<T> GetIntervalsStartingAt(DateTime start)
		{
			return IntervalNode.GetIntervalsStartingAt(Root, start);
		}

		public IEnumerable<T> Intervals
		{
			get {
				if (Root == null) {
					yield break;
				}

				var p = IntervalNode.FindMin(Root);
				while (p != null) {
					yield return p.Value;
					p = p.Successor();
				}
			}
		}

		public IEnumerable<T> Values
		{
			get {
				if (Root == null) {
					yield break;
				}

				var p = IntervalNode.FindMin(Root);
				while (p != null) {
					yield return p.Value;
					p = p.Successor();
				}
			}
		}

		public virtual void Clear()
		{            
			Root = null;
			Count = 0;
			m_SummarizedDurationDays = 0d;
			m_MinMaxValuesDirty = true;
		}

		public bool TryGetInterval(T data, out T value)
		{
			return TryGetIntervalImpl(Root, data, out value);
		}			

		protected bool TryGetIntervalImpl(IntervalNode subtree, T data, out T value)
		{
			if (subtree != null) {
				//int compareResult = data.StartDate.CompareTo(subtree.Value.StartDate);
				int compareResult = Compare(data, subtree.Value);
				if (compareResult < 0) {
					return TryGetIntervalImpl(subtree.Left, data, out value);
				} else if (compareResult > 0) {
					return TryGetIntervalImpl(subtree.Right, data, out value);
				} else {					
					if (Compare(data, subtree.Value) == 0) {
						value = subtree.Value;
						return true;
					}
				}
			}
			value = default(T);
			return false;
		}

		protected void Visit(VisitNodeHandler visitor)
		{
			if (Root != null) {
				Root.Visit(visitor, 0);
			}
		}

		public void Print()
		{
			Visit((node, level) => {
				Console.Write(new string(' ', 2 * level));
				Console.Write(string.Format("{0}.{1}", node.Value.ToString(), node.Max));
				Console.WriteLine();
			});
		}

		//static int _newID = 0;
		public class IntervalNode
		{
			//int ID;

			protected IntervalNode Parent;
			public int Balance { get; protected set; }
			public IntervalNode Left { get; protected set; }
			public IntervalNode Right { get; protected set; }
			public T Value { get; protected internal set; }
			public DateTime Max { get; protected set; }

			/***
			public IntervalNode()
			{
				ID = _newID++;
			}
			***/

			public IntervalNode(T value)
			{
				//ID = _newID++;
				Left = null;
				Right = null;
				Balance = 0;                
				Value = value;
				Max = value.EndDate;
			}

			/**
			public override string ToString ()
			{
				return String.Format("Node {0}", ID);
			}
			**/

			public static IntervalNode Add(IntervalNode root, T value, out bool wasAdded, out bool wasSuccessful)
			{
				wasAdded = false;
				wasSuccessful = false;
				if (root == null) {
					root = new IntervalNode(value);                    
					wasAdded = true;
					wasSuccessful = true;
				} else {					
					IntervalNode newChild;
					int cmp = Compare (value, root.Value);
					if (cmp < 0) {
						newChild = Add(root.Left, value, out wasAdded, out wasSuccessful);
						if (root.Left != newChild) {
							root.Left = newChild;
							newChild.Parent = root;
						}
						if (wasAdded) {
							root.Balance--;
							if (root.Balance == 0) {
								wasAdded = false;
							} else if (root.Balance == -2) {
								if (root.Left.Balance == 1) {
									int elemLeftRightBalance = root.Left.Right.Balance;
									root.Left = RotateLeft(root.Left);
									root = RotateRight(root);
									root.Balance = 0;
									root.Left.Balance = elemLeftRightBalance == 1 ? -1 : 0;
									root.Right.Balance = elemLeftRightBalance == -1 ? 1 : 0;
								} else if (root.Left.Balance == -1) {
									root = RotateRight(root);
									root.Balance = 0;
									root.Right.Balance = 0;
								}
								wasAdded = false;
							}
						}
					} else {
						newChild = Add(root.Right, value, out wasAdded, out wasSuccessful);
						if (root.Right != newChild) {
							root.Right = newChild;						
							newChild.Parent = root;
						}
						if (wasAdded) {
							root.Balance++;
							if (root.Balance == 0) {
								wasAdded = false;
							} else if (root.Balance == 2) {
								if (root.Right.Balance == -1) {
									int elemRightLeftBalance = root.Right.Left.Balance;
									root.Right = RotateRight(root.Right);
									root = RotateLeft(root);
									root.Balance = 0;
									root.Left.Balance = elemRightLeftBalance == 1 ? -1 : 0;
									root.Right.Balance = elemRightLeftBalance == -1 ? 1 : 0;
								} else if (root.Right.Balance == 1) {
									root = RotateLeft(root);
									root.Balance = 0;
									root.Left.Balance = 0;
								}
								wasAdded = false;
							}
						}
					}
					ComputeMax(root);
				}
				return root;
			}
				
			public static void ComputeMax(IntervalNode node)
			{
				DateTime maxRange = node.Value.EndDate;

				if (node.Left == null && node.Right == null) {
					node.Max = maxRange;
				} else if (node.Left == null) {
					node.Max = (maxRange.CompareTo(node.Right.Max) >= 0) ? maxRange : node.Right.Max;
				} else if (node.Right == null) {
					node.Max = (maxRange.CompareTo(node.Left.Max) >= 0) ? maxRange : node.Left.Max;
				} else {
					DateTime leftMax = node.Left.Max;
					DateTime rightMax = node.Right.Max;

					if (leftMax.CompareTo(rightMax) >= 0) {
						node.Max = maxRange.CompareTo(leftMax) >= 0 ? maxRange : leftMax;
					} else {
						node.Max = maxRange.CompareTo(rightMax) >= 0 ? maxRange : rightMax;
					}
				}
			}            
				
			public static IntervalNode FindMin(IntervalNode node)
			{
				while (node != null && node.Left != null) {
					node = node.Left;
				}
				return node;
			}
				
			public static IntervalNode FindMax(IntervalNode node)
			{
				while (node != null && node.Right != null) {
					node = node.Right;
				}
				return node;
			}
				
			public IntervalNode Successor()
			{
				if (Right != null)
					return FindMin(Right);
				else
				{
					var p = this;
					while (p.Parent != null && p.Parent.Right == p)
					{
						p = p.Parent;
					}
					return p.Parent;
				}
			}
				
			public IntervalNode Predecesor()
			{
				if (Left != null)
				{
					return FindMax(Left);
				}
				else
				{
					var p = this;
					while (p.Parent != null && p.Parent.Left == p)
					{
						p = p.Parent;
					}
					return p.Parent;
				}
			}

			public static IntervalNode Delete(IntervalNode root, T arg, out bool wasDeleted, out bool wasSuccessful)
			{
				wasDeleted = false;
				wasSuccessful = false;
				IntervalNode newChild;

				int cmp = Compare (arg, root.Value);

				if (cmp < 0) {
					if (root.Left != null) {
						newChild = Delete(root.Left, arg, out wasDeleted, out wasSuccessful);
						root.Left = newChild;
						if (wasDeleted) {
							root.Balance++;
						}
					}
				} else if (cmp == 0) {
					if (root.Left != null && root.Right != null)
					{
						var min = FindMin(root.Right);
						root.Swap(min);
						//SwapNodes (root, min);
						//root = min;

						wasDeleted = false;
						newChild = Delete(root.Right, arg, out wasDeleted, out wasSuccessful);
						root.Right = newChild;
						if (wasDeleted)
						{
							root.Balance--;
						}
					}
					else if (root.Left == null)
					{
						wasDeleted = true;
						wasSuccessful = true;
						if (root.Right != null)
						{
							root.Right.Parent = root.Parent;
						}
						return root.Right;
					}
					else
					{
						wasDeleted = true;
						wasSuccessful = true;					
						if (root.Left != null)
						{
							root.Left.Parent = root.Parent;
						}
						return root.Left;
					}
				} else {
					if (root.Right != null) {
						newChild = Delete(root.Right, arg, out wasDeleted, out wasSuccessful);
						root.Right = newChild;
						if (wasDeleted) {
							root.Balance--;
						}
					}
				}
					
				ComputeMax(root);

				if (wasDeleted) {
					if (root.Balance == 1 || root.Balance == -1) {
						wasDeleted = false;
						return root;
					}
					else if (root.Balance == -2) {
						if (root.Left.Balance == 1) {
							int leftRightBalance = root.Left.Right.Balance;
							root.Left = RotateLeft(root.Left);
							root = RotateRight(root);
							root.Balance = 0;
							root.Left.Balance = (leftRightBalance == 1) ? -1 : 0;
							root.Right.Balance = (leftRightBalance == -1) ? 1 : 0;
						} else if (root.Left.Balance == -1) {
							root = RotateRight(root);
							root.Balance = 0;
							root.Right.Balance = 0;
						} else if (root.Left.Balance == 0) {
							root = RotateRight(root);
							root.Balance = 1;
							root.Right.Balance = -1;
							wasDeleted = false;
						}
					} else if (root.Balance == 2) {
						if (root.Right.Balance == -1) {
							int rightLeftBalance = root.Right.Left.Balance;
							root.Right = RotateRight(root.Right);
							root = RotateLeft(root);
							root.Balance = 0;
							root.Left.Balance = (rightLeftBalance == 1) ? -1 : 0;
							root.Right.Balance = (rightLeftBalance == -1) ? 1 : 0;
						} else if (root.Right.Balance == 1) {
							root = RotateLeft(root);
							root.Balance = 0;
							root.Left.Balance = 0;
						} else if (root.Right.Balance == 0) {
							root = RotateLeft(root);
							root.Balance = -1;
							root.Left.Balance = 1;
							wasDeleted = false;
						}
					}
				}

				return root;
			}

			protected void Swap(IntervalNode node)
			{                
				var dataValue = Value;
				Value = node.Value;
				node.Value = dataValue;
			}

			public static IEnumerable<T> GetIntervalsStartingAt(IntervalNode subtree, DateTime start)
			{
				if (subtree != null) {
					//int compareResult = start.Start.CompareTo(subtree.Value.Start);
					int compareResult = start.CompareTo(subtree.Value.StartDate);
					if (compareResult < 0) {
						foreach (T val in GetIntervalsStartingAt(subtree.Left, start))
							yield return val;
					} else if (compareResult > 0) {
						foreach (T val in GetIntervalsStartingAt(subtree.Right, start))
							yield return val;
					} else {						
						yield return subtree.Value;
					}
				} else {
					yield break;
				}
			}
				
			public IEnumerable<T> GetIntervalsOverlappingWith(T toFind)
			{
				if (toFind.EndDate.CompareTo(Value.StartDate) <= 0) {
					//toFind ends before subtree.Data begins, prune the right subtree
					if (Left != null) {
						foreach (T val in Left.GetIntervalsOverlappingWith(toFind))
							yield return val;
					}
				} else if (toFind.StartDate.CompareTo(Max) >= 0) {
					//toFind begins after the subtree.Max ends, prune the left subtree
					if (Right != null) {
						foreach (T val in Right.GetIntervalsOverlappingWith(toFind))
							yield return val;
					}
				} else {                    
					// search the left subtree
					if (Left != null) {
						foreach (T val in Left.GetIntervalsOverlappingWith(toFind))
							yield return val;
					}

					if (Intersect(Value, toFind)) {
						yield return Value;
					}

					// search the right subtree
					if (Right != null) {
						foreach (T val in Right.GetIntervalsOverlappingWith(toFind))
							yield return val;
					}
				}

				yield break;
			}

			// <summary>
			// Gets all intervals in this subtree that are overlapping the argument interval. 
			// If multiple intervals starting at the same time/value are found to overlap, they are returned in decreasing order of their End values.
			// </summary>
			// <param name="toFind">To find.</param>
			// <returns></returns>
			public IEnumerable<T> GetIntervalsOverlappingWith(DateTime start, DateTime end)
			{
				if (end.CompareTo(Value.StartDate) <= 0) {
					//toFind ends before subtree.Data begins, prune the right subtree
					if (Left != null) {
						foreach (var value in Left.GetIntervalsOverlappingWith(start, end)) {
							yield return value;
						}
					}                    
				} else if (start.CompareTo(Max) >= 0) {
					//toFind begins after the subtree.Max ends, prune the left subtree
					if (Right != null) {
						foreach (var value in Right.GetIntervalsOverlappingWith(start, end)) {
							yield return value;
						}
					}
				} else {
					if (Left != null) {
						foreach (var value in Left.GetIntervalsOverlappingWith(start, end)) {
							yield return value;
						}
					}

					if (Intersect(Value, start, end)) {
						yield return Value;
					}

					if (Right != null) {
						foreach (var value in Right.GetIntervalsOverlappingWith(start, end)) {
							yield return value;
						}
					}
				}
			}

			public void Visit(VisitNodeHandler visitor, int level)
			{
				if (Left != null) {
					Left.Visit(visitor, level + 1);
				}

				visitor(this, level);

				if (Right != null) {
					Right.Visit(visitor, level + 1);
				}
			}
				
			protected static IntervalNode RotateLeft(IntervalNode node)
			{
				var right = node.Right;
				//Debug.Assert(node.Right != null);

				var rightLeft = right.Left;

				node.Right = rightLeft;
				ComputeMax(node);

				var parent = node.Parent;
				if (rightLeft != null) {
					rightLeft.Parent = node;
				}
				right.Left = node;
				ComputeMax(right);

				node.Parent = right;
				if (parent != null) {
					if (parent.Left == node) {
						parent.Left = right;
					} else {
						parent.Right = right;
					}
				}
				right.Parent = parent;
				return right;
			}
				
			protected static IntervalNode RotateRight(IntervalNode node)
			{
				var left = node.Left;
				//Debug.Assert(node.Left != null);

				var leftRight = left.Right;
				node.Left = leftRight;
				ComputeMax(node);

				var parent = node.Parent;
				if (leftRight != null) {
					leftRight.Parent = node;
				}
				left.Right = node;
				ComputeMax(left);

				node.Parent = left;
				if (parent != null) {
					if (parent.Left == node) {
						parent.Left = left;
					} else {
						parent.Right = left;
					}
				}
				left.Parent = parent;
				return left;
			}				
		}
	}
}
