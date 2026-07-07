using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KS.Foundation
{
	public interface ISettingsProvider
	{
		object GetSetting (string section, string key, object defaultValue = null);
		void SetSetting (string section, string key, object value, object defaultValue = null);
	}

	public interface ISupportsSettings
	{	
		void LoadSettings (ISettingsProvider provider);
		void SaveSettings (ISettingsProvider provider);
	}

	public class ConfigFile : DisposableObject, ISettingsProvider
	{		
		public class ConfigFileEntry
		{			
			public string Key { get; set; }
			public object Value { get; set; }

			public ConfigFileEntry(string key, object value = null)
			{
				Key = key;
				Value = value;
			}

			public override string ToString ()
			{
				if (String.IsNullOrEmpty (Key))
					return Value.SafeString ();
				else 
					return String.Format ("{0} = {1}", Key, Value.ToInvariantString());
			}
		}

		public class ConfigFileSection
		{						
			public string Name  { get; private set; }
			public List<ConfigFileEntry> Entries  { get; private set; }
			public ConfigFileSection(string name = null)
			{
				Name = name;
				Entries = new List<ConfigFileEntry>();
			}

			public override string ToString ()
			{
				StringBuilder SB = new StringBuilder ();
				if (!String.IsNullOrEmpty (Name))
					SB.AppendLine (String.Format("[{0}]", Name));
				Entries.ForEach (entry => SB.AppendLine (entry.ToString()));
				return SB.ToString ();
			}
		}

		public List<ConfigFileSection> Sections { get; private set; }

		public string FileName { get; set; }

		public ConfigFile (string fname)
		{
			FileName = fname;
			Sections = new List<ConfigFileSection> ();
			OpenAndParse ();
		}			

		public bool Dirty { get; private set; }
		public string LastErrorDescription { get; private set; }
		public bool HadErrors
		{
			get{
				return !String.IsNullOrEmpty (LastErrorDescription);
			}
		}

		private void OpenAndParse()
		{
			if (!Strings.FileExists (FileName))
				return;

			string content = TextFile.LoadTextFile (FileName);
			if (String.IsNullOrEmpty (content))
				return;

			if (TextFile.TextFileLastError != 0) {
				LastErrorDescription = TextFile.TextFileLastErrorDescription;
				throw new Exception (TextFile.TextFileLastErrorDescription);
			}

			string[] Lines = Strings.SplitLines (content);
			List<string> sectionLines = new List<string> ();
			string sectionName = String.Empty;

			for (int i = 0; i < Lines.Length; i++) {
				string line = Lines [i].Trim();
				if (String.IsNullOrEmpty (line) || line [0] == ';' || line.StrLeft(2) == "//")
					continue;
			
				if (line [0] == '[') {
					// parse section
					ParseSection(sectionName, sectionLines);
					sectionLines.Clear ();
					sectionName = line.StrMid (2, line.Length - 2);

				} else {
					sectionLines.Add (line);
				}
			}

			ParseSection(sectionName, sectionLines);
		}

		private void ParseSection(string name, List<string> lines)
		{
			//if (String.IsNullOrEmpty (name) || lines.IsNullOrEmpty ())
			if (lines.IsNullOrEmpty ())
				return;

			ConfigFileSection sec = new ConfigFileSection(name);
			for (int k = 0; k < lines.Count; k++) {
				string entry = lines [k];
				int m = entry.IndexOf ('=');
				if (m > 0) {
					string key = entry.StrLeft (m).Trim();
					string value = entry.StrMid (m + 2).Trim();

					if (!String.IsNullOrEmpty(key))
						sec.Entries.Add (new ConfigFileEntry (key, value));
				}
			}
			Sections.Add (sec);
		}

		public bool ClearSection(string section)
		{
			lock (SyncObject) {
				var sect = Sections.FirstOrDefault (sec => sec.Name == section);
				if (sect != null && !sect.Entries.IsNullOrEmpty()) {
					sect.Entries.Clear ();
					Dirty = true;
					return true;
				}
				return false;
			}
		}

		public IEnumerable<KeyValuePair<string, object>> ReadSection(string section)
		{
			lock (SyncObject) {
				var sect = Sections.FirstOrDefault (sec => sec.Name == section);
				if (sect != null && !sect.Entries.IsNullOrEmpty()) {
					return sect.Entries.Select(e => new KeyValuePair<string, object>(e.Key, e.Value));
				}
				return Enumerable.Empty<KeyValuePair<string, object>>();
			}
		}

		public object GetSetting(string section, string key, object defaultValue = null) 
		{
			lock (SyncObject) {
				if (section == null)
					section = String.Empty;

				if (key == null)
					key = String.Empty;

				//ConfigFileSection sec = Sections.FirstOrDefault (s => s.UpperName == section.ToUpperInvariant());
				ConfigFileSection sec = Sections.FirstOrDefault (s => s.Name.Equals (section, StringComparison.InvariantCultureIgnoreCase));

				if (sec == null)
					return defaultValue;

				//ConfigFileEntry entry = sec.Entries.FirstOrDefault (e => e.UpperKey == key.ToUpperInvariant ());
				ConfigFileEntry entry = sec.Entries.FirstOrDefault (e => e.Key.Equals (key, StringComparison.InvariantCultureIgnoreCase));
				if (entry == null)
					return defaultValue;
			
				return entry.Value;
			}
		}

		public void SetSetting(string section, string key, object value, object defaultValue = null)
		{
			lock (SyncObject) {
				if (section == null)
					section = String.Empty;

				if (key == null)
					key = String.Empty;

				if (value == null)
					value = defaultValue;

				ConfigFileSection sec = Sections.FirstOrDefault (s => s.Name.Equals (section, StringComparison.InvariantCultureIgnoreCase));
				ConfigFileEntry entry = null;

				if (sec == null) {
					sec = new ConfigFileSection (section);
					Sections.Add (sec);
					Dirty = true;
				} else {
					entry = sec.Entries.FirstOrDefault (e => e.Key.Equals (key, StringComparison.InvariantCultureIgnoreCase));
				}
				
				if (entry == null) {
					entry = new ConfigFileEntry (key, value);
					sec.Entries.Add (entry);
					Dirty = true;
				} else {
					if (entry.Value.SafeString () != value.SafeString ()) {
						entry.Value = value;
						Dirty = true;
					}
				}				
			}
		}

		public void SaveAs(string fname)
		{
			lock (SyncObject) {
				Dirty |= FileName != fname;
				FileName = fname;
				Save ();
			}
		}

		public void Save()
		{
			lock (SyncObject) {
				if (!Dirty || HadErrors || String.IsNullOrEmpty(FileName))
					return;

				try {
					string backName = null;
					if (File.Exists (this.FileName)) {
						backName = Path.Combine (Strings.GetPathName (FileName),
							Path.GetFileNameWithoutExtension (FileName) + ".back");
						if (File.Exists (backName) && KillFile (backName))							
							File.Move (FileName, backName);
					}

					try {
						using (StreamWriter writer = new StreamWriter(FileName, false, Encoding.UTF8))
						{
							Sections.ForEach (sec => writer.WriteLine (sec.ToString ()));
							writer.Flush();
							writer.Close();
						}	
						Dirty = false;
					} catch (Exception ex) {
						ex.LogError();
						if (!String.IsNullOrEmpty(backName) && File.Exists(backName) && KillFile (FileName))
							File.Move (backName, FileName);
					}
				} catch (Exception ex) {
					ex.LogError ();
				}
			}
		}

		private bool KillFile(string fname)
		{
			try {
				if (File.Exists(fname))
					File.Delete (fname);
				return true;
			} catch (Exception ex) {
				ex.LogError ();
				return false;
			}
		}			

		protected override void CleanupManagedResources ()
		{
			Save ();
			base.CleanupManagedResources ();
		}			
	}
}

