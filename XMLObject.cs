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
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace KS.Foundation
{    
    public static class XmlObjectHelpers
    {        
        public static string SerializeToXml<T>(this T obj)
        {
            return ToXmlString<T>(obj);
        }

        public static T DeserializeFromXml<T>(this string xml)
        {
            return FromXmlString<T>(xml);
        }        

        public static void ToXmlStream<T>(T o, Stream stream)
        {            
            XmlSerializer serializer = null;

            try
            {
                serializer = typeof(T).CreateDefaultXmlSerializer();                
                serializer.Serialize(stream, o);                
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {                
            }
        }

        public static T FromXmlStream<T>(Stream stream)
        {
            if (stream == null || stream.Length == 0)
                return default(T);            

            XmlSerializer serializer = null;

            try
            { 
                if (stream.CanSeek)
                    stream.Seek(0, SeekOrigin.Begin);

                serializer = typeof(T).CreateDefaultXmlSerializer();
                return (T)serializer.Deserialize(stream);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {                
            }
        }

        //public static string ToXmlString<T>(T o)
        //{
        //    if (o.IsNull())
        //        return String.Empty;

        //    using (MemoryStream ms = new MemoryStream())
        //    using (StreamReader reader = new StreamReader(ms))
        //    {
        //        ToXmlStream<T>(o, ms);
        //        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        //        return reader.ReadToEnd();
        //    }
        //}

        public static string ToXmlString<T>(T serialisableObject)
        {
            XmlSerializer xmlSerializer = CreateDefaultXmlSerializer(serialisableObject.GetType());

            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add(String.Empty, String.Empty);

            using (var ms = new MemoryStream())
            {
                using (var xw = XmlWriter.Create(ms,
                    new XmlWriterSettings()
                    {
                        Encoding = new UTF8Encoding(false),                        
                        Indent = true,
                        NewLineOnAttributes = true,
                        OmitXmlDeclaration = true,
                    }))
                {
                    xmlSerializer.Serialize(xw, serialisableObject, ns);
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }
        
        public static T FromXmlString<T>(string xml)
        {
            if (String.IsNullOrEmpty(xml))
            {
                return default(T);
            }

            //using (MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(xml)))            
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xml)))            
            {                
                return FromXmlStream<T>(ms);
            }            
        }

        public static void ToXmlFile<T>(T o, string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                ToXmlStream<T>(o, fs);
                fs.Flush(true);
                fs.Close();
            }
        }

        public static T FromXmlFile<T>(string path)
        {
            if (!System.IO.File.Exists(path))
                return default(T);

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return FromXmlStream<T>(fs);
            }
        }

        private static readonly Dictionary<Type, XmlSerializer> _xmlSerializerCache = new Dictionary<Type, XmlSerializer>();
        public static XmlSerializer CreateDefaultXmlSerializer(this Type type)
        {
            XmlSerializer serializer;
            if (_xmlSerializerCache.TryGetValue(type, out serializer))
            {
                return serializer;
            }
            else
            {
                var importer = new XmlReflectionImporter();
                var mapping = importer.ImportTypeMapping(type, null, null);                
                serializer = new XmlSerializer(mapping);
                return _xmlSerializerCache[type] = serializer;
            }
        }
    }

    public sealed class XmlAnything<T> : IXmlSerializable
    {
        public XmlAnything() { }
        public XmlAnything(T t) { this.Value = t; }
        public T Value { get; set; }

        public void WriteXml(XmlWriter writer)
        {
            if (Value == null)
            {
                writer.WriteAttributeString("type", "null");
                return;
            }
            Type type = this.Value.GetType();

            XmlSerializer serializer = type.CreateDefaultXmlSerializer();
            writer.WriteAttributeString("type", type.AssemblyQualifiedName);
            serializer.Serialize(writer, this.Value);
        }

        private Type VersionIndependentType(string typeString)
        {
            string[] names = typeString.Split(new char[] { ',' }, 2);
            if (names.Length != 2)
                throw new ArgumentException("Invalid type string.");

            string assemblyName = names[1];
            string typeName = names[0];

            Type typeToDeserialize = null;

            AssemblyName aname = Assembly.GetExecutingAssembly().GetName();
            Version assemblyVersion = aname.Version;
            String assemblyTitle = aname.Name;

            String searchString = assemblyTitle + ", Version=";

            int searchCount = searchString.Length;
            int searchIndex = assemblyName.IndexOf(searchString);
            if (searchIndex != -1)
            {
                int versionLength = assemblyName.Substring((searchCount + searchIndex)).IndexOf(",");
                assemblyName = assemblyName.Replace(assemblyName.Substring((searchCount + searchIndex), versionLength), assemblyVersion.ToString());
            }

            searchIndex = typeName.IndexOf(searchString);
            if (searchIndex != -1)
            {
                int versionLength = typeName.Substring((searchCount + searchIndex)).IndexOf(",");
                typeName = typeName.Replace(typeName.Substring((searchCount + searchIndex), versionLength), assemblyVersion.ToString());
            }

            // **************

            searchString = "KS.Gantt.Gantt";
            string replaceString = "KS.Gantt.GanttControl";

            assemblyName = assemblyName.Replace(searchString, replaceString);
            typeName = typeName.Replace(searchString, replaceString);

            typeToDeserialize = Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));

            return typeToDeserialize;
        }

        public void ReadXml(XmlReader reader)
        {
            if (!reader.HasAttributes)
                throw new FormatException("expected a type attribute!");
            string type = reader.GetAttribute("type");
            reader.Read(); // consume the value
            if (type == "null")
                return;// leave T at default value

            Type tt = VersionIndependentType(type);
            XmlSerializer serializer = tt.CreateDefaultXmlSerializer();

            // ToDo: Check: Should work with app settings
            //Type tt = typeof(T);
            //XmlSerializer serializer = tt.CreateDefaultXmlSerializer();

            //XmlSerializer serializer = Type.GetType(type).CreateDefaultXmlSerializer();
            this.Value = (T)serializer.Deserialize(reader);
            reader.ReadEndElement();
        }

        public XmlSchema GetSchema() { return (null); }
    } 
}
