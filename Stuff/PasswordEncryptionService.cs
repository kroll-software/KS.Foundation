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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Security.Cryptography;

namespace KS.Foundation
{
    public interface IPasswordEncryptionService
    {
        string EncryptString(string s);
        string DecryptString(string s);
    }
        
    public class PasswordEncryptionService : IPasswordEncryptionService
    {
        private byte[] entropy;

        public PasswordEncryptionService(string entropySource)
        {
            entropy = System.Text.Encoding.Unicode.GetBytes(entropySource);
        }

        private string EncryptFromSecureString(System.Security.SecureString input)
        {
            byte[] encryptedData = System.Security.Cryptography.ProtectedData.Protect(
                System.Text.Encoding.Unicode.GetBytes(ToInsecureString(input)),
                entropy,
                System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        private SecureString DecryptToSecureString(string encryptedData)
        {
            try
            {
                if (String.IsNullOrEmpty(encryptedData))
                    return new SecureString();

                byte[] decryptedData = System.Security.Cryptography.ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedData),
                    entropy,
                    System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return ToSecureString(System.Text.Encoding.Unicode.GetString(decryptedData));
            }
            catch
            {
                return new SecureString();
            }
        }

        private SecureString ToSecureString(string input)
        {
            SecureString secure = new SecureString();
            foreach (char c in input)
            {
                secure.AppendChar(c);
            }
            secure.MakeReadOnly();
            return secure;
        }

        private string ToInsecureString(SecureString input)
        {
            if (input == null)
                return String.Empty;

            string returnValue = string.Empty;
            IntPtr ptr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(input);
            try
            {
                returnValue = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(ptr);
            }
            return returnValue;
        }

        public string EncryptString(string s)
        {
            try
            {
                return EncryptFromSecureString(ToSecureString(s));
            }
            catch (Exception)
            {
                return "";
            }
        }

        public string DecryptString(string s)
        {
            try
            {
                return ToInsecureString(DecryptToSecureString(s));
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
