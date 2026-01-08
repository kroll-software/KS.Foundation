using System.Data;
using System;
using System.IO;

namespace KS.Foundation
{
	public class CSVReader
	{
		public static BlockingQueue<DataRow> RowQueue
		{
			get
			{
				return Singleton<BlockingQueue<DataRow>>.Instance;
			}
		}

        public enum CSVColumnTypes
		{
			ctUnknown = 0,
			ctString = 1
		}
		
		public delegate void ProgressChangeEventHandler(int PercentDone);
		private ProgressChangeEventHandler ProgressChangeEvent;
		
		public event ProgressChangeEventHandler ProgressChange
		{
			add
			{
				ProgressChangeEvent = (ProgressChangeEventHandler) System.Delegate.Combine(ProgressChangeEvent, value);
			}
			remove
			{
				ProgressChangeEvent = (ProgressChangeEventHandler) System.Delegate.Remove(ProgressChangeEvent, value);
			}
		}			
		
		// <<<<<<<<<<<<<<<<<<<<<<<< Interface >>>>>>>>>>>>>>>>>>>>>>>>>>
		
		private int m_MaxColumns;
		
		private bool bCancel;
		private int m_RecordCount;
		private int m_ColumnCount;
		private int m_LastError;
		private string m_LastErrorDescription;

        private Int64 m_BytesRead;
        private int m_MaxRows;

		private bool bHasResult;
		private string[] ColumnValues;
		private CSVColumnTypes[] ColumnTypes;
		private int[] ColumnMaxLengths;
		
		// ------- File IO	
		private StreamReader File;
        private Int64 m_Filesize;
		
		private System.Text.StringBuilder SB;
		private Dos2Win Dos2Win;
		
		
		public string Filename { get; set; }		
		public string Delimiter { get; set; }		
		public string TextMarker { get; set; }
		public string DecimalMarker { get; set; }
		public bool ConvertFromAscii { get; set; }
		public bool ColumnHeaders { get; set; }

		public int PercentDone
		{
			get
            {
				if (File == null)
				{
					return 0;
				}
				else
				{
                    if (m_MaxRows > 0)
                    {
                        if (m_RecordCount < 5)
                            return 0;

                        double AvgLineLength = m_BytesRead / m_RecordCount;

                        if (AvgLineLength == 0)
                            return 0;
                        
                        int ProjectedLines = (int)(m_Filesize / (int)AvgLineLength);                        
                        return Math.Min((int)(m_RecordCount * 100 / Math.Min(ProjectedLines, m_MaxRows)), 100);
                    }
                    else
					    return Math.Min((int)(File.BaseStream.Position * 100 / File.BaseStream.Length), 100);
				}
			}
		}
		
		public bool IsFileOpen { get; private set; }
		
		public bool EOF
		{
			get{
				if (File == null)
				{
					return true;
				}
				else
				{
					try
					{
						return File.Peek() < 0 && ! bHasResult;
					}
					catch (Exception ex)
					{
						m_LastError = - 1;
						m_LastErrorDescription = ex.Message;
						return true;
					}
				}
			}
		}

        public Int64 Filesize
		{
			get{
				return m_Filesize;
			}
		}

        public Int64 BytesRead
        {
            get
            {
                return m_BytesRead;
            }
        }

		public int RecordCount
		{
			get
            {
				return m_RecordCount;
			}
		}        
		
		public int ColumnCount
		{
			get{
				return m_ColumnCount;
			}
		}
		
		public string ColumnValue(int Index){
			if (Index < m_MaxColumns)
			{
				return ColumnValues[Index];
			}
			else
			{
				return "";
			}
		}
		
		public int ColumnMaxLen(int Index){
			if (Index < m_MaxColumns)
			{
				return ColumnMaxLengths[Index];
			}
			else
			{
				return 0;
			}
		}
		
		public CSVColumnTypes ColumnType(int Index){
			if (Index < m_MaxColumns)
			{
				return ColumnTypes[Index];
			}
			else
			{
				return CSVColumnTypes.ctUnknown;
			}
		}
		
		
		public int MaxColumns
		{
			get{
				return m_MaxColumns;
			}
			set
			{
				if (value < 1)
				{
					value = 1;
				}
				m_MaxColumns = value;
				ResetColumnWidths();
				ResetColumnProperties();
			}
		}
		
