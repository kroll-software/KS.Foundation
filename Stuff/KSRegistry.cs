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
using Microsoft.Win32;

namespace KS.Foundation
{
	public class KSRegistry
	{
		
		public enum RegTrees
		{
			CurrentUser = 0,
			LocalMachine = 1,
            ClassesRoot = 2
		}

		RegTrees r_RegTree = RegTrees.CurrentUser;
		public RegTrees RegTree
		{
			get
			{return r_RegTree;}
			set
			{r_RegTree = value;}
		}
		
		string r_sectionKey;
		public string sectionKey
		{
			set
			{
				r_sectionKey = value;
			}
		}
		
		string r_valueKey;
		public string valueKey
		{
			set
			{
				r_valueKey = value;
			}
		}
		
		object r_defaultValue;
		public object defaultValue
		{
			set
			{
				r_defaultValue = value;
			}
		}
		
		public object value
		{
			get
			{
				RegistryKey regKey = null;
				object vValue = null;

				switch (r_RegTree)
				{
					case RegTrees.CurrentUser:
						regKey = Registry.CurrentUser.OpenSubKey(r_sectionKey, false);
						if (regKey == null)
						{						
							return r_defaultValue;
						}
						break;

					case RegTrees.LocalMachine:
						regKey = Registry.LocalMachine.OpenSubKey(r_sectionKey, false);
						if (regKey == null)
						{							
							return r_defaultValue;
						}
						break;


                    case RegTrees.ClassesRoot:
                        regKey = Registry.ClassesRoot.OpenSubKey(r_sectionKey, false);
                        if (regKey == null)
                        {                            
                            return r_defaultValue;
                        }
                        break;

					default:
						break;
				}

				vValue = regKey.GetValue(r_valueKey, r_defaultValue);
				regKey.Close();

				if (vValue == null)
					return r_defaultValue;
				else
					return vValue;
			}
			set
			{
				RegistryKey regKey = null;
				
				switch (r_RegTree)
				{
					case RegTrees.CurrentUser:
						regKey = Registry.CurrentUser.OpenSubKey(r_sectionKey, true);
						if (regKey == null)
						{
							createSubKey();
							regKey = Registry.CurrentUser.OpenSubKey(r_sectionKey, true);
						}
						break;

					case RegTrees.LocalMachine:
						regKey = Registry.LocalMachine.OpenSubKey(r_sectionKey, true);
						if (regKey == null)
						{
							createSubKey();
							regKey = Registry.LocalMachine.OpenSubKey(r_sectionKey, true);
						}
						break;

                    case RegTrees.ClassesRoot:
                        regKey = Registry.ClassesRoot.OpenSubKey(r_sectionKey, true);
                        if (regKey == null)
                        {
                            createSubKey();
                            regKey = Registry.ClassesRoot.OpenSubKey(r_sectionKey, true);
                        }
                        break;
                    
                    default:
						break;
				}
				
				regKey.SetValue(r_valueKey, value);
				regKey.Close();
			}
		}
		

		public bool SectionExists()
		{
			RegistryKey regKey = null;

			switch (r_RegTree)
			{
				case RegTrees.CurrentUser:
					regKey = Registry.CurrentUser.OpenSubKey(r_sectionKey, false);
					break;

				case RegTrees.LocalMachine:
					regKey = Registry.LocalMachine.OpenSubKey(r_sectionKey, false);
					break;

                case RegTrees.ClassesRoot:
                    regKey = Registry.ClassesRoot.OpenSubKey(r_sectionKey, false);
                    break;

				default:
					break;
			}
			
			return (regKey != null);
		}

		public void deleteValue ()
		{
			RegistryKey regKey = null;

			switch (r_RegTree)
			{
				case RegTrees.CurrentUser:
					regKey = Registry.CurrentUser.OpenSubKey(r_sectionKey, true);
					break;

				case RegTrees.LocalMachine:
					regKey = Registry.LocalMachine.OpenSubKey(r_sectionKey, true);
					break;

                case RegTrees.ClassesRoot:
                    regKey = Registry.ClassesRoot.OpenSubKey(r_sectionKey, true);
                    break;

				default:
					break;
			}
			
			if (regKey != null)
			{
				regKey.DeleteValue(r_valueKey);
				regKey.Close();
			}
		}
		
		private void createSubKey ()
		{
			RegistryKey regkey = null;

			switch (r_RegTree)
			{
				case RegTrees.CurrentUser:
					regkey = Registry.CurrentUser.CreateSubKey(r_sectionKey);
					break;

				case RegTrees.LocalMachine:
					regkey = Registry.LocalMachine.CreateSubKey(r_sectionKey);
					break;

                case RegTrees.ClassesRoot:
                    regkey = Registry.ClassesRoot.CreateSubKey(r_sectionKey);
                    break;

				default:
					break;
			}
			
			regkey.Close();
		}
		
	}
	
}
