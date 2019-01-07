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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace KS.Foundation
{
    public static class JsonObjects
    {		
		public static void SerializeToStream<T>(this T obj, Stream s)
		{
			if (obj == null)
				return;

			using (StreamWriter writer = new StreamWriter(s))
			using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
			{
				JsonSerializer ser = new JsonSerializer();
				ser.TypeNameHandling = TypeNameHandling.Auto;
				ser.TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
				ser.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Local;
				ser.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
				//ser.DefaultValueHandling = DefaultValueHandling.Include;
				ser.Serialize(jsonWriter, obj);
				jsonWriter.Flush();
			}
		}

		public static T Deserialize<T>(Stream s)
		{
			using (StreamReader reader = new StreamReader(s))
			using (JsonTextReader jsonReader = new JsonTextReader(reader))
			{
				JsonSerializer ser = new JsonSerializer();
				ser.TypeNameHandling = TypeNameHandling.Auto;
				ser.TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
				ser.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Local;
				ser.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
				//ser.DefaultValueHandling = DefaultValueHandling.Populate;
				return ser.Deserialize<T>(jsonReader);
			}
		}
			
		public static JsonSerializerSettings DefaultJsonSerializerSettings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.Auto,
			TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple,
			DateTimeZoneHandling = DateTimeZoneHandling.Local,
			NullValueHandling = NullValueHandling.Ignore,
			//DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
		};

        public static string SerializeToJson<T>(this T obj, Newtonsoft.Json.Formatting formatting = Formatting.None)
        {
            if (obj == null)
                return String.Empty;

			return JsonConvert.SerializeObject (obj, formatting, DefaultJsonSerializerSettings);
        }

        public static T DeserializeFromJson<T>(this string json) where T : class
        {
            if (json.IsNullOrEmpty())
                return null;

			return JsonConvert.DeserializeObject<T> (json, DefaultJsonSerializerSettings);
        }

        public static object DeserializeFromJson(this string json, Type type)
        {
            if (json == null)
                return null;

			return JsonConvert.DeserializeObject (json, type, DefaultJsonSerializerSettings);
        }
    }
}