		public int LastError
		{
			get{
				return m_LastError;
			}
		}
		
		public string LastErrorDescription
		{
			get
            {
				return m_LastErrorDescription;
			}
		}
		
		
		// <<<<<<<<<<<<<<<<<<<<<<<<< Implementation >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
		
		public CSVReader() {
			Dos2Win = new Dos2Win();
			
			SB = new System.Text.StringBuilder();
			Reset();
		}
		
		~CSVReader()
		{
			Close();        
			//base.Finalize();
		}
			        
		public bool Queued  { get; set; }
        
		public void Reset ()
		{
			Filename = "";
			Delimiter = ";";
			ConvertFromAscii = false;
			TextMarker = "\"";
			DecimalMarker = ".";
			ColumnHeaders = false;
			bHasResult = false;

            Queued = false;
						
            m_BytesRead = -1;
            m_MaxRows = -1;

            m_RecordCount = -1;
			m_ColumnCount = 0;
			m_LastError = 0;
			m_LastErrorDescription = "";
			
			m_MaxColumns = 256;
			ResetColumnWidths();
			ResetColumnProperties();
			
			//bCancel = False
			SB.Remove(0, SB.Length);
		}
		
		private void ResetError ()
		{
			m_LastError = 0;
			m_LastErrorDescription = "";
		}
		
		private void ResetColumnWidths ()
		{
			ColumnMaxLengths = new int[m_MaxColumns-1 + 1];
		}
		
		private void ResetColumnProperties ()
		{
			ColumnValues = new string[m_MaxColumns-1 + 1];
			ColumnTypes = new CSVColumnTypes[m_MaxColumns-1 + 1];
		}
		
		// --- File
		public bool Open(string FName, bool AutoDetectDelimiters, System.Text.Encoding Encoding)
		{
            if (Queued)
            {
                RowQueue.Start();
            }

			if (IsFileOpen)
			{
				Close();
			}
			
			if (Encoding == null)
			{
				Encoding = System.Text.Encoding.Default;
			}
			
			if (FName != "")
			{
				Filename = FName;
			}
			
			if (! Strings.FileExists(Filename))
			{
				m_LastError = - 1;
				m_LastErrorDescription = "File not found";
				return false;
			}
			
			try
			{
				FileInfo info = new FileInfo(Filename);
				m_Filesize = (int)(info.Length);
			}
			catch (Exception ex)
			{
				m_LastError = - 1;
				m_LastErrorDescription = ex.Message;
				return false;
			}
			
			try
			{
				File = new StreamReader(Filename, Encoding, true);
			}
			catch (Exception ex)
			{
				m_LastError = - 1;
				m_LastErrorDescription = ex.Message;
				return false;
			}
			
			IsFileOpen = true;
			
			ResetError();
			ResetColumnProperties();
			ResetColumnWidths();
			
			if (AutoDetectDelimiters)
			{
				Delimiter = AutoDetectDelimiter();
				if (Delimiter != null)
				{
					TextMarker = AutoDetectTextmarker(Delimiter);
					if (TextMarker != null)
					{
						ColumnHeaders = AutoDetectColumnHeaders(Delimiter, TextMarker);
					}
				}
				
				if (Delimiter == null)
				{
					Delimiter = ";";
				}
				if (TextMarker == null)
				{
					TextMarker = "";
				}
				if (Encoding == System.Text.Encoding.ASCII)
					ConvertFromAscii = AutoDetectAscii();
				if (! MoveFirst())
				{
					return false;
				}
			}
			
			return true;
		}
		
		public void Close ()
		{
			if (IsFileOpen)
			{
				try
				{
					File.Close();
				}
				catch (Exception)
				{
				}
			}	

			if (Queued)
				RowQueue.Quit();

			IsFileOpen = false;
			m_Filesize = 0;
			
			ResetError();
			ResetColumnProperties();
			ResetColumnWidths();
		}
		
		public bool InitRead()
		{			
			if (! IsFileOpen)
			{
				return false;
			}
			
			if (! MoveFirst())
			{
				return false;
			}
						
            m_MaxRows = 0;
            m_BytesRead = 0;
            
            m_RecordCount = 0;
			m_ColumnCount = 0;

			if (! MoveNext())
			{
				return false;
			}
			
			if (ColumnHeaders)
			{
				m_RecordCount = 0;                
				if (! MoveNext())
				{
					return false;
				}
			}
			
			return true;
		}
		
