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
    public static class Logging
    {				
		public static LogManager Manager
		{
			get{
				return Singleton<LogManager>.Instance;
			}
		}

		[DebuggerNonUserCode]
		public static void RegisterLogger(ILogger logger)
		{
			if (logger != null)
				Manager.RegisterLogger (logger);
		}

		[DebuggerNonUserCode]
		public static void UnregisterLogger(ILogger logger)
		{
			if (logger != null)
				Manager.UnregisterLogger (logger.GetType());
		}

		[DebuggerNonUserCode]
        public static string ProgramVersion { get; private set; }        
		        
        //private static string RandomGuid = System.Guid.NewGuid().ToString();

		public static void SetupLogging()
		{
			SetupLogging (LogLevels.Debug, LogTargets.Console, null);
		}

		public static void SetupLogging(LogLevels logLevel, LogTargets logTarget, string logPath = "")
		{
			try {
				Manager.Clear ();

				if (logTarget.HasFlag (LogTargets.Console))
					Manager.RegisterLogger (new ConsoleLogger (logLevel));
				if (logTarget.HasFlag (LogTargets.Events))
					Manager.RegisterLogger (new EventLogger (logLevel));
				if (logTarget.HasFlag (LogTargets.File))
					Manager.RegisterLogger (new FileLogger (logLevel, logPath));
				
				ProgramVersion = Assembly.GetExecutingAssembly ().GetName ().Version.ToString ();	
			} catch (Exception ex) {
				ex.LogError ();
			}
		}


        // ************** Log Extensions for Exception Free Logging ******************

		[DebuggerNonUserCode]
        static bool CanLog(object obj)
        {
			return obj != null && Manager.Count > 0;
        }

		[DebuggerNonUserCode]
        public static void LogStack(this object obj, params object[] variables)
        {
			if (!CanLog(obj))
                return;

            StackTrace stackTrace = new StackTrace();           // get call stack
            if (stackTrace.FrameCount < 2)
                return;

            List<object> values = new List<object>();
            //Type type = obj.GetType();

            string MessageTemplate = "{Typename}.{Method}(";
            values.Add(obj.GetType().Name);
            values.Add(stackTrace.GetFrame(1).GetMethod().Name);

            if (variables != null)
            {
                int i = 0;
                foreach (object v in variables)
                {
                    if (i > 0)
                        MessageTemplate += ", ";

                    i++;
                    MessageTemplate += "{p" + i + "}";

                    if (v is string)
                        values.Add("\"" + v + "\"");
                    else
                        values.Add(v);
                }
            }

            MessageTemplate += ")";

			Manager.Debug (MessageTemplate, values.ToArray());
        }

		[DebuggerNonUserCode]
        public static void LogInformation(this object obj, string MessageTemplate, params object[] propertyValues)
        {
			if (!CanLog(obj))
                return;
			Manager.Information (MessageTemplate, propertyValues);            
        }

		[DebuggerNonUserCode]
        public static void LogVerbose(this object obj, string MessageTemplate, params object[] propertyValues)
        {
			if (!CanLog(obj))
                return;
			Manager.Verbose (MessageTemplate, propertyValues);            
        }

		[DebuggerNonUserCode]
        public static void LogWarning(this object obj, string MessageTemplate, params object[] propertyValues)
        {
			if (!CanLog(obj))
                return;
			Manager.Warning (MessageTemplate, propertyValues);        
        }

		[DebuggerNonUserCode]
        public static void LogError(this object obj, string MessageTemplate, params object[] propertyValues)
        {
			if (!CanLog(obj))
                return;
			Manager.Error (MessageTemplate, propertyValues);            
        }

		[DebuggerNonUserCode]
        public static void LogDebug(this object obj, string MessageTemplate, params object[] propertyValues)
        {
			if (!CanLog(obj))
                return;
			Manager.Debug (MessageTemplate, propertyValues);            
        }

		[DebuggerNonUserCode]
        public static void LogFatal(this object obj, string MessageTemplate, params object[] propertyValues)
        {
			if (!CanLog(obj))
                return;
			Manager.Fatal (MessageTemplate, propertyValues);
        }

		[DebuggerNonUserCode]
		public static void LogWarning(this Exception ex, string ExceptionType = "Inline")
		{   
			if (!CanLog(ex))
				return;

			if (ex is OperationCanceledException)
				return;

			while (ex.InnerException != null)
				ex = ex.InnerException;

			Debug.WriteLine(ex.Message);

			//Manager.Warning ("Error: {Error}, ExceptionType: {ExceptionType}, Version: {Version}", ex.Message, ExceptionType, ProgramVersion);
			Manager.Warning ("Error: {0}, ExceptionType: {1}, Version: {2}, Stack: {3}", ex.Message, ExceptionType, ProgramVersion, Concurrency.GetStackTrace());
		}

		[DebuggerNonUserCode]
		public static void LogError(this Exception ex, string ExceptionType = "Inline")
		{   
			if (!CanLog(ex))
				return;

			if (ex is OperationCanceledException)
				return;

			while (ex.InnerException != null)
				ex = ex.InnerException;

			Debug.WriteLine(ex.Message);

			//Manager.Error ("Error: {Error}, ExceptionType: {ExceptionType}, Version: {Version}, Stack: {Stack}", ex.Message, ExceptionType, ProgramVersion, Concurrency.GetStackTrace());
			Manager.Error ("Error: {0}, ExceptionType: {1}, Version: {2}, Stack: {3}", ex.Message, ExceptionType, ProgramVersion, Concurrency.GetStackTrace());
		}

		[DebuggerNonUserCode]
		public static void LogFatal(this Exception ex, string ExceptionType = "Inline")
        {			
            if (!CanLog(ex))
                return;

            if (ex is OperationCanceledException)
                return;

            while (ex.InnerException != null)
                ex = ex.InnerException;

			System.Diagnostics.Debug.WriteLine(ex.Message);

			//Manager.Fatal ("Error: {Error}, ExceptionType: {ExceptionType}, Version: {Version}, Stack: {Stack}", ex.Message, ExceptionType, ProgramVersion, Concurrency.GetStackTrace());
			Manager.Fatal ("Error: {0}, ExceptionType: {1}, Version: {2}, Stack: {3}", ex.Message, ExceptionType, ProgramVersion, Concurrency.GetStackTrace());
        }
    }
}
