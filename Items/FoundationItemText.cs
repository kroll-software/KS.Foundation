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
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Drawing;
//using System.Windows.Forms;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Security.Permissions;
//using MsgPack;

namespace KS.Foundation
{
    [DataContract]
    [Serializable]
    [TypeConverter(typeof(ExpandableObjectConverter))]        
    [XmlInclude(typeof(FoundationItem))]
    public abstract class FoundationItemText : FoundationItem, ICloneable, ISerializable
    {
        public FoundationItemText()
            : base()
        {
        }

		public FoundationItemText(object context)
            : base(context)
        {
        }

        // this method is automatically called during serialization
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("Text", m_Text);
            info.AddValue("Description", m_Description);
        }

        // this constructor is automatically called during deserialization
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public FoundationItemText(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "Text":
                        m_Text = entry.Value as String;
                        break;

                    case "Description":
                        m_Description = entry.Value as String;
                        break;
                }
            }            
        }
        
        
        //protected string m_Text = String.Empty;
        protected string m_Text;
        [DataMember]
        public virtual string Text
        {
            get
            {
                return m_Text;
            }
            set
            {
                if (m_Text != value)
                {
                    m_Text = value;
                    Modified = true;                    
                }
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeText()
        {
            return !m_Text.IsNullOrEmpty();
        }


        protected string m_Description;
        [DataMember]
        public virtual string Description
        {
            get
            {
                return m_Description;
            }
            set
            {
                if (m_Description != value)
                {
                    m_Description = value;
                    Modified = true;                    
                }
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeDescription()
        {
            return !m_Description.IsNullOrEmpty();
        }

        public override string ToString()
        {
            if (m_Text == null)
                return base.ToString();
            
            return m_Text.ToString();                            
        }

        // *** Clone ***

		public override object CopyTo(FoundationItem retval, object targetContext)
        {
            base.CopyTo(retval, targetContext);

            ((FoundationItemText)retval).m_Text = this.m_Text;
            ((FoundationItemText)retval).m_Description = this.m_Description;

            return retval;
        }        

        // *** IDataErrorInfo Members ***

        public override string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case "Text":
                        if (String.IsNullOrEmpty(this.Text))
                            return "No Text Provided";
                        break;

                    default:
                        return String.Empty;
                }

                return String.Empty;
            }
        }

        public override string Error
        {
            get
            {
                //if (String.IsNullOrEmpty(this.Text))
                //    return Properties.Resources.InfoNoTextProvided;

                return String.Empty;
            }
        }
    }
}
