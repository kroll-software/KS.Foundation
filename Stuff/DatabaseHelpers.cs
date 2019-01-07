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

using System.Diagnostics;
using System;
using System.Data;
using System.Collections;
using System.Runtime.InteropServices;

namespace KS.Foundation
{
	public sealed class DatabaseHelpers
	{		
		private static string m_WindowsUser = "";
		
		public static string WindowsUser()
		{
            if (m_WindowsUser != "")
			{
                return m_WindowsUser;
			}
			else
			{
				string strUser = "";
				string[] varaUser;

				try
				{
					strUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
					varaUser = strUser.Split('\\');
                    m_WindowsUser = varaUser[1];
				}
				catch
				{
                    m_WindowsUser = strUser.SafeString();
				}

                return m_WindowsUser;
			}
		}
		
		public static string GetSQLServerConnectionString(string Server, string Database, bool bWinSecurity, string UserID, string Password, long ConnectionTimeout)
		{
			string returnValue;
			if (bWinSecurity)
			{
				returnValue = "Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=" + Database + ";Data Source=" + Server + ";Connect Timeout=" + ConnectionTimeout.ToString();
			}
			else
			{
				returnValue = "Password=" + Password + ";Persist Security Info=True;User ID=" + UserID + ";Initial Catalog=" + Database + ";Data Source=" + Server + ";Connect Timeout=" + ConnectionTimeout.ToString();
			}
			return returnValue;
		}

		public static string GetAccessConnectionString(string Filename, long ConnectionTimeout)
		{
			const string MDAC_PROVIDER = "Microsoft.Jet.OLEDB.4.0";            
			return "PROVIDER=" + MDAC_PROVIDER + ";Data Source=" + Filename + ";Persist Security Info=False";
		}
	}
	
}
