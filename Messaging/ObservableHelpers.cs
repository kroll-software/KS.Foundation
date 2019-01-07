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

namespace KS.Foundation
{
    public interface IFoundationMessage
    {
        string Subject { get; }
        bool ShouldReEnqueue { get; }        
    }

    public struct FoundationMessage : IFoundationMessage
    {
		public FoundationMessage(string subject, string body, Type itemType, string itemKey, object context, object data)
        {
            m_Subject = subject;
            m_Body = body;
            m_ItemType = itemType;
            m_Key = itemKey;
            m_Context = context;
            m_Data = data;
            m_Timestamp = DateTime.Now;
        }

        private object m_Context;
		public object Context
        {
            get
            {
                return m_Context;
            }
        }

        private Type m_ItemType;
        public Type ItemType
        {
            get
            {
                return m_ItemType;
            }
        }

        private string m_Key;
        public string Key
        {
            get
            {
                return m_Key;
            }
        }

        private string m_Subject;
        public string Subject
        {
            get
            {
                return m_Subject;
            }
        }

        private string m_Body;
        public string Body
        {
            get
            {
                return m_Body;
            }
        }

        private object m_Data;
        public object Data
        {
            get
            {
                return m_Data;
            }
        }

        private DateTime m_Timestamp;
        public DateTime Timestamp 
        {
            get
            {
                return m_Timestamp;
            }
        }

        public bool ShouldReEnqueue
        {
            get
            {
                //return Data != null;
				return true;
            }
        }
    }

    public class MessageUnknownException : Exception
    {
        internal MessageUnknownException()
        {
        }
    }	   
}
