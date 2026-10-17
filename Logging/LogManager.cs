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
				ILogger log = null;
				lock (SyncObject) { Loggers.TryGetValue (typeof(ConsoleLogger), out log); }
				return log as ConsoleLogger;
			}
		}

		public EventLogger EventLogger
		{
			get{
				ILogger log = null;
				lock (SyncObject) { Loggers.TryGetValue (typeof(EventLogger), out log); }
				return log as EventLogger;
			}
		}

		public FileLogger FileLogger
		{
			get{
				ILogger log = null;
				lock (SyncObject) { Loggers.TryGetValue (typeof(FileLogger), out log); }
				return log as FileLogger;
			}
		}

		[DebuggerNonUserCode]
		public int Count
		{
			get{
				lock (SyncObject) { return Loggers.Count; }
			}
		}

		[DebuggerNonUserCode]
		public LogManager()
		{
			Loggers = new Dictionary<Type, ILogger> ();

			// ExecutionAndPublication: Bei parallelem Erstzugriff darf nur EIN Observable-
			//  Instanz erzeugt werden, sonst verteilen sich Abonnenten und Absender
			//  auf verschiedene Instanzen und Log-Nachrichten gehen still verloren.
			Observable = new Lazy<Observable<LogMessage>> (() => new Observable<LogMessage> (), LazyThreadSafetyMode.ExecutionAndPublication);
		}

		[DebuggerNonUserCode]
		public void RegisterLogger(ILogger logger)
		{
			if (logger == null)
				return;

			lock (SyncObject) {
				Loggers.Add (logger.GetType(), logger);
			}
		}

		[DebuggerNonUserCode]
		public void UnregisterLogger(Type type)
		{
			if (type == null)
				return;

			ILogger logger = null;
			lock (SyncObject) {
				if (!Loggers.TryGetValue (type, out logger)) {
					Loggers.Remove (type);
					return;
				}
				Loggers.Remove (type);
			}

			// Außerhalb des Locks freigeben: Das Logger-Dispose darf selbst wieder loggen.
			(logger as IDisposable)?.Dispose ();
		}

		[DebuggerNonUserCode]
		public void Clear()
		{
			// Snapshot unter Lock, Freigabe AUFERHALB (Logger-Dispose darf selbst loggen).
			ILogger[] snapshot;
			lock (SyncObject) {
				snapshot = Loggers.Values.ToArray ();
				Loggers.Clear ();
			}

			snapshot.DisposeListObjects ();
		}

		[DebuggerNonUserCode]
		public void LogMessage(LogMessage message)
		{
			if (String.IsNullOrEmpty(message.Text))
				return;
		
			// Snapshot unter Lock: Logger können aus jedem Thread registriert/entfernt werden.
			ILogger[] snapshot;
			lock (SyncObject) { snapshot = Loggers.Values.ToArray (); }

			switch (message.Level) {
			case LogLevels.Verbose:
				snapshot.ForEach (l =>l.Verbose(message.Text, message.Args));
				break;
			case LogLevels.Debug:
				snapshot.ForEach (l =>l.Debug(message.Text, message.Args));
				break;
			case LogLevels.Information:
				snapshot.ForEach (l =>l.Information(message.Text, message.Args));
				break;
			case LogLevels.Error:
				snapshot.ForEach (l =>l.Error(message.Text, message.Args));
				break;
			case LogLevels.Fatal:
				snapshot.ForEach (l =>l.Fatal(message.Text, message.Args));
				break;
			case LogLevels.Warning:
				snapshot.ForEach (l =>l.Warning(message.Text, message.Args));
				break;
			case LogLevels.None:
				return;			
			}

			SendLogMessage (message);
		}

		[DebuggerNonUserCode]
		public void Verbose(string message, params object[] args)
		{
			// Snapshot unter Lock: Logger können aus jedem Thread registriert/entfernt werden.
			ILogger[] snapshot;
			lock (SyncObject) { snapshot = Loggers.Values.ToArray (); }
			snapshot.ForEach (l => l.Verbose(message, args));
			SendLogMessage (LogLevels.Verbose, message, args);
		}

		[DebuggerNonUserCode]
		public void Debug(string message, params object[] args)
		{
			ILogger[] snapshot;
			lock (SyncObject) { snapshot = Loggers.Values.ToArray (); }
			snapshot.ForEach (l => l.Debug(message, args));
			SendLogMessage (LogLevels.Debug, message, args);
		}

		[DebuggerNonUserCode]
		public void Information(string message, params object[] args)
		{
			ILogger[] snapshot;
			lock (SyncObject) { snapshot = Loggers.Values.ToArray (); }
			snapshot.ForEach (l => l.Information(message, args));
			SendLogMessage (LogLevels.Information, message, args);
		}

		[DebuggerNonUserCode]
		public void Warning(string message, params object[] args)
		{
			ILogger[] snapshot;
			lock (SyncObject) { snapshot = Loggers.Values.ToArray (); }
			snapshot.ForEach (l => l.Warning(message, args));
			SendLogMessage (LogLevels.Warning, message, args);
		}

		[DebuggerNonUserCode]
		public void Error(string message, params object[] args)
		{
			ILogger[] snapshot;
			lock (SyncObject) { snapshot = Loggers.Values.ToArray (); }
			snapshot.ForEach (l => l.Error(message, args));
			SendLogMessage (LogLevels.Error, message, args);
		}

		[DebuggerNonUserCode]
		public void Fatal(string message, params object[] args)
		{
			ILogger[] snapshot;
			lock (SyncObject) { snapshot = Loggers.Values.ToArray (); }
			snapshot.ForEach (l => l.Fatal(message, args));
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