		public bool MoveFirst()
		{
			if (! IsFileOpen)
			{
				return false;
			}
			try
			{
				File.DiscardBufferedData(); // darauf muss man ja nun erstmal kommen ...
				File.BaseStream.Seek(File.CurrentEncoding.GetPreamble().Length, SeekOrigin.Begin);
				//File.BaseStream.Seek(0, IO.SeekOrigin.Begin)
			}
			catch (Exception ex)
			{
				m_LastError = - 1;
				m_LastErrorDescription = ex.Message;
				return false;
			}
			
			bHasResult = false;
			ResetColumnProperties();
			return true;
		}
		
		
		public bool MoveNext()
		{
LabelStart:
			
			ResetError();
			ResetColumnProperties();
			
			try
			{
				if (File.Peek() < 0)
				{
					bHasResult = false;
					return true;
				}
			}
			catch (Exception ex)
			{
				m_LastError = - 1;
				m_LastErrorDescription = ex.Message;
				return false;
			}
			
			SB.Length = 0;
			
			char[] cc = new char[1];
			char c;
			char cp;
			
			bool bTextFlag = false;
			
			int iColumn = 0;

			char cTextMarker = '\0';
			if (TextMarker.Length > 0)
				cTextMarker = TextMarker[0];

			char cDelimiter = '\0';
			if (Delimiter.Length > 0)
				cDelimiter = Delimiter[0];
			
						
			try
			{
                while (File.Peek() >= 0)
                {
					File.Read(cc, 0, 1);
                    this.m_BytesRead++;

					c = cc[0];
					
					if (ConvertFromAscii)
					{
						c = Dos2Win.Ascii2Ansi(c);
					}
					
					
					if (bTextFlag)
					{
						if (c == cTextMarker)
						{
							if (File.Peek() >= 0)
							{
								cp = Strings.Chr(File.Peek());
								if (cp == TextMarker[0])
								{
									SB.Append(c);
									if (File.Peek() >= 0)
									{
										File.Read(cc, 0, 1);
                                        this.m_BytesRead++;

										c = cc[0];
										if (ConvertFromAscii)
										{
											c = Dos2Win.Ascii2Ansi(c);
										}
									}
								}
								else
								{
									bTextFlag = false;
								}
							}
						}
						else
						{
							SB.Append(c);
						}
					}
					else
					{
						if (c == cTextMarker)
						{
							bTextFlag = true;
							//ColumnProperties(iColumn).ColumnType = CSVColumnTypes.ctString
							
							if (iColumn < m_MaxColumns)
							{
								ColumnTypes[iColumn] = CSVColumnTypes.ctString;
							}
						}
						else if (c == cDelimiter || c == '\n' || c == '\r')
						{
							
							// --- Skip Empty Line
                            if (SB.Length == 0 && iColumn == 0 && (c == '\n' || c == '\r'))
							{
								if (File.Peek() < 0)
								{
									bHasResult = false;
									return true;
								}
								goto LabelStart;
							}
							
							//ColumnProperties(iColumn).Value = SB.ToString
							if (iColumn < m_MaxColumns)
							{
								ColumnValues[iColumn] = SB.ToString();
								
								if (SB.Length > ColumnMaxLengths[iColumn])
								{
									ColumnMaxLengths[iColumn] = SB.Length;
								}
							}
							
							//SB = New System.Text.StringBuilder
							SB.Length = 0;
							iColumn++;

                            if (c == '\n' || c == '\r')
							{
								m_ColumnCount = iColumn;
								break;
							}
							
						}
						else if (c == '\n' || c == '\r')
						{
							// --- Skip
						}
						else
						{
							SB.Append(c);
						}
					}
				}
			}
			catch (Exception ex)
			{
				m_LastError = - 1;
				m_LastErrorDescription = ex.Message;
				return false;
			}
			
			if (SB.Length > 0)
			{
				if (iColumn < m_MaxColumns)
				{
					ColumnValues[iColumn] = SB.ToString();
					
					if (SB.Length > ColumnMaxLengths[iColumn])
					{
						ColumnMaxLengths[iColumn] = SB.Length;
					}
				}
				
				iColumn++;
			}
			
			if (iColumn < m_MaxColumns)
			{
				m_ColumnCount = iColumn;
			}
			else
			{
				m_ColumnCount = m_MaxColumns;
			}
			
			m_RecordCount++;
			
			bHasResult = true;
			return true;
		}
		
