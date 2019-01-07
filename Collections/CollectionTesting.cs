using System;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Linq;

namespace KS.Foundation.Testing
{
	public static class CollectionTesting
	{
		public static IEnumerable<int> NumberProducer(int count)
		{
			Random rnd = new Random (System.Environment.TickCount);
			int i = 0;
			while (i++ < count) {
				yield return rnd.Next (count);
			}
		}

		public static List<int> RandomList(int count)
		{
			IEnumerable<int> enu = NumberProducer (count);

			List<int> result = new List<int> ();

			foreach (int i in enu)
				result.Add (i);

			return result;
		}

		public static SortedSet<int> RandomSortedSet(int count)
		{
			Random rnd = new Random (System.Environment.TickCount);

			SortedSet<int> result = new SortedSet<int> ();

			int i = 0;
			while (i++ < count)
				result.Add (rnd.Next());

			return result;
		}

		public static LinkedList<int> RandomLinkedList(int count)
		{
			IEnumerable<int> enu = NumberProducer (count);

			LinkedList<int> result = new LinkedList<int> ();

			foreach (int i in enu)
				result.AddLast (i);

			return result;
		}

		public static BinarySortedList<int> RandomBinarySortedList(int count)
		{
			IEnumerable<int> enu = NumberProducer (count);

			BinarySortedList<int> result = new BinarySortedList<int> ();

			foreach (int i in enu)
				result.Add (i);
			//result.AddUnsorted (i);

			//result.NaturalMergeSort ();

			return result;
		}

		public static void CheckSorting(IEnumerable<int> source, int n)
		{						
			int count = 0;
			int errCount = 0;
			PerformanceTimer.Time (() => {								
				int prev = -1;
				foreach (int value in source) {
					count++;
					if (value < prev)
						errCount++;					
					prev = value;
				}
			}, 1, "Iteraing items");

			if (count != n)
				Console.WriteLine ("Count mismatch: count={0}", count);

			if (errCount == 0)
				Console.WriteLine ("Sorting OK");
			else {
				Console.WriteLine ("{0} Sorting ERRORS !!!", errCount);
				string values = "{" + String.Join (",", source) + "}";
				Console.WriteLine(values);
			}
		}

		public static ClassicLinkedList<int> RandomFoundationLinkedList(int count, bool sorted)
		{
			Random rnd = new Random (System.Environment.TickCount);

			IComparer<int> comparer = null;
			if (sorted)
				comparer = Comparer<int>.Default;
			ClassicLinkedList<int> result = new ClassicLinkedList<int> (comparer);

			int i = 0;
			while (i++ < count) {				
				//int val = rnd.Next (1000);
				int val = rnd.Next (count);
				//Console.WriteLine (val);

				result.AddSorted (val, true);
				//result.AddLast (val);

				//result.CheckForSortingErrors ();
				/**
				if (result.CheckForSortingErrors () > 0)
					break;
					**/
			}

			//result.Sort ();

			return result;
		}

		public static ClassicLinkedList<int> SequentialLinkedList(int count)
		{
			ClassicLinkedList<int> result = new ClassicLinkedList<int> ();

			int i = 0;
			while (i++ < count)
				result.AddSorted (i, true);

			return result;
		}
	}
}

