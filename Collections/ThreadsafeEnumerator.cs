using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Threading;

namespace KS.Foundation
{
	public struct ThreadsafeEnumerator<T> : IEnumerator<T>
	{
		public T Current
		{
			get { return _current; }
		}

		/*** with a count property, it still gets errors..
		public int Count
		{
			get{
				return _size;
			}
		}
		***/

		public ThreadsafeEnumerator(T[] buffer, int size = 0)
		{			
			if (size <= 0)
				_size = buffer.Length;
			else
				_size = size;
			
			_counter = 0;
			_buffer = buffer;
			_current = default(T);
		}

		object IEnumerator.Current
		{
			get { return _current; }
		}

		T IEnumerator<T>.Current
		{
			get { return _current; }
		}

		public void Dispose()
		{
			_buffer = null;
			GC.SuppressFinalize (this);
		}

		public bool MoveNext()
		{
			if (_counter < _size)
			{
				_current = _buffer[_counter++];

				return true;
			}

			_current = default(T);

			return false;
		}

		public void Reset()
		{
			_counter = 0;
		}

		bool IEnumerator.MoveNext()
		{
			return MoveNext();
		}

		void IEnumerator.Reset()
		{
			Reset();
		}

		T[] _buffer;
		int _counter;
		int _size;
		T _current;
	}
}

