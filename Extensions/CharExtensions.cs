﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KS.Foundation
{
    public static class CharExtensions
    {
        public static char ToChar(this int CharCode)
        {
            return (char)CharCode;
        }

        public static int ToAsc(this char c)
        {
            return (int)(c);
        }
    }
}
