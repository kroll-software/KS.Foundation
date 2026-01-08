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
using System.Linq;
using System.Drawing;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Runtime.CompilerServices;  // both for new PropertyChanged Syntax
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KS.Foundation
{    
    public interface IKeyedObject
    {
        string Key { get; }        
    }

    public interface IFoundationItem : IDisposable
    {
        string Key { get; set; }
        object Context { get; set; }
		void SetUp(object context);
        void Rewire();                
        void CleanUp();
        [XmlIgnore]
        bool Modified { get; set; }
        object Clone(object context);
        bool IsDisposed { get; }

        void FixConsistency();
        void CheckConsistency();
    }

    [DataContract]
    [Serializable]    
    [TypeConverter(typeof(ExpandableObjectConverter))]    
    public abstract class FoundationItem : DisposableObject, IFoundationItem, IKeyedObject, IDisposable, ICloneable, ISerializable, IDataErrorInfo
    {                        
        [XmlIgnore]
		[JsonIgnore]
        [DataMember]  // ToDo: To check | Important for Undo/Redo
        public virtual bool Modified { get; set; }        

        private object m_Tag = null;
        [Browsable(false)]
        [DataMember]
        public object Tag
        {
            get
            {
                return m_Tag;
            }
            set
            {
                if (value != null && !(value as ISerializable != null || (Attribute.IsDefined(value.GetType(), typeof(SerializableAttribute)))))
                    throw new Exception("Tag-Property must be set to a serializable type or attribute");

                m_Tag = value;
            }
        }

        //public static int baseItem_instanceCount = 0;
        //private int m_InstanceCount = 0;
        public FoundationItem()
            : this(null, null)
        {
            //m_InstanceCount = baseItem_instanceCount++;

            // attention !!!
            //m_ModelContext = ModelContextFoundation.currentmodelcontext;
            //m_Key = System.Guid.NewGuid().ToString();
            //_hash = m_Key.GetHashCode();
        }

        //protected override void OnFinalizerCalled()
        //{            
        //    System.Diagnostics.Debug.WriteLine(String.Format("Instance No. {0} of {1} was not disposed before garbage collection.", m_InstanceCount.ToString(), this.GetType().FullName));
        //}

        public FoundationItem(object context)
            : this(context, null)
        {
        }

		public FoundationItem(object context, string key)
        {
            m_Context = context;            
            if (key == null)
                m_Key = System.Guid.NewGuid().ToString();
            else
                m_Key = key;
            _hash = m_Key.GetHashCode();
        }
        
        [XmlIgnore]
        [IgnoreDataMember]
        private int _hash = 0;
        public override int GetHashCode()
        {
            if (_hash == 0)
                _hash = m_Context.SafeHash().CombineHash(m_Key.SafeHash());
            return _hash;
        }

        public int GetHashCode(FoundationItem item)
        {
            return item.GetHashCode();
        }

        [XmlIgnore]        
        [IgnoreDataMember]
        [JsonIgnore]
        public bool IsDeSerializing { get; private set; }
        
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            IsDeSerializing = true;
            //m_SerializingContextOtherFlag = context.State == StreamingContextStates.Other;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            IsDeSerializing = false;
            OnAfterDeserialized();
        }

        protected virtual void OnAfterDeserialized()
        {

        }

        // this method is automatically called during serialization
        [EditorBrowsable(EditorBrowsableState.Advanced)]        
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (context.Context != null)
                m_Context = context.Context;

            info.AddValue("Key", m_Key);

            // Zukunftssicher: KEINE Verwendung von StreamingContextStates
            var tag = m_Tag;
            if (tag != null &&
                (tag is ISerializable ||
                Attribute.IsDefined(tag.GetType(), typeof(SerializableAttribute))))
            {
                info.AddValue("Tag", tag);
            }
        }


        // this constructor is automatically called during deserialization
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public FoundationItem(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new System.ArgumentNullException("info");

            if (context.Context != null)
                m_Context = context.Context;

            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "Key":
                        Key = entry.Value as String;
                        break;

                    case "Tag":
                        m_Tag = entry.Value;
                        break;
                }
            }            

            if (String.IsNullOrEmpty(m_Key))
                Key = System.Guid.NewGuid().ToString();

            //if (!String.IsNullOrEmpty(m_Key))
            //    _hash = m_Key.GetHashCode();            
        }        


        //[NonSerialized]
        //[XmlIgnore]
		protected object m_Context = null;
        [Browsable(false)]        
        [XmlIgnore]
		public virtual object Context
        {
            get
            {
                if (IsDisposed)
                    return null;
                else
                    return m_Context;
            }
            set
            {
                if (m_Context != value)
                {
                    _hash = 0;
                    m_Context = value;                    
                }
            }
        }        

        // (Step 1)
		public virtual void SetUp(object context)
        {
            Context = context;
        }

        // (Step 2)
        public virtual void Rewire()
        {
            if (m_Context == null)
                throw new InvalidOperationException("ModelContext must not be null on Rewire()");
        }

        public virtual void FixConsistency()
        {
        }

        // (Step 4)
        //private bool m_IsCleanedUp = false;
        public virtual void CleanUp()
        {
            //m_IsCleanedUp = true;
        }

        // (Step 5)
        // protected override void CleanupManagedResources()

        // Diagnosis.Debug
        public virtual void CheckConsistency()
        {
        }        

        [IgnoreDataMember]
        protected string m_Key = null;
        [DataMember]
        [XmlAttribute]
        [JsonInclude]
        [ReadOnly(true)]    // OK, still Xml-Serialized
        public virtual string Key
        {
            get
            {
                //System.Diagnostics.Debug.Assert(m_Key != null);
                return m_Key;
            }
            set
            {
                if (m_Key != value)
                {
                    m_Key = value;
                    _hash = 0;
                    Modified = true;                    
                }
            }
        }        

        // *** Clone ***        

        public object CopyTo(FoundationItem retval)
        {
            return this.CopyTo(retval, this.m_Context);
        }

		public virtual object CopyTo(FoundationItem retval, object targetContext)
        {                        
            retval.m_Key = this.m_Key;
            retval._hash = this._hash;
            retval.m_Context = targetContext;
            retval.Tag = this.Tag;
            retval.Modified = this.Modified;            
            return retval;
        }               

        public virtual object Clone()
        {
            return this.CopyTo(Activator.CreateInstance(this.GetType(), this.m_Context) as FoundationItem, this.m_Context);            
        }

		public virtual object Clone(object targetContext)
        {			
			return this.CopyTo(Activator.CreateInstance(this.GetType(), targetContext) as FoundationItem, targetContext);
        }        

        // ********* IDisposable **********           

        protected override void CleanupManagedResources()
        {
            //System.Diagnostics.Debug.Assert(m_GanttModelContext == null || m_GanttModelContext.IsLoadingOrClosing() || m_IsCleanedUp);            

            m_Context = null;

            // CleanUp here and set to null
            if (m_Tag != null && m_Tag.IsDisposable())
            {
                ((IDisposable)m_Tag).Dispose();                
            }

            base.CleanupManagedResources();
        }

        // *** IDataErrorInfo Members ***
        public virtual string this[string columnName]
        {
            get
            {
                return String.Empty;
            }
        }

        public virtual string Error
        {
            get
            {
                return String.Empty;
            }
        }        
    }

    public static class BaseItemExtensions
    {
		public static bool IsValidModelObject<TModel>(this FoundationItem item, object context)
        {
            return item != null && !item.IsDisposed && item.Context == context;
        }
    }
}