		// --- Auto Detect Functions
		
		private int CountCharacters(string S, string c)
		{
			char cc;
			int Count = 0;
			foreach (char tempLoopVar_cc in S)
			{
				cc = tempLoopVar_cc;
				if (cc == c[0])
				{
					Count++;
				}
			}
			return Count;
		}
		
		// Returns Delimiter Or Nothing
		public string AutoDetectDelimiter()
		{
			ResetError();
			if (! IsFileOpen)
			{
				return null;
			}
			if (! MoveFirst())
			{
				return null;
			}
			
			int[] charcount = new int[4];
			int i;
			string S;
			
			for (i = 0; i <= 3; i++)
			{
				charcount[i] = 0;
			}
			
			try
			{
				for (i = 0; i <= 9; i++)
				{
					if (File.Peek() < 0)
					{
						break;
					}
					S = File.ReadLine();
					
					charcount[0] += CountCharacters(S, ",");
					charcount[1] += CountCharacters(S, ";");
					charcount[2] += CountCharacters(S, "\t");
					charcount[3] += CountCharacters(S, "|");
				}
			}
			catch (Exception ex)
			{
				m_LastError = - 1;
				m_LastErrorDescription = ex.Message;
				return null;
			}
			
			int index = 0;
			int imax = 0;
			
			for (i = 0; i <= 3; i++)
			{
				if (charcount[i] > imax)
				{
					imax = charcount[i];
					index = i;
				}
			}
			
			if (imax == 0)
			{
				return null;
			}
			else
			{
				switch (index)
				{
					case 0:
						return ",";
					
					case 1:	
						return ";";
						
					case 2:	
						return "\t";
						
					case 3:	
						return "|";
				}
			}
			
			// --- Hier kommen wir nie an...
			return null;
		}
		
		
		// Returns Textmarker Or Nothing
		public string AutoDetectTextmarker(string Delimiter)
		{
			string OldMarker = TextMarker;
			TextMarker = "";
			
			ResetError();
			if (! IsFileOpen) goto ErrExit;
			if (! MoveFirst()) goto ErrExit;			
			if (! InitRead()) goto ErrExit;
			
			if (! EOF)
			{
				if (! MoveNext()) goto ErrExit;
			}
			
			if (! bHasResult) goto ErrExit;
			
			string S;
			int i;
			for (i = 0; i <= m_ColumnCount - 1; i++)
			{
				S = ColumnValue(i);
				if (S.Length > 1)
				{
					if (Strings.StrLeft(S, 1) == "\"" && Strings.StrRight(S, 1) == "\"")
					{
						TextMarker = OldMarker;
						return "\"";
					}
					
					if (Strings.StrLeft(S, 1)== "\'" && Strings.StrRight(S, 1) == "\'")
					{
						TextMarker = OldMarker;
						return "\'";
					}
				}
			}
			
ErrExit:
			TextMarker = OldMarker;
			return null;
		}
		
		
		public bool AutoDetectColumnHeaders(string Delimiter, string TextMarker)
		{
			ResetError();
			if (! IsFileOpen)
			{
				return false;
			}
			if (! MoveFirst())
			{
				return false;
			}
		
			bool OldColumnHeaders = ColumnHeaders;
			ColumnHeaders = false;
			
			string OldMarker = TextMarker;			
			string OldDelimiter = Delimiter;
			
			if (! InitRead())
			{
				goto ErrExit;
			}
			
			string S;
			int i;
			for (i = 0; i <= m_ColumnCount - 1; i++)
			{
				S = ColumnValue(i);
				if (S.IsNumeric() || S.Length == 0)
				{
					TextMarker = OldMarker;
					Delimiter = OldDelimiter;
					ColumnHeaders = OldColumnHeaders;
					return false;
				}
			}
			
			return true;
			
ErrExit:
			TextMarker = OldMarker;
			Delimiter = OldDelimiter;
			ColumnHeaders = OldColumnHeaders;
			return false;
		}
		
