using System;
using System.Linq.Expressions;

namespace KS.Foundation
{
	public static class FastConstructors
	{
		public static class New<T> where T : new()
		{
			public static readonly Func<T> Instance = Expression.Lambda<Func<T>>
				(
					Expression.New(typeof(T))
				).Compile();
		}

		/*** Usage
		MyType me = New<MyType>.Instance();
		***/
	}


}

