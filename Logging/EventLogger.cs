using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace KS.Foundation
{
	public class EventLoggerEventArgs : EventArgs
	{
		public string Message { get; private set; }
		public EventLoggerEventArgs(string message)
		{
			Message = message;
		}			
	}

	public class EventLogger : BaseLogger
	{
		public event EventHandler<EventLoggerEventArgs> LogEvent;
		public void OnLogEvent(string message)
		{
			if (LogEvent != null)
				LogEvent (this, new EventLoggerEventArgs (message));
		}

		public EventLogger()
		{
			LogLevel = LogLevels.Debug;
		}

		public EventLogger(LogLevels level)
		{
			LogLevel = level;
		}

		[DebuggerNonUserCode]
		protected override void OutputMessage(string s)
		{
			OnLogEvent (s);
		}
	}
}

