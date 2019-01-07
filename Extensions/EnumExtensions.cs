using System;

namespace KS.Foundation
{
	public static class EnumExtensions
	{
		/***	Can't be compiled without Microsoft.CSharp, sorry
		public static T SetFlag<T>(this Enum value, T flag, bool set)
		{
			Type underlyingType = Enum.GetUnderlyingType(value.GetType());

			// note: AsInt mean: math integer vs enum (not the c# int type)
			dynamic valueAsInt = Convert.ChangeType(value, underlyingType);
			dynamic flagAsInt = Convert.ChangeType(flag, underlyingType);
			if (set)
			{
				valueAsInt |= flagAsInt;
			}
			else
			{
				valueAsInt &= ~flagAsInt;
			}

			return (T)valueAsInt;
		}
		***/
	}
}

