using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace KS.Foundation
{
	public class FileLogger : BaseLogger
	{		
		readonly LogFile m_LogFile;

		public string LogFileName 
		{
			get{
				return m_LogFile.FileName;
			}
		}
			
		public static string DefaultFileNameTemplate = "PPP-{0:u}.log";

		[DebuggerNonUserCode]
		public FileLogger(LogLevels level, string filepath, long maxSize = 640 * 1024)
		{
			LogLevel = level;

			try {
				m_LogFile = new LogFile (filepath, maxSize, DefaultFileNameTemplate);
				m_LogFile.OpenLogFile (false);	
			} catch (Exception ex) {
				Console.WriteLine (ex.Message);
			}
		}			

		[DebuggerNonUserCode]
		protected override void OutputMessage(string s)
		{			
			try {
				if (m_LogFile.IsOpen)
					m_LogFile.AddLogLine (s);	
			} catch (Exception) {				
			}
		}

		[DebuggerNonUserCode]
		protected override void CleanupManagedResources ()
		{
			try {
				if (m_LogFile != null)
					m_LogFile.CloseLogFile ();	
			} catch (Exception) {				
			}
			base.CleanupManagedResources ();
		}
	}
}

