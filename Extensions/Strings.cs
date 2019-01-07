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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace KS.Foundation
{
	public static class Strings
	{
		public static string TrimRightLinebreaks(this string S)
		{
			if (S == null)
				return String.Empty;

			int k = S.Length - 1;
			while (k >= 0 && S[k] == '\r' || S[k] == '\n' || S[k] == ' ')
				k--;

			if (k < 0)
				return String.Empty;
			else
				return S.Substring(0, k + 1);
		}

		public static bool Is7Bit(this string S)
		{
            if (String.IsNullOrEmpty(S))
                return true;

			return Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(S)) == S;
		}

		public static bool IsPunctation(this char c)
		{
			int b = Asc (c);
			if (b > 122)
				return b < 127;
			if (b > 90)
				return b < 97;
			if (b > 67)
				return b < 65;
			if (b > 31)
				return b < 48;
			return b == 10;
		}

		public static string Fill(int Count, char c)
		{
            return new string(c, Count);            
		}

		public static byte[] ConcatBytes(byte[] b1, byte[] b2)
		{
			if (b1.Length == 0) return b2;
			if (b2.Length == 0) return b1;

			byte[] bret = new byte[b1.Length + b2.Length];

			System.Buffer.BlockCopy(b1, 0, bret, 0, b1.Length);
			System.Buffer.BlockCopy(b2, 0, bret, b1.Length + 1, b2.Length);

			return bret;
		}

		public static bool StrLike(this string str, string wild, bool case_sensitive = false)
		{			
			if (str == null)
				str = "";
			if (wild == null)
				wild = "";

			if (! case_sensitive)
			{
				wild = wild.ToUpperInvariant();
				str = str.ToUpperInvariant();
			}

			int cp = 0, mp = 0;
			int i = 0;
			int j = 0;
			
			while ( (i < str.Length) && (j < wild.Length) && (wild[j] != '*') )
			{
				if ((wild[j] != str[i]) && (wild[j] != '?')) 
				{
					return false;
				}
				i++;
				j++;
			}
		
			while (i<str.Length) 
			{
				if (j<wild.Length && wild[j] == '*') 
				{
					if ((j++)>=wild.Length) 
					{
						return true;
					}
					mp = j;
					cp = i+1;
				} 
				else if (j<wild.Length && (wild[j] == str[i] || wild[j] == '?')) 
				{
					j++;
					i++;
				} 
				else 
				{
					j = mp;
					i = cp++;
				}
			}
		
			while (j<wild.Length && wild[j] == '*')
				j++;
					
			return j >= wild.Length;			
		}

		public static string[] Split(string S, string SplitChar, int MaxList)
		{
			if (string.IsNullOrEmpty (S))
				return new String[] { "" };

			List<string> A = new List<string>();
			int i;
			int j = 0;
			int l = SplitChar.Length;
       
			while ((j > -1) && (j < S.Length))
			{
				i = S.IndexOf(SplitChar, j);

				if (i == -1)
				{
					A.Add(S.Substring(j));
					break;
				}
				else
				{
					if ((MaxList > 0) && (A.Count >= MaxList - 1)) {
						A.Add (S.Substring (j));
						break;
					} else {
						A.Add (S.Substring (j, i - j));
					}

					j = i + l;
				}
			}

			return A.ToArray();
			/***
			string[] ret = new string[A.Count];
			for (int k = 0; k < A.Count; k++) {
				ret [k] = A [k];
			}
			return ret;
			***/
		}

		public static string[] Split(this string S, char SplitChar)
		{						
			return S.Split(SplitChar);
		}

		public static string[] Split(this string S, string SplitChar)
		{
			return Split(S, SplitChar, 0);
		}

		public static string[] SplitLines(this string S)
		{
            return Split(Replace(S, "\r\n", "\n"), "\n", 0);
		}

		public static double SecondsDiff(System.DateTime date1, System.DateTime date2)
		{
			return ((TimeSpan)(date2.Subtract(date1))).TotalSeconds;
		}

		public static string FormatDateShort(System.DateTime Expression)
		{
			//return String.Format(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, Expression);
			return Expression.ToShortDateString();
		}

		public static string FormatDateLong(System.DateTime Expression)
		{
			//return String.Format(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern, Expression);
			return Expression.ToLongDateString();
		}

		public static string FormatTimeShort(System.DateTime Expression)
		{
			//return String.Format(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, Expression);
			return Expression.ToShortTimeString();
		}

		public static string FormatTimeLong(System.DateTime Expression)
		{
			//return String.Format(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern, Expression);
			return Expression.ToLongTimeString();
		}

		public static string FormatDateTimeShort(System.DateTime Expression)
		{
			return Expression.ToShortDateString() + " " + Expression.ToShortTimeString();
		}

		public static string FormatDateTimeLong(System.DateTime Expression)
		{
			//return String.Format(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern, Expression);
			return Expression.ToLongDateString() + " " + Expression.ToLongTimeString();
		}

		public static string Space(int Number)
		{
            return new string(' ', Number);
		}

		public static char Chr(int CharCode)
		{
			return (char)CharCode;
		}

        public static int Asc(char c)
        {
            return (int)(c);
        }

        public static int Bool2Int(bool b)
        {
            if (b)
                return 1;
            else
                return 0;
        }

        public static bool ParseBool(string S)
        {
            if (String.IsNullOrEmpty(S))
                return false;

            switch (S.ToUpper())
            {
                case "0":
                case "F":
                case "FALSE":
                case "FALSCH":
                case "OFF":
                    return false;

                default:
                    return true;
            }

            // geht beides nicht:
            //return System.Convert.ToBoolean(S);
            //return bool.Parse(S);
        }

		public static int InStr(int Start, string String1, string String2)
		{
			if (String.IsNullOrEmpty(String1) || Start < 1 || Start > String1.Length) 
				return 0;			
			return String1.IndexOf(String2, Start - 1) + 1;
		}

		public static int InStr(string String1, string String2)
		{
			return InStr(1, String1, String2);
		}

        public static string ReverseString(string S)
        {
            if (String.IsNullOrEmpty(S))
                return String.Empty;

            StringBuilder SB = new StringBuilder(S.Length);
            for (int i = S.Length - 1; i > 0; i--)
                SB.Append(S[i]);
            return SB.ToString();
        }

        public static int InStrRev(int Start, string String1, string String2)
        {
			if (String.IsNullOrEmpty(String1) || Start < 1 || Start > String1.Length) 
				return 0;            
            String1 = ReverseString(String1);
            return String1.IndexOf(String2, Start - 1) + 1;            
        }

        public static int InStrRev(string String1, string String2)
		{
            return InStrRev(1, String1, String2);
		}
        
		public static string Replace(string Expression, string Find, string Replacement)
		{
            if (String.IsNullOrEmpty(Expression))
                return String.Empty;
			return Expression.Replace(Find, Replacement);
		}

		public static string ReplaceNoCase(string Expression, string Find, string Replacement)
		{
            if (String.IsNullOrEmpty(Expression))
                return String.Empty;

			int count, position0, position1;
			count = position0 = position1 = 0;
			string upperString = Expression.ToUpper();
			string upperPattern = Find.ToUpper();

			if(upperString.IndexOf(upperPattern) == -1) 
				return Expression;

			int inc = (Expression.Length / Find.Length) * (Replacement.Length - Find.Length);
			char[] chars = new char[Expression.Length + Math.Max(0, inc)];
			while( (position1 = upperString.IndexOf(upperPattern, position0)) != -1 )
			{
				for (int i = position0; i < position1; ++i )
					chars[count++] = Expression[i];
				
				for (int i = 0; i < Replacement.Length; ++i)
					chars[count++] = Replacement[i];
				
				position0 = position1 + Find.Length;
			}
			
			if ( position0 == 0 ) return Expression;
			
			for (int i = position0; i < Expression.Length; ++i)
				chars[count++] = Expression[i];
			
			return new string(chars, 0, count);
		}


		public static string SQLDouble(double d)
		{
			System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("en-US", false).NumberFormat;
			nfi.NumberGroupSeparator = "";
			try
			{
				return d.ToString(nfi);
			}
			catch
			{
				return "";
			}
		}

		public static string SQLDateTime(object dt, bool MSSQL)
		{
			if (dt.IsDate())
			{
				DateTime d;
				try
				{
					d = (DateTime)dt;
				}
				catch (Exception)
				{
					return "NULL";
				}
				
				if (MSSQL)
					return d.ToString(@"\'yyyy-MM-dd\THH:mm:ss\'");
				else
					return d.ToString("\\#MM\\/dd\\/yyyy HH:mm:ss\\#");
			}
			else
			{
				return "NULL";
			}
		}
		
		public static string SQLDate(object dt, bool MSSQL)
		{
			if (dt.IsDate())
			{
				DateTime d;
				try
				{
					d = (DateTime)dt;
				}
				catch (Exception)
				{
					return "NULL";
				}
				
				if (MSSQL)
					return d.ToString(@"\'yyyy-MM-dd\'");
				else
					return d.ToString("\\#MM\\/dd\\/yyyy\\#");
			}
			else
			{
				return "NULL";
			}
		}
		
		public static string SQLBool(object b)
		{
			try
			{
				if (System.Convert.ToBoolean(b))
					return "TRUE";
				else
					return "FALSE";
			}
			catch
			{
				return "FALSE";
			}
		}
		
		public static string FindBlock(this string S, string S1, string S2 = null)
		{
			int starttemp = 0;
			return FindBlock(S, S1, S2, ref starttemp);
		}

		public static string FindBlock(string S, string S1, string S2, ref int start, bool caseSensitive = false)
		{
            if (String.IsNullOrEmpty(S))
                return String.Empty;

            if (String.IsNullOrEmpty(S1))
                S1 = "";

            if (String.IsNullOrEmpty(S2))
                S2 = "";

			int i;
			int k;
			
			if (start < 1)
				start = 1;
			
			if (start > S.Length)
				return String.Empty;
						
			if (S1.Length == 0)
			{
				i = start;
			}
			else
			{
				if (caseSensitive)
					i = InStr(start, S, S1);
				else
					i = InStr(start, S.ToUpper(), S1.ToUpper());
				
				if (i == 0)
					return String.Empty;
				
				i = i + S1.Length;
			}
			
			if (S2.Length == 0)
			{
				start = S.Length;
				return StrMid(S, i).Trim();
			}
			else
			{
				if (caseSensitive)
					k = InStr(i, S, S2);
				else
					k = InStr(i, S.ToUpper(), S2.ToUpper());
					
				if (k == 0)
					return String.Empty;
								
				start = k + S2.Length;
				
				k -= 1;
				
				if (k < i)
					return String.Empty;
								
				return StrMid(S, i, k - i + 1);
			}
		}

		public static string DeleteBlock(this string S, string S1, string S2)
        {
            int starttemp = 0;
            return DeleteBlock(S, S1, S2, ref starttemp);
        }

        public static string DeleteBlock(string S, string S1, string S2, ref int start, bool caseSensitive = false)
        {
            if (String.IsNullOrEmpty(S))
                return String.Empty;

            if (String.IsNullOrEmpty(S1))
                S1 = "";

            if (String.IsNullOrEmpty(S2))
                S2 = "";

            int i;
            int k;

            if (start < 1)
            {
                start = 1;
            }
            if (start > S.Length)
            {
                return S;
            }

			if (caseSensitive)
				i = InStr(start, S, S1);            	
			else
				i = InStr(start, S.ToUpper(), S1.ToUpper());
				
            if (i == 0)
            {
                return S;
            }

			if (caseSensitive)
				k = InStr(i + S1.Length, S, S2);            	
			else
				k = InStr(i + S1.Length, S.ToUpper(), S2.ToUpper());
				
            if (k == 0)
            {
                return S;
            }

            k = k + S2.Length;            
            start = k + 1;

            return StrLeft(S, i - 1) + StrMid(S, k);
        }
		
		public static object ConvertNull(string S)
		{
            if (String.IsNullOrEmpty(S))
                return DBNull.Value;

            S = S.TrimEnd();

			if (S == "")
			{
				return DBNull.Value;
			}
			else
			{
				return S;
			}
		}
		
		public static string RestoreNull(object S)
		{			
			try
			{
				if (S == DBNull.Value)
				{
					return "";
				}
				else
				{
					return S.ToString().TrimEnd();
				}
			}
			catch (Exception)
			{
				return "";
			}
		}
		
		public static int RestoreInteger(object S)
		{
			if (S.IsNumeric())
			{
				return Int32.Parse(S.ToString());
			}
			else
			{
				return 0;
			}
		}
		
		
		public static string ConvertNullSQL(string S)
		{
            if (String.IsNullOrEmpty(S))
                return "NULL";

			if (S.Length == 0)
				return "NULL";
			else
				return "\'" + DoubleQuotes(S) + "\'";
		}
		
		public static string Email2Domain(string strEMail)
		{
            if (String.IsNullOrEmpty(strEMail))
                return String.Empty;

			int i;
			try
			{
				i = InStr(1, strEMail, "@");
				if (i > 0)
					return StrMid(strEMail, i + 1).ToLowerInvariant();
				else
					return String.Empty;
			}
			catch (Exception)
			{
				return String.Empty;
			}
		}
		
		public static string StrLeft(this string S, int i)
		{
            if (String.IsNullOrEmpty(S))
                return String.Empty;

			int l = S.Length;

			if ((l == 0) || (i < 1))
				return String.Empty;
            else if (i >= l)
                return S;
            else            
                return S.Substring(0, i);            
		}
		
		public static string StrRight(this string S, int i)
		{
            if (String.IsNullOrEmpty(S))
                return String.Empty;

			int l = S.Length;

			if ((l == 0) || (i < 1))
				return String.Empty;
			else if (i >= l)
				return S;
			else
				return S.Substring(l - i, i);
		}
		
		public static string StrMid(this string S, int Start)
		{
            if (String.IsNullOrEmpty(S))
                return String.Empty;

			int l = S.Length;

			if ((Start > l) || (Start < 1))
				return String.Empty;
			else
				return S.Substring(Start - 1);
		}

		public static string StrMid(this string S, int Start, int Length)
		{
            if (String.IsNullOrEmpty(S))
                return String.Empty;

			int l = S.Length;

			if ((Start > l) || (Start < 1) || (Length < 1))
				return String.Empty;
			else
				return S.Substring(Start - 1, Math.Min(Length, l - Start + 1));
		}        
		
		public static bool IsValidEmail(string strEmail)
		{
            if (string.IsNullOrEmpty(strEmail))
                return false;

			int i;
			int k;
			
			// --- Test
			if (InStr(strEmail.ToUpper(), "FAX:") > 0)
			{
				return true;
			}

            if (InStr(strEmail, " ") > 0)
            {
                return false;
            }

			i = InStr(strEmail, "@");
			if (i < 1)
			{
				return false;
			}
			
			k = InStr(i + 1, strEmail, ".");
			return (k > 0);
		}
		
		public static string FixURL(string strURL)
		{
            if (String.IsNullOrEmpty(strURL))
                return String.Empty;

			strURL = strURL.Trim();
			if (strURL == "") return String.Empty;
						
			if ((InStr(strURL.ToUpper(), "HTTP://") == 0) && (InStr(strURL.ToUpper(), "HTTPS://") == 0))
			{
				strURL = "http://" + strURL;
			}
			return strURL;
		}
		
		public static string BackSlash(this string S, bool bOn)
		{
            if (String.IsNullOrEmpty(S))
				S = String.Empty;

			string ps = System.IO.Path.DirectorySeparatorChar.ToString();

			if (bOn)
			{
				if (StrRight(S, 1) != ps)
					S = S + ps;
			}
			else
			{
				if (StrRight(S, 1) == ps)
					S = StrLeft(S, S.Length - 1);
			}
			
			return S;
		}
		
		public static string GetPathName(this string FilePath)
		{
            if (String.IsNullOrEmpty(FilePath))
                return String.Empty;

			// Todo
			//return System.IO.Path.GetDirectoryName (FilePath);

            // This can throw an error and so the result would be undefined
            //if (IsDirectory(BackSlash(FilePath, false)))
            //    return FilePath;

			int i = FilePath.Length;

			string ps = System.IO.Path.DirectorySeparatorChar.ToString();
			
			while (i > 0)
			{
				if (FilePath.Substring(i - 1, 1)== ps)
					break;

				i -= 1;
			}
			
			if (i > 3)
            //if (i > 0)
				i -= 1;

			return StrLeft(FilePath, i);
		}
		
		public static string ApplicationPath(bool bBackSlash)
		{
			return BackSlash(GetPathName(System.Reflection.Assembly.GetExecutingAssembly().Location.ToString()), bBackSlash);
		}
		
		public static string LeadingZero(string S, int Digits)
		{
            if (S == null)
                S = String.Empty;
			while (S.Length < Digits)
				S = "0" + S;					
			return S;
		}
		
		//Public Function LeadingZero(ByVal i As Integer, Optional ByVal Digits As Integer = 2) As String
		//    Dim nfi As System.Globalization.NumberFormatInfo
		//    nfi = New System.Globalization.CultureInfo("en-US", False).NumberFormat
		//    nfi.NumberGroupSeparator = ""
		
		//    Dim S As String
		//    S = System.Convert.ToInt32(i).ToString(nfi)
		//    Do While Len(S) < Digits
		//        S = "0" & S
		//    Loop
		
		//    LeadingZero = S
		//End Function
		
		public static string FormatSekunden2hms(double Sekunden)
		{
			double h;
			double m;
			double S;
			double Rest;
			
			if (Sekunden == 0)
				return "00:00:00";
						
			Sekunden = Math.Round(Sekunden);			
			h = Math.Floor(Sekunden / 3600);
			Rest = Sekunden % 3600;
			m = Math.Floor(Rest / 60);
			S = Math.Floor(Rest % 60);

			return String.Format ("{0}:{1}:{2}", LeadingZero(Convert.ToInt32(h).ToString(), 2), LeadingZero(Convert.ToInt32(m).ToString(), 2), LeadingZero(Convert.ToInt32(S).ToString(), 2));
		}
		
		public static string QuotedString(string S)
		{
			return '\u0022'+ S + '\u0022';
		}
		
		
		public static string DoubleQuotes(string S)
		{
            if (String.IsNullOrEmpty(S))
                return String.Empty;

			return S.Replace("\'", "\'\'");
		}
		
		public static string DoubleDoubleQuotes(string S)
		{
            if (String.IsNullOrEmpty(S))
                return String.Empty;

			return S.Replace("\"", "\"\"");
		}
		
		public static string GetFilename(string FullFileName)
		{
            if (String.IsNullOrEmpty(FullFileName))
                return String.Empty;

			int i = FullFileName.Length;

			string ps = System.IO.Path.DirectorySeparatorChar.ToString();

			while (i > 0)
			{
				if (FullFileName.Substring(i - 1, 1)== ps)
					break;
				
				i -= 1;
			}
			
			return StrRight(FullFileName, FullFileName.Length - i);
		}
		
		public static string GetExtension(string FileName, bool mitPunkt)
		{
            if (String.IsNullOrEmpty(FileName))
                return String.Empty;

			int i = FileName.Length;
			
			while (i > 0)
			{
				if (FileName.Substring(i - 1, 1) == ".")
					break;
				
				i -= 1;
			}
			
			if (i == 0)
				return String.Empty;

			if (mitPunkt)
				i -= 1;
						
			return StrRight(FileName, FileName.Length - i);
		}
		
		public static string BackSlashURL(string S, bool bBackSlash)
		{
            if (String.IsNullOrEmpty(S))
                S = "";

			try
			{	
				string C = StrRight(S, 1);
				
				if (bBackSlash)
				{
					if (!(C == "/" || C == "\\"))
						S = S + "/";
				}
				else
				{
					if (C == "/" || C == "\\")
						S = StrLeft(S, S.Length - 1);
				}
				
				return S;	
			}
			catch
			{
				return S;
			}
		}
		
		public static string GetPathnameURL(string FullFileName)
		{
            if (String.IsNullOrEmpty(FullFileName))
                return String.Empty;

			int i = FullFileName.Length;
			string C;
			
			while (i > 0)
			{
				C = FullFileName.Substring(i - 1, 1);
				if (C == "/" || C == "\\")
					break;

				i -= 1;
			}
			
			if (i > 0)
				i -= 1;
			
			return StrLeft(FullFileName, i);
		}
		
		public static string DeleteStr(string S, int Start, int Length)
		{
			try
			{
				return StrLeft(S, Start - 1) + StrRight(S, S.Length - Start - Length + 1);
			}
			catch (Exception)
			{
				return String.Empty;
			}
		}
		
		public static string InsertStr(string S1, string S2, int i)
		{            
			return StrLeft(S1, i) + S2 + StrRight(S1, S1.Length - i);
		}
		
		public static void DevideStr(string S, string SplitChar, ref string S1, ref string S2)
		{
			int i;
			i = InStr(S, SplitChar);
			
			if (i > 0)
			{
				S1 = StrLeft(S, i - 1);
				S2 = StrRight(S, S.Length - i);
			}
			else
			{
				S1 = S;
				S2 = String.Empty;
			}
		}
		
		public static ArrayList String2ArrayList(string S, char Delimiter)
		{
			ArrayList ret = new ArrayList();
            if (String.IsNullOrEmpty(S))
                return ret;

            string Z;

			string[] A = Strings.Split(S, Delimiter);			
			foreach (string Z_loop in A)
			{
                Z = Z_loop.Trim();				
				if (Z != "")
				{
					ret.Add(Z);
				}
			}
			
			return ret;
		}

        public static List<string> String2List(string S, char Delimiter)
        {
            List<string> ret = new List<string>();
            if (String.IsNullOrEmpty(S))
                return ret;

            string Z;

            string[] A = Strings.Split(S, Delimiter);
            foreach (string Z_loop in A)
            {
                Z = Z_loop.Trim();
                if (Z != "")
                {
                    ret.Add(Z);
                }
            }

            return ret;
        }
        
        public static bool FileExists(string FileFullPath)
        {
            if (String.IsNullOrEmpty(FileFullPath))
                return false;
            
            if (FileFullPath.Trim().Length == 0) return false;

            try
            {
                System.IO.FileInfo f = new System.IO.FileInfo(FileFullPath);
                return f.Exists;
            }
            catch (Exception)
            {
                return false;
            }            
        }

        public static bool FileIsReadOnly(string FileFullPath)
        {
            if (String.IsNullOrEmpty(FileFullPath))
                return false;

            if (FileFullPath.Trim().Length == 0) return false;

            try
            {
                System.IO.FileInfo f = new System.IO.FileInfo(FileFullPath);
                return f.IsReadOnly;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public static bool DirExists(string DirPath)
        {
            if (String.IsNullOrEmpty(DirPath.Trim()))
                return false;

            DirPath = BackSlash(DirPath, false);

            try
            {
                System.IO.DirectoryInfo f = new System.IO.DirectoryInfo(DirPath);
                return f.Exists;
            }
            catch (Exception)
            {
                return false;
            }            
        }

        public static bool DirIsReadOnly(string DirPath)
        {
            if (String.IsNullOrEmpty(DirPath.Trim()))
                return false;

            DirPath = BackSlash(DirPath, false);

            try
            {
                System.IO.DirectoryInfo f = new System.IO.DirectoryInfo(DirPath);
                return (f.Attributes & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public static bool IsDirectory(string path)
        {
            try
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(path);
                return di.Exists;

                //System.IO.FileInfo f = new System.IO.FileInfo(path);
                //return (f.Attributes & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string ShortenFileName(string sFilePathAndName, int MaxCharAllowed)
        {
            if (String.IsNullOrEmpty(sFilePathAndName))
                return String.Empty;

            string sPath = "";
            string sName = "";            
          
			string sMiddle = ".." + System.IO.Path.DirectorySeparatorChar.ToString();
          
            sFilePathAndName = sFilePathAndName.Trim();
          
            if (sFilePathAndName.Length <= MaxCharAllowed)
               return sFilePathAndName;               
                      
            sPath = GetPathName(sFilePathAndName);

            sPath = sPath.Trim();
            sName = GetFilename(sFilePathAndName);

            if (sName.Length > MaxCharAllowed - 6)
                sName = StrMid(sName, sName.Length - (MaxCharAllowed - 6));            
          
            if ( ((String)(sMiddle + sName)).Length >= MaxCharAllowed - 3)
            {
               sPath = StrLeft(sPath, 3);
            }
            else
            {
                if (((String)(sPath + sMiddle + sName)).Length > MaxCharAllowed)
                {
                    sPath = StrLeft(sPath, MaxCharAllowed - sMiddle.Length - sName.Length);
                }               
            }
          
            return sPath + sMiddle + sName;
        }

        public static string WrapText(this string S, int MaxWidth)
        {
            if (String.IsNullOrEmpty(S))
                return String.Empty;

            string[] Lines = SplitLines(S);

            StringBuilder SB = new StringBuilder(S.Length + 100);

            foreach (string line in Lines)
            {
                if (SB.Length > 0)
                    SB.Append("\r\n");

                SB.Append(WrapLine(line, MaxWidth));                
            }

            return SB.ToString();
        }

        private static string WrapLine(string S, int MaxWidth)
        {
            if (String.IsNullOrEmpty(S))
                return String.Empty;

            if (S.Length <= MaxWidth)
                return S;
            else
            {                                
                StringBuilder SB = new StringBuilder(S.Length + 20);

                while (S.Length > MaxWidth)
                {
                    int i = MaxWidth - 1;
                    while (i >= 0 && !IsWrapCharacter(S[i]))
                        i--;
                    
                    if (i < 0)
                        i = MaxWidth;

                    if (SB.Length > 0)
                        SB.Append("\r\n");

                    SB.Append(S.Substring(0, i + 1));

                    S = S.Substring(i + 1).Trim();
                }

                if (S.Length > 0)
                {
                    if (SB.Length > 0)
                        SB.Append("\r\n");

                    SB.Append(S);
                }

                return SB.ToString();
            }
        }

        public static bool IsWrapCharacter(this char c)
        {
            switch (c)
            {
            case ' ':
            case '-':
            case '_':
            case ',':
            case ';':
            case '.':
            case '!':
            case '?':
			case '\\':
			case '/':
                return true;

            default:
                return false;
            }
        }

        public static string CamelCaseToSpace(this string CamelCase)
        {            
			if (String.IsNullOrEmpty (CamelCase))
				return String.Empty;

            StringBuilder sb = new StringBuilder();
			bool bSpaceFlag = true;
            foreach (char c in CamelCase)
            {
				if (c == ' ')
					bSpaceFlag = true;
				else if (!bSpaceFlag && Char.IsUpper (c)) {
					sb.Append (" ");
					bSpaceFlag = true;
				} else if (!Char.IsUpper (c)) {
					bSpaceFlag = false;
				}

                sb.Append(c);
            }
            return sb.ToString();
        }

		public static string ProperCase(this string S)
        {
			if (String.IsNullOrEmpty (S))
				return String.Empty;

            StringBuilder sb = new StringBuilder();
            int i = 0;
            bool bSpaceFlag = false;
            foreach (char c in S)
            {
                if ((i == 0 || bSpaceFlag) && Char.IsLower(c))                
                    sb.Append(Char.ToUpper(c));                
                else
                    sb.Append(c);

                bSpaceFlag = c == ' ';
                i++;
            }
            return sb.ToString();
        }

        public static string RandomString(int maxlength, bool alphanumeric)
        {
            StringBuilder SB = new StringBuilder(maxlength);

            Random rnd = new Random();
            for (int i = 0; i < rnd.Next(1, maxlength + 1); i++)
            {
                if (alphanumeric)
                {
                    if (SB.Length > 0 && rnd.NextDouble() > 0.9d)
                    {
                        SB.Append(' ');
                    }
                    else
                    {
                        switch (rnd.Next(3))
                        {
                            case 0:
                                if (SB.Length > 0)
                                    SB.Append((char)rnd.Next(48, 58));
                                break;

                            case 1:
                                SB.Append((char)rnd.Next(65, 91));
                                break;

                            case 2:
                                SB.Append((char)rnd.Next(97, 123));
                                break;
                        }
                    }
                }
                else
                    SB.Append((char)rnd.Next(32, 127));
            }

            return SB.ToString();
        }

        // Older Functions for Compatibility
        public static String GetSafeString(Object obj)
        {
            return obj.SafeString();
        }

        public static int GetSafeInteger(Object obj, int DefaultValue)
        {
            return obj.SafeInt(DefaultValue);
        }

        public static decimal GetSafeDecimal(Object obj, decimal DefaultValue)
        {
            return obj.SafeDecimal(DefaultValue);
        }

        public static double GetSafeDouble(Object obj, double DefaultValue)
        {
            return obj.SafeDouble(DefaultValue);
        }

        public static float GetSafeFloat(Object obj, float DefaultValue)
        {
            return obj.SafeFloat(DefaultValue);
        }

        public static DateTime GetSafeDateTime(Object obj, DateTime DefaultValue)
        {
            return obj.SafeDateTime(DefaultValue);
        }

        public static bool GetSafeBool(Object obj)
        {
            return obj.SafeBool();
        }

        public static bool IsDate(object value)
        {
            return value.IsDate();
        }

        public static bool IsNumeric(object value)
        {
            return value.IsNumeric();
        }
	}
	
}
