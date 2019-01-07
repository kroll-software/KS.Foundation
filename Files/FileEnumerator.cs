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
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace KS.Foundation
{

	/***
	string[] mp3files = (from f in Directory.GetFiles(@"C:\DirName", "*", SearchOption.AllDirectories)
		where f.EndsWith(".mp3")
		select f).ToArray();
	***/

	public class FilenameComparer : IComparer<String>
	{
		public int Compare(string f1, string f2)
		{
			if (f1 == null || f2 == null)
				return 0;
			//return Key.CompareTo (other.Key);
			int val = String.Compare (f1, f2, true, System.Globalization.CultureInfo.InvariantCulture);
			if (val == 0)
				return String.Compare (f1, f2, false, System.Globalization.CultureInfo.InvariantCulture);
			return val;
		}
	}

    public class FileEnumerator : DisposableObject
    {
        public delegate void FileFoundEventHandler(string FileName);
        public event FileFoundEventHandler FileFound;
		public void OnFileFound(string FileName)
		{
			try {
				if (FileFound != null)
					FileFound (FileName);	
			} catch (Exception ex) {
				ex.LogError ();
			}
		}

        public delegate void DirFoundEventHandler(string PathName);
        public event DirFoundEventHandler DirFound;
		public void OnDirFound(string PathName)
		{
			try {
				if (DirFound != null)
					DirFound (PathName);	
			} catch (Exception ex) {
				ex.LogError ();
			}
		}
        
		public string DrivePath  { get; set; }

		private string m_FileMask = "";
        private string[] FileMaskArray = null;
        public string FileMask
        {
            get
            {
                return m_FileMask;
            }
            set
            {
                m_FileMask = value;
				if (String.IsNullOrEmpty(value))
					FileMaskArray = null;
                else
                {
					List<string> list = new List<string> ();
					string[] a = m_FileMask.Split(new char[]{';'}, StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < a.Length; i++) {
						string ep = a [i].Trim ();
						if (!String.IsNullOrEmpty (ep))
							list.Add (ep);
					}
					FileMaskArray = list.ToArray ();
                }
            }
        }

		private string m_ExclusionMask = "";
        private string[] ExclusionMaskArray = null;
        public string ExclusionMask
        {
            get
            {
                return m_ExclusionMask;
            }
            set
            {
                m_ExclusionMask = value;
				if (String.IsNullOrEmpty(value))
                    ExclusionMaskArray = null;
                else
                {
					List<string> list = new List<string> ();
                    string[] a = m_ExclusionMask.Split(new char[]{';'}, StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < a.Length; i++) {						
						string ep = a[i].Trim ();
						if (!String.IsNullOrEmpty (ep)) {
							if (ep.IndexOf ('*') < 0)
								ep = "*" + ep + "*";
							list.Add (ep);
						}
					}
					ExclusionMaskArray = list.ToArray ();
                }
            }
        }

		public long MinSize { get; set; }
		public long MaxSize { get; set; }

		public bool Recursive { get; set; }
        
		public List<String> FileList { get; private set; }
		public BlockingQueue<string> Queue { get; private set; }

		public int TotalDirsChecked { get; private set; }
		public int TotalFilesChecked { get; private set; }
		public int Count { get; private set; }

        private bool bCancel = false;

		public FileEnumerator(string drivePath, bool includeSubFolders)
        {
            bCancel = false;
            FileList = new List<string>();
			AddFileAction = AddFileToList;

			DrivePath = drivePath;
			Recursive = includeSubFolders;
			FileMask = "*.*";
        }			

        public void Sort()
        {
            if (FileList != null)
                //FileList.Sort(StringComparer.InvariantCultureIgnoreCase);
				FileList.Sort(new FilenameComparer());
        }

        private void ClearFileList()
        {
            if (FileList != null)
                FileList.Clear();
			Count = 0;
        }

        public long FileSize(string FName)
        {
            try
            {
                FileInfo fi = new FileInfo(FName);
                return fi.Length;
            }
            catch (Exception)
            {
                return 0;
            }            
        }
    
        public DateTime LastModified(string FName)
        {
            try
            {
                FileInfo fi = new FileInfo(FName);
                return fi.LastWriteTime;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }            
        }        
			
		public bool CheckSize (string path)
		{
			if (MinSize <= 0 && MaxSize <= 0)
				return true;			
			long sz = FileSize (path);
			if (MinSize > 0 && sz < MinSize)
				return false;
			if (MaxSize > 0 && sz > MaxSize)
				return false;
			return true;
		}			

		Action<string> AddFileAction = null;

		private void AddFileToList(string path)
		{
			FileList.Add (path);
		}

		private void AddFileToQueue(string path)
		{
			Queue.Enqueue (path);
		}

		public void ScanToQueue(int qsize = 1024)
		{
			Queue = new BlockingQueue<string> (qsize);
			AddFileAction = AddFileToQueue;
			Scan ();
			Queue.Quit ();
		}

		public void ScanToList()
		{			
			AddFileAction = AddFileToList;
			Scan ();
		}

        public void Scan()
        {
            bCancel = false;
            ClearFileList();
			TotalDirsChecked = 0;
			TotalFilesChecked = 0;
			Count = 0;

			if (!String.IsNullOrEmpty(DrivePath) && FileMaskArray != null)
            {
				string[] paths = DrivePath.Split (new char[]{ ';' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string path in paths) {
					if (!String.IsNullOrEmpty(path))
						FilesSearch (DrivePath);
				}
            }
        }

        private bool IsExclusion(string Path)
        {
            if (ExclusionMaskArray == null)
                return false;            

			foreach (string ep in ExclusionMaskArray)
			{
				if (Path.StrLike(ep))
					return true;
			}

            return false;
        }

		private void AddFile(string path)
		{
			if (bCancel)                               
				return;

			TotalFilesChecked++;

			if (IsExclusion (path) || !CheckSize(path))
				return;

			foreach (string fm in FileMaskArray) {
				if (path.StrLike (fm)) {					
					AddFileAction (path);
					Count++;
					OnFileFound (path);
					return;
				}
			}
		}

		public bool IsSymbolicLink(string path)
		{
			FileInfo pathInfo = new FileInfo(path);
			return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
		}

		private void AddDirectory(string drivePath)
		{		
			if (bCancel)                               
				return;

			TotalDirsChecked++;

			if (IsSymbolicLink (drivePath))
				return;

			// Directory
			drivePath = drivePath.BackSlash(true);
			if (IsExclusion(drivePath))
				return;

			OnDirFound(drivePath);

			string[] FFound;

			try
			{
				FFound = Directory.GetFiles(drivePath, "*.*", SearchOption.TopDirectoryOnly);
			}
			catch (Exception ex)
			{                
				ex.LogError ();
				FFound = new string[0];
			}

			foreach (string f in FFound)
			{
				AddFile(f);
				if (bCancel)
					return;
			}

			if (Recursive) {
				try
				{
					FFound = Directory.GetDirectories(drivePath);
				}
				catch (Exception ex)
				{
					ex.LogError ();
					FFound = new string[0];
				}

				foreach (string f in FFound) {
					AddDirectory(f);
					if (bCancel)
						return;
				}                
			}
		}

		private void FilesSearch(string drivePath)
		{            			
			if (bCancel)                               
				return;

			try
			{
				FileInfo fi = new FileInfo(drivePath);
				if (fi.Attributes.HasFlag(FileAttributes.Directory))
					AddDirectory(drivePath);
				else					
					AddFile(drivePath);				
			}
			catch (Exception ex)
			{
				ex.LogError ();          
			}
		}			

        public void Cancel()
        {
            bCancel = true;
			if (Queue != null) {				
				Queue.Quit ();
				Queue.Clear ();
			}
        }

		protected override void CleanupManagedResources ()
		{
			Cancel ();
			ClearFileList ();			
			base.CleanupManagedResources ();
		}
    }
}