		public bool AutoDetectAscii()
		{
			ResetError();
			if (! IsFileOpen)
			{
				return false;
			}
			if (! MoveFirst())
			{
				return false;
			}
			
			int i;
			
			System.Text.StringBuilder SB = new System.Text.StringBuilder();
			
			try
			{
				for (i = 0; i <= 50; i++)
				{
					if (File.Peek() < 0)
					{
						break;
					}
					SB.Append(File.ReadLine());
				}
			}
			catch (Exception ex)
			{
				m_LastError = - 1;
				m_LastErrorDescription = ex.Message;
				return false;
			}
			
			return Dos2Win.IsAscii(SB.ToString());
		}


		// --- Higer Level Functions ---
		
		public DataSet GetDataset(int MaxRows)
		{
			
			bool OldColumnHeaders = ColumnHeaders;
			ColumnHeaders = false;
			if (! InitRead())
			{
				return null;
			}
			ColumnHeaders = OldColumnHeaders;

            m_MaxRows = MaxRows;
			
			int i;
			DataSet DS = new DataSet();
			DataTable DT = new DataTable();

			bCancel = false;
			
			string strColumnName;
			
			if (ColumnHeaders)
			{
				for (i = 0; i <= m_ColumnCount - 1; i++)
				{
					strColumnName = ColumnValues[i].Trim();
					if (strColumnName == "")
					{
						strColumnName = "Column" + i.ToString();
					}

                    try
                    {
                        DT.Columns.Add(strColumnName, Type.GetType("System.String"));
                    }
                    catch
                    {
                        strColumnName = "Column" + i.ToString();
                        DT.Columns.Add(strColumnName, Type.GetType("System.String"));
                    }					
				}
				if (! EOF)
				{
					if (! MoveNext())
					{
						return null;
					}
				}
				m_RecordCount = 1;
			}
			else
			{
				for (i = 0; i <= m_ColumnCount - 1; i++)
				{
					DT.Columns.Add("Column" + i.ToString(), Type.GetType("System.String"));
				}
			}
			
			DataRow Row;
			int Percent;            
			
			if (! EOF)
			{
				if (ProgressChangeEvent != null)
					ProgressChangeEvent(0);
				
				while (! EOF && ! bCancel)
				{
					Row = DT.NewRow();
					for (i = 0; i <= m_ColumnCount - 1; i++)
					{
						try
						{
							Row[i] = Strings.ConvertNull(ColumnValues[i]);
						}
						catch (Exception)
						{
						}
					}

                    if (Queued)
                        RowQueue.Enqueue(Row);                        
                    else
                        DT.Rows.Add(Row);
					
					if (! MoveNext())
					{
						return null;
					}
					
                    Percent = PercentDone;
                    if (ProgressChangeEvent != null)
                        ProgressChangeEvent(Percent);
					
					if (MaxRows > 0 && m_RecordCount > MaxRows)
					{
						break;
					}                    
				}
				
				if (ProgressChangeEvent != null)
					ProgressChangeEvent(100);
			}


            //if (Queued)
            //    RowQueue.Quit();

			DS.Tables.Add(DT);
			return DS;
		}
			
        public bool Cancel()
        {
            bCancel = true;
            return true;
        }

		public static DataSet LoadFile(string fname, string delimiter = null, bool ColumnHeaders = true, string textMarker = null, string decimalMarker = null, System.Text.Encoding enc = null, bool queued = false)
		{
			CSVReader CSV = null;

			bool bAuto = delimiter.IsNullOrEmpty() || textMarker.IsNullOrEmpty () || decimalMarker.IsNullOrEmpty ();
			if (enc == null)
				//enc = System.Text.Encoding.UTF8;
				enc = System.Text.Encoding.GetEncoding("Latin1");

			try {
				CSV = new CSVReader ();

				if (!delimiter.IsNullOrEmpty())
					CSV.Delimiter = delimiter;
				if (!textMarker.IsNullOrEmpty ())
					CSV.TextMarker = textMarker;
				if (!decimalMarker.IsNullOrEmpty ())
					CSV.DecimalMarker = decimalMarker;
				CSV.Open (fname, bAuto, enc);

				CSV.ColumnHeaders = ColumnHeaders;

				CSV.Queued = queued;
				return CSV.GetDataset (0);
			} catch (Exception ex) {
				ex.LogError();
				return null;
			}
			finally{
				if (CSV != null)
					CSV.Close();
			}
		}		
    }		
}
