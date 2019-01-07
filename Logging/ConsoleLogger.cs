using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace KS.Foundation
{
	public class ConsoleLogger : BaseLogger
	{
		public ConsoleLogger()
		{
			LogLevel = LogLevels.Debug;
		}

		public ConsoleLogger(LogLevels level)
		{
			LogLevel = level;
		}

		[DebuggerNonUserCode]
		protected override void OutputMessage(string s)
		{
			Console.WriteLine (s);
		}
	}
}

