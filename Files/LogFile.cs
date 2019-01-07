using System;
using System.IO;

namespace KS.Foundation
{
	public class LogFile : DisposableObject
	{			
		public string PathName { get; private set; }
		public string FileName { get; private set; }
		public string FileNameTemplate { get; private set; }
		public System.Text.Encoding Encoding { get; private set; }

		public bool IsOpen { get; private set; }
		public bool ThrowExceptions { get; set; }

		public long Size { get; private set; }
		public long MaxSize { get; private set; }

		private StreamWriter File;
		//private Sync File;

		public const string DefaultFileNameTemplate = "{0:yyyy-MM-dd_hh-mm-ss-tt}.log";
		
		public LogFile(string fname, System.Text.Encoding encoding = null)
		{
			FileName = fname;
			IsOpen = false;
			Size = 0;
			MaxSize = 0;

			if (encoding == null)
				encoding = System.Text.Encoding.UTF8;
			Encoding = encoding;
		}

		public LogFile(string path, long maxSize, string filenameTemplate = null, System.Text.Encoding encoding = null)
		{
			PathName = Strings.BackSlash(path, true);

			if (String.IsNullOrEmpty (filenameTemplate))
				filenameTemplate = DefaultFileNameTemplate;
			FileNameTemplate = filenameTemplate;

			IsOpen = false;
			Size = 0;
			MaxSize = maxSize;
			if (encoding == null)
				encoding = System.Text.Encoding.UTF8;
			Encoding = encoding;
		}
		
		public bool KillLogfile()
		{
			CloseLogFile();

			if (String.IsNullOrEmpty(FileName))
				return false;			
			
			try
			{
				System.IO.File.Delete(FileName);
				return true;
			}
			catch (Exception ex)
			{
				if (ThrowExceptions)
					throw ex;
				else
					ex.LogError ();

				return false;
			}				
		}
			
		private int BreakCharLength;

		public bool OpenLogFile(bool bAppend)
		{
			CloseLogFile();

			if (String.IsNullOrEmpty (FileName)) {
				if (bAppend) {
					this.LogWarning ("LOG-FileName not set.");
					return false;
				}

				try {
					FileName = PathName + String.Format (FileNameTemplate, DateTime.Now);	
				} catch (Exception ex) {
					ex.LogError ();
				}

				if (String.IsNullOrEmpty (FileName)) {
					this.LogWarning ("LOG-FileName is null.");
					return false;
				}
			}

			try
			{
                File = new StreamWriter(FileName, bAppend, Encoding);
				BreakCharLength = File.NewLine.Length;
				File.AutoFlush = true;
				IsOpen = true;
				return true;
			}
			catch (Exception ex)
			{
				IsOpen = false;
				File = null;

				if (ThrowExceptions)
					throw ex;
				else
					ex.LogError ();

				return false;
			} finally {
				if (bAppend && IsOpen) {
					try {
						FileInfo fi = new FileInfo (FileName);
						Size = fi.Length;	
					} catch (Exception ex) {
						ex.LogError ();
						Size = 0;
					}
				}
			}
		}
		
		
		public void CloseLogFile ()
		{			
			if (!IsOpen || File == null) {
				IsOpen = false;
				Size = 0;
				return;
			}
			
			try
			{
				File.Flush();
			}
			catch (Exception ex)
			{
				if (ThrowExceptions)
					throw ex;
				else
					ex.LogError ();
			}

			try {
				File.Close();
			} catch {}

			try
			{								
				File.Dispose();
			}
			catch (Exception)
			{
			}
			finally {		
				File = null;
				Size = 0;
				IsOpen = false;
			}
		}

		public void AddLogLine()
		{
			AddLogLine (String.Empty);
		}

		public void AddLogLine(string S)
		{
			//if (IsDisposed || !IsOpen || S == null || S.Length == 0)
			if (IsDisposed || !IsOpen || S == null)
				return;

			try
			{
				if (MaxSize > 0 && Size + S.Length + BreakCharLength > MaxSize) {
					CloseLogFile();
					FileName = null;
					OpenLogFile(false);
					if (!IsOpen)
						return;
				}

				File.WriteLine(S);
				Size += S.Length + BreakCharLength;
			}
			catch (Exception ex)
			{
				if (ThrowExceptions)
					throw ex;
				else
					ex.LogError ();
			}
		}
			
		protected override void CleanupManagedResources ()
		{
			ThrowExceptions = false;
			CloseLogFile();
			base.CleanupManagedResources ();
		}

		/***
		protected override void CleanupUnmanagedResources ()
		{				
			// ToDo: OK ?
			ThrowExceptions = false;
			CloseLogFile();
			base.CleanupUnmanagedResources ();
		}
		***/
	}	
}
