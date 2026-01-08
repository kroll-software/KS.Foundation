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
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace KS.Foundation
{
    [DataContract]
    [Serializable]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class XmlPropertiesProxy
    {
        public XmlPropertiesProxy()
        {
        }

        public XmlPropertiesProxy(string key, object value)
        {
            m_Key = key;
            m_Value = value;

            if (value != null)
                m_TypeCode = value.GetType().FullName;
        }
        
        private string m_Key = "";
        [XmlAttribute("Key")]
        [DataMember]
        public string Key
        {
            get
            {
                return m_Key;
            }
            set
            {
                m_Key = value;
            }
        }
        
        private object m_Value = "";
        [XmlIgnore]
        public object Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                m_Value = value;
            }
        }

        private string m_TypeCode = "";
        [XmlAttribute("Type")]
        [DataMember(Name = "TypeName")]
        public string TypeCode
        {
            get
            {                
                if (m_Value == null)
                    return null;
                else
                    return m_Value.GetType().FullName;
            }
            set
            {
                m_TypeCode = value;
            }
        }

        [XmlElement("Value")]
        [DataMember(Name = "Value")]
        public string ValueXML
        {
            get
            {
                if (m_Value == null)
                    return null;
                else
                {
                    if (m_Value is Boolean) return ((bool)m_Value) ? "true" : "false";
                    
                    TypeConverter tc = TypeDescriptor.GetConverter(m_Value.GetType());
                    return (!tc.IsValid(m_Value)) ? null : tc.ConvertToString(m_Value);
                }
            }
            set
            {
                if (m_TypeCode == "")
                    return;                

                switch (m_TypeCode)
                {
				case "DateTime":
				case "System.DateTime":					
						m_Value = value.SafeDateTime ();;
                        break;

				case "Boolean":
				case "System.Boolean":
					m_Value = value.SafeBool ();                    
					break;
                default:
                    Type t = Type.GetType(m_TypeCode, false, true);

                    if (t == null)
                    {
                        m_Value = null;
                        return;
                    }

                    try
                    {
                        TypeConverter tc = TypeDescriptor.GetConverter(t);
                        m_Value = tc.ConvertTo(value, t);
                    }
                    catch (Exception)
                    {
                        // this is for XML-Parsing and XML-Parsing should be error-tolerant
                        m_Value = null;
                    }
                    break;
                }                
            }
        }
    }

    [DataContract]
    [Serializable]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    [XmlInclude(typeof(FoundationItemText))]
    public abstract class FoundationItemTextProperties : FoundationItemText, ICloneable, ISerializable
    {
        [NonSerialized]
        protected SerializableDictionary<string, object> m_Properties = null;

        [DataMember(Name = "Properties")]        
        [XmlArray("Properties")]
        [XmlArrayItem("Property")]
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public XmlPropertiesProxy[] PropertiesXML
        {
            get
            {
                if (m_Properties == null)
                    return null;

                XmlPropertiesProxy[] p = new XmlPropertiesProxy[m_Properties.Keys.Count];

                int i = 0;
                foreach (string key in m_Properties.Keys)
                {
                    p[i] = new XmlPropertiesProxy(key, m_Properties[key]);                    
                    i++;
                }

                return p;
            }
            set
            {
                if (value == null || value.Length == 0)
                    return;

                m_Properties = new SerializableDictionary<string, object>();

                foreach (XmlPropertiesProxy p in value)
                {
                    if (!m_Properties.ContainsKey(p.Key))
                        m_Properties.Add(p.Key, p.Value);
                }
            }
        }

        public FoundationItemTextProperties()
            : base()
        {
        }

		public FoundationItemTextProperties(object context)
            : base(context)
        {
        }

        // this method is automatically called during serialization
        [EditorBrowsable(EditorBrowsableState.Advanced)]        
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("Properties", m_Properties, typeof(SerializableDictionary<string, object>));
        }

        // this constructor is automatically called during deserialization
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public FoundationItemTextProperties(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    //case "Text":
                    //    m_Text = entry.Value as String;
                    //    break;

                    //case "Description":
                    //    m_Description = entry.Value as String;
                    //    break;

                    case "Properties":
                        m_Properties = entry.Value as SerializableDictionary<string, object>;
                        break;
                }
            }

            //try
            //{
            //    m_Properties = info.GetValue("Properties", typeof(SerializableDictionary<string, object>)) as SerializableDictionary<string, object>;
            //}
            //catch (Exception ex)
            //{
            //    System.Diagnostics.Debug.WriteLine(ex.Message);
            //}            
        }

        public SerializableDictionary<string, object> Properties
        {
            get
            {
                return m_Properties;
            }
        }
        
        public void SetProperty(string key, object value)
        {
            if (m_Properties == null)
                m_Properties = new SerializableDictionary<string, object>();

            if (m_Properties.ContainsKey(key))
            {
                if (m_Properties[key] != value)
                {
                    m_Properties[key] = value;
                    Modified = true;
                }
            }
            else
            {
                m_Properties.Add(key, value);
                Modified = true;
            }
        }

        public object GetProperty(string key)
        {
            if (m_Properties == null)
                return null;

            if (m_Properties.ContainsKey(key))
            {
                return m_Properties[key];
            }
            else
            {
                return null;
            }
        }

        public bool ContainsProperty(string key)
        {
            return m_Properties != null && m_Properties.ContainsKey(key);
        }


        public string GetPropertiesXML()
        {
            if (m_Properties.IsNullOrEmpty())
                return String.Empty;

            try
            {
                return m_Properties.SerializeToXml();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return String.Empty;
            }            
        }

        public void SetPropertiesXML(string xml)
        {
            if (String.IsNullOrEmpty(xml))
            {
                m_Properties = null;
                return;
            }

            try
            {
                m_Properties = xml.DeserializeFromXml<SerializableDictionary<string, object>>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                m_Properties = null;
            }

            return;            
        }        

        // *** Clone ***

		public override object CopyTo(FoundationItem retval, object targetContext)
        {
            base.CopyTo(retval, targetContext);

            FoundationItemTextProperties item = retval as FoundationItemTextProperties;

            item.m_Properties = null;
            if (this.m_Properties != null)
            {
                item.m_Properties = new SerializableDictionary<string, object>();
                foreach (string key in this.m_Properties.Keys)
                    item.m_Properties.Add(key, this.m_Properties[key]);
            }

            return item;
        }

        
        protected override void CleanupManagedResources()
        {
            if (m_Properties != null)
                m_Properties.Clear();

            base.CleanupManagedResources();
        }
    }
}
