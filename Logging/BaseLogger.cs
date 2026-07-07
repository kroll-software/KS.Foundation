/*
{*******************************************************************}
{                                                                   }
{          KS-Foundation Library                                    }
{          Build rock solid DotNet applications                     }
{          on a threadsafe foundation without the hassle            }
{                                                                   }
{          Copyright (c) 2014 - 2018 by Kroll-Software,             }
{          Altdorf, Switzerland, All Rights Reserved                }
{          www.kroll-software.ch                                    }
{                                                                   }
{   Licensed under the MIT license                                  }
{   Please see LICENSE.txt for details                              }
{                                                                   }
{*******************************************************************}
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.Tracing;
//using Serilog;

namespace KS.Foundation
{
	public enum LogLevels
	{
		None,
		Verbose,
		Debug,
		Information,
		Warning,
		Error,
		Fatal
	}

	[Flags]
	public enum LogTargets
	{
		Off = 0,
		Console = 1,            
		File = 2,
		Events = 4,
		Online = 8
	}

	public interface ILogger : IDisposable
	{
		LogLevels LogLevel { get; set; }
		void Verbose(string message, params object[] args);
		void Debug(string message, params object[] args);
		void Information(string message, params object[] args);
		void Warning(string message, params object[] args);
		void Error(string message, params object[] args);
		void Fatal(string message, params object[] args);
	}

	public abstract class BaseLogger : DisposableObject, ILogger
	{			
		public LogLevels LogLevel { get; set; }
		protected abstract void OutputMessage(string s);

		[DebuggerNonUserCode]
		protected virtual string EnrichMessage(string message, LogLevels level)
		{
			return String.Format ("{0}\t[{1}]\t{2}", DateTime.UtcNow.FormatUtcServerString(), level.ToString(), message);
		}

		[DebuggerNonUserCode]
		public void Verbose(string message, params object[] args)
		{
			if (LogLevel > LogLevels.Verbose)
				return;

			// Logging is about logging errors.
			// it should work fail-safe and never produce new errors, indeed !
			try {
				if (args != null)
					message = String.Format (message, args);

				OutputMessage (EnrichMessage(message, LogLevels.Verbose));	
			} catch {}
		}

		[DebuggerNonUserCode]
		public void Debug(string message, params object[] args)
		{
			if (LogLevel > LogLevels.Debug)
				return;

			try {
				if (args != null)
					message = String.Format (message, args);

				OutputMessage (EnrichMessage(message, LogLevels.Debug));
			} catch {}
		}

		[DebuggerNonUserCode]
		public void Information(string message, params object[] args)
		{
			if (LogLevel > LogLevels.Information)
				return;

			try {
				if (args != null)
					message = String.Format (message, args);

				OutputMessage (EnrichMessage(message, LogLevels.Information));
			} catch {}
		}

		[DebuggerNonUserCode]
		public void Warning(string message, params object[] args)
		{
			if (LogLevel > LogLevels.Warning)
				return;

			try {
				if (args != null)
					message = String.Format (message, args);

				OutputMessage (EnrichMessage(message, LogLevels.Warning));
			} catch {}
		}

		[DebuggerNonUserCode]
		public void Error(string message, params object[] args)
		{
			if (LogLevel > LogLevels.Error)
				return;

			try {
				if (args != null)
					message = String.Format (message, args);

				OutputMessage (EnrichMessage(message, LogLevels.Error));
			} catch {}
		}

		[DebuggerNonUserCode]
		public void Fatal(string message, params object[] args)
		{
			if (LogLevel > LogLevels.Fatal)
				return;

			try {
				if (args != null)
					message = String.Format (message, args);

				OutputMessage (EnrichMessage(message, LogLevels.Fatal));
			} catch {}
		}
	}
}

