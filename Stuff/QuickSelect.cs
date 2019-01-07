using System;
using System.Collections.Generic;

namespace KS.Foundation
{
	public class QuickSelect<T> 
	{
		private T[] array;
		private IComparer<T> comparer;

		public int Select (T[] items, int n, int count = 0, IComparer<T> comp = null) 
		{
			if (items.IsNullOrEmpty ())
				return -1;

			array = items;
			if (comp == null)
				comparer = Comparer<T>.Default;
			else
				comparer = comp;

			if (count == 0)
				count = items.Length;

			return RecursiveSelect(0, count - 1, n);
		}

		private int Partition (int left, int right, int pivot) 
		{
			T pivotValue = array[pivot];
			Swap(right, pivot);
			int storage = left;
			for (int i = left; i < right; i++) {
				if (comparer.Compare(array[i], pivotValue) < 0) {
					Swap(storage, i);
					storage++;
				}
			}
			Swap(right, storage);
			return storage;
		}

		private int RecursiveSelect (int left, int right, int k) 
		{
			if (left == right) 
				return left;
			
			int pivotIndex = MedianOfThreePivot(left, right);
			int pivotNewIndex = Partition(left, right, pivotIndex);
			int pivotDist = (pivotNewIndex - left) + 1;
			int result;
			if (pivotDist == k) {
				result = pivotNewIndex;
			} else if (k < pivotDist) {
				result = RecursiveSelect(left, pivotNewIndex - 1, k);
			} else {
				result = RecursiveSelect(pivotNewIndex + 1, right, k - pivotDist);
			}
			return result;
		}
			
		private int MedianOfThreePivot (int leftIdx, int rightIdx) 
		{
			T left = array[leftIdx];
			int midIdx = (leftIdx + rightIdx) / 2;
			T mid = array[midIdx];
			T right = array[rightIdx];

			if (comparer.Compare(left, mid) > 0) {
				if (comparer.Compare(mid, right) > 0) {
					return midIdx;
				} else if (comparer.Compare(left, right) > 0) {
					return rightIdx;
				} else {
					return leftIdx;
				}
			} else {
				if (comparer.Compare(left, right) > 0) {
					return leftIdx;
				} else if (comparer.Compare(mid, right) > 0) {
					return rightIdx;
				} else {
					return midIdx;
				}
			}
		}

		private void Swap (int left, int right) 
		{
			T tmp = array[left];
			array[left] = array[right];
			array[right] = tmp;
		}
	}
}

