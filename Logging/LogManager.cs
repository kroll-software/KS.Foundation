using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;
using System.Reflection;

namespace KS.Foundation
{		
	public class LogManager : DisposableObject, IObservable<LogMessage>
	{
		public Lazy<Observable<LogMessage>> Observable { get; private set; }
		[DebuggerNonUserCode]
		public IDisposable Subscribe(IObserver<LogMessage> observer)
		{
			return Observable.Value.Subscribe (observer);
		}

		[DebuggerNonUserCode]
		public void Unsubscribe(IObserver<LogMessage> observer)
		{
			Observable.Value.Unsubscribe (observer);
		}

		/***
		[DebuggerNonUserCode]
		static string[] ToStringArray(object arg) {
			var collection = arg as System.Collections.IEnumerable;
			if (collection != null) {
				return collection
					.Cast<object>()
					.Select(x => x.ToString())
					.ToArray();
			}

			if (arg == null) {
				return new string[] { };
			}

			return new string[] { arg.ToString() };
		}
		***/

		[DebuggerNonUserCode]
		protected void SendLogMessage(LogLevels level, string text, params object[] args)
		{
			try {
				if (IsDisposed || !Observable.IsValueCreated)
					return;
				//Observable.Value.SendMessage (new LogMessage (level, text, DateTime.UtcNow, ToStringArray(args)));	
				Observable.Value.SendMessage (new LogMessage (level, text, DateTime.UtcNow, args));
			} catch (Exception) {
				//System.Diagnostics.Debug.Assert (false);
			}
		}

		[DebuggerNonUserCode]
		protected void SendLogMessage(LogMessage message)
		{
			try {
				if (IsDisposed || !Observable.IsValueCreated || String.IsNullOrEmpty(message.Text))
					return;
				Observable.Value.SendMessage (message);	
			} catch (Exception) {
				//System.Diagnostics.Debug.Assert (false);
			}
		}

		public Dictionary<Type, ILogger> Loggers { get; private set; }

		public ConsoleLogger ConsoleLogger
		{
			get{
				ILogger log;
				if (Loggers.TryGetValue (typeof(ConsoleLogger), out log))
					return log as ConsoleLogger;
				return null;
			}
		}

		public EventLogger EventLogger
		{
			get{
				ILogger log;
				if (Loggers.TryGetValue (typeof(EventLogger), out log))
					return log as EventLogger;
				return null;
			}
		}			

		public FileLogger FileLogger
		{
			get{
				ILogger log;
				if (Loggers.TryGetValue (typeof(FileLogger), out log))
					return log as FileLogger;
				return null;
			}
		}

		[DebuggerNonUserCode]
		public int Count
		{
			get{
				return Loggers.Count;
			}
		}

		[DebuggerNonUserCode]
		public LogManager()
		{
			Loggers = new Dictionary<Type, ILogger> ();
			Observable = new Lazy<Observable<LogMessage>> ();
		}

		[DebuggerNonUserCode]
		public void RegisterLogger(ILogger logger)
		{
			if (logger == null)
				return;
			Loggers.Add (logger.GetType(), logger);
		}

		[DebuggerNonUserCode]
		public void UnregisterLogger(Type type)
		{
			if (type == null)
				return;

			ILogger logger;
			if (Loggers.TryGetValue (type, out logger) && (logger as IDisposable) != null)
				(logger as IDisposable).Dispose ();
			
			Loggers.Remove (type);
		}

		[DebuggerNonUserCode]
		public void Clear()
		{
			Loggers.Values.DisposeListObjects ();
			Loggers.Clear();
		}

		[DebuggerNonUserCode]
		public void LogMessage(LogMessage message)
		{
			if (String.IsNullOrEmpty(message.Text))
				return;
			
			switch (message.Level) {
			case LogLevels.Verbose:
				Loggers.Values.ForEach (l => l.Verbose(message.Text, message.Args));
				break;
			case LogLevels.Debug:
				Loggers.Values.ForEach (l => l.Debug(message.Text, message.Args));
				break;
			case LogLevels.Information:
				Loggers.Values.ForEach (l => l.Information(message.Text, message.Args));
				break;
			case LogLevels.Error:
				Loggers.Values.ForEach (l => l.Error(message.Text, message.Args));
				break;
			case LogLevels.Fatal:
				Loggers.Values.ForEach (l => l.Fatal(message.Text, message.Args));
				break;
			case LogLevels.Warning:
				Loggers.Values.ForEach (l => l.Warning(message.Text, message.Args));
				break;
			case LogLevels.None:
				return;			
			}

			SendLogMessage (message);
		}

		[DebuggerNonUserCode]
		public void Verbose(string message, params object[] args)
		{
			Loggers.Values.ForEach (l => l.Verbose(message, args));
			SendLogMessage (LogLevels.Verbose, message, args);
		}

		[DebuggerNonUserCode]
		public void Debug(string message, params object[] args)
		{
			Loggers.Values.ForEach (l => l.Debug(message, args));
			SendLogMessage (LogLevels.Debug, message, args);
		}

		[DebuggerNonUserCode]
		public void Information(string message, params object[] args)
		{
			Loggers.Values.ForEach (l => l.Information(message, args));
			SendLogMessage (LogLevels.Information, message, args);
		}

		[DebuggerNonUserCode]
		public void Warning(string message, params object[] args)
		{
			Loggers.Values.ForEach (l => l.Warning(message, args));
			SendLogMessage (LogLevels.Warning, message, args);
		}

		[DebuggerNonUserCode]
		public void Error(string message, params object[] args)
		{
			Loggers.Values.ForEach (l => l.Error(message, args));
			SendLogMessage (LogLevels.Error, message, args);
		}

		[DebuggerNonUserCode]
		public void Fatal(string message, params object[] args)
		{
			Loggers.Values.ForEach (l => l.Fatal(message, args));
			SendLogMessage (LogLevels.Fatal, message, args);
		}
			
		protected override void CleanupManagedResources ()
		{
			if (Observable.IsValueCreated)
				Observable.Value.Dispose ();

			Clear ();
			base.CleanupManagedResources ();
		}
	}
}

