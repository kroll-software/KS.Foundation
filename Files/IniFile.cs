using System.Runtime.InteropServices;

namespace KS.Foundation
{
	public class IniFile
	{
        private const int C_BufSize = 65535;
		
		#region "API Calls"
		// standard API declarations for INI access
		// changing only "As Long" to "As Int32" (As Integer would work also)
		[DllImport("kernel32",EntryPoint="WritePrivateProfileStringW", ExactSpelling=true, CharSet=CharSet.Unicode, SetLastError=true)]
		private static extern int WritePrivateProfileString(string lpApplicationName, string lpKeyName, string lpString, string lpFileName);
		
		[DllImport("kernel32",EntryPoint="GetPrivateProfileStringW", ExactSpelling=true, CharSet=CharSet.Unicode, SetLastError=true)]
		private static extern int GetPrivateProfileString(string lpApplicationName, string lpKeyName, string lpDefault, string lpReturnedString, int nSize, string lpFileName);
		
		[DllImport("kernel32",EntryPoint="GetPrivateProfileSectionNamesW", ExactSpelling=true, CharSet=CharSet.Unicode, SetLastError=true)]
		private static extern int GetPrivateProfileSectionNames(string lpReturnedString, int nSize, string lpFileName);
		
		[DllImport("kernel32",EntryPoint="GetPrivateProfileSectionW", ExactSpelling=true, CharSet=CharSet.Unicode, SetLastError=true)]
		private static extern int GetPrivateProfileSection(string lpAppName, string lpReturnedString, int nSize, string lpFileName);
		
		#endregion
		
		private string m_Filename;
		
		public string Filename
		{
			get
            {				
                return m_Filename;
			}
			set
			{
				m_Filename = value;
			}
		}

        public IniFile()
        {
        }

        public IniFile(string path)
        {
            m_Filename = path;
        }
		
		private string FixIniKey(string S)
		{
			S = S.Replace("[", "{");
			S = S.Replace("]", "}");            

			return S;
		}

        private string FixIniValue(string S)
        {            
            if (Strings.StrLeft(S, 1) == "\"" && Strings.StrRight(S, 1) == "\"")
                S = "\"" + S + "\"";

            S = S.Replace("\t", "{#9}");

            return S;
        }
		
		public System.Collections.ArrayList INIReadSections()
		{
			int n;
			string sData;
            sData = Strings.Space(32768); // allocate some room
			
			System.Collections.ArrayList col = new System.Collections.ArrayList();			
			string[] varaSections;
			
			n = GetPrivateProfileSectionNames(sData, sData.Length, m_Filename);
			if (n > 0) // return whatever it gave us
			{
				varaSections = Strings.Split(sData, '\0');
				foreach (string strSection in varaSections)
				{					
					if (strSection.Trim()!= "")
					{
						col.Add(strSection);
					}
				}
			}
			
			return col;
		}

        public System.Collections.ArrayList INIReadSection(string Section)
        {
            int n;
            string sData;
            sData = Strings.Space(C_BufSize); // allocate some room

            System.Collections.ArrayList col = new System.Collections.ArrayList();
            string strSection;
            string[] varaSections;

            n = GetPrivateProfileSection(Section, sData, sData.Length, m_Filename);
            if (n > 0) // return whatever it gave us
            {
                varaSections = Strings.Split(sData, '\0');
                foreach (string tempLoopVar_strSection in varaSections)
                {
                    strSection = tempLoopVar_strSection;
                    if (strSection.Trim() != "")
                    {
                        col.Add(strSection);
                    }
                }
            }

            return col;
        }

        public System.Collections.ArrayList INIReadKeys(string Section)
        {
            int n;
            string sData;
            sData = Strings.Space(32768); // allocate some room

            System.Collections.ArrayList col = new System.Collections.ArrayList();            
            string[] varaKeys;

            n = GetPrivateProfileSection(Section, sData, sData.Length, m_Filename);

            int QuotePos;

            if (n > 0) // return whatever it gave us
            {
                varaKeys = Strings.Split(sData, '\0');
                foreach (string strKey in varaKeys)
                {
                    if (strKey.Trim() != "")
                    {
                        QuotePos = Strings.InStr(strKey, "=");
                        if (QuotePos > 0)
                            col.Add(Strings.StrLeft(strKey, QuotePos - 1).Replace("{#9}", "\t"));
                    }
                }
            }

            return col;
        }
		
		public string INIRead(string SectionName, string KeyName, string DefaultValue)
		{
			string returnValue;
			// primary version of call gets single value given all parameters
			
			if (!Strings.FileExists(m_Filename))
			{
				return DefaultValue;
			}

            KeyName = FixIniKey(KeyName);
			
			int n;
			string sData;
            sData = Strings.Space(32768); // allocate some room
			n = GetPrivateProfileString(SectionName, KeyName, DefaultValue, sData, sData.Length, m_Filename);
			if (n > 0) // return whatever it gave us
			{
				returnValue = sData.Substring(0, n);
                returnValue = returnValue.Replace("{#9}", "\t");
			}
			else
			{
                returnValue = DefaultValue;
			}
			return returnValue;
		}
				
		public string INIRead(string SectionName, string KeyName)
		{
			// overload 1 assumes zero-length default
			return INIRead(SectionName, KeyName, "");
		}
		
		public string INIRead(string SectionName)
		{
			// overload 2 returns all keys in a given section of the given file
			return INIRead(SectionName, null, "");
		}
		
		public string INIRead()
		{
			// overload 3 returns all section names given just path
			return INIRead(null, null, "");
		}		
		
		public void INIWrite (string SectionName, string KeyName, string TheValue)
		{
			if (! Strings.FileExists(Filename))
			{
				System.IO.StreamWriter Writer = new System.IO.StreamWriter(Filename, false, System.Text.Encoding.Unicode);
				Writer.WriteLine("[" + SectionName + "]");
                Writer.WriteLine(FixIniKey(KeyName) + "=" + FixIniValue(TheValue));
				Writer.Flush();
				Writer.Close();
			}
			else
			{
                KeyName = FixIniKey(KeyName);
                TheValue = FixIniValue(TheValue);
				WritePrivateProfileString(SectionName, KeyName, TheValue, m_Filename);
			}
		}
		
		public void INIDelete (string SectionName, string KeyName) // delete single line from section
		{
			WritePrivateProfileString(SectionName, KeyName, null, m_Filename);
		}
		
		public void INIDelete (string SectionName)
		{
			// delete section from INI file
			WritePrivateProfileString(SectionName, null, null, m_Filename);
		}
		
	}
	
}
