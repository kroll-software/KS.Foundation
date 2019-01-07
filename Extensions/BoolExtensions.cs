using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KS.Foundation
{
    public static class BoolExtensions
    {
        public static int ToInt(this bool b)
        {
            if (b)
                return 1;
            else
                return 0;
        }

		public static string ToLowerString(this bool b)
		{
			if (b)
				return "true";
			else
				return "false";
		}

		public static char ToUpperChar(this bool b)
		{
			if (b)
				return 'T';
			else
				return 'F';
		}
    }
}
