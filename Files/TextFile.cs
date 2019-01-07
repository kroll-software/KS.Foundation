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
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace KS.Foundation
{
	public static class TextFile
    {        				
		public static int TextFileLastError { get; private set; }
		public static string TextFileLastErrorDescription { get; private set; }

        public static string LoadTextFile(string filePath)
        {     
            if (!Strings.FileExists(filePath))
            {
                TextFileLastError = -1;
                TextFileLastErrorDescription = "File not found";
				return String.Empty;
            }

            string S;
            StreamReader File = null;

            try
            {
                File = new StreamReader(filePath, Encoding.Default, true);
                S = File.ReadToEnd();
                File.Close();
                return S;
            }
            catch (Exception ex)
            {
                TextFileLastError = -2;
                TextFileLastErrorDescription = ex.Message;
				return String.Empty;
            }
            finally
            {
                if (File != null)
                {
                    try
                    {
                        File.Dispose();
                        File = null;
                    }
                    catch { }
                }
            }
        }


        public static bool SaveTextFile(string filePath, string S, bool Append, object Encoding)
        {
            TextFileLastError = 0;
            TextFileLastErrorDescription = "";

            Encoding Enc;
            if (Encoding == null)            
                Enc = System.Text.Encoding.Unicode;            
            else            
                Enc = (Encoding)Encoding;

            StreamWriter File = null;

            try
            {                
                File = new StreamWriter(filePath, Append, Enc);
                File.Write(S);
                File.Flush();
                File.Close();
            }
            catch (Exception ex)
            {
                TextFileLastError = -3;
                TextFileLastErrorDescription = ex.Message;
                return false;
            }
            finally
            {
                if (File != null)
                {
                    try
                    {
                        File.Dispose();
                        File = null;
                    }
                    catch{}                    
                }
            }
            
			return true;
        }
    }
}
