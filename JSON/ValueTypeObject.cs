using System;
using System.ComponentModel;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Pfz.Collections;

namespace KS.Foundation
{

	public static class JsonObjectArray
	{
		public static string ToJson(this object[] arr)
		{
			if (arr == null)
				return "[]";		
			return String.Format("[{0}]", String.Join (",", arr.Select(val => (val.GetType().Name + ":" + val).EscapeJson())));
		}			

		public static object[] FromJson(this string json)
		{
			if (String.IsNullOrEmpty(json))
				return new object[]{};
			JToken token = JToken.Parse (json);
			if (token == null)
				return new object[]{};
			List<object> lst = new List<object> ();
			token.Value<JArray>().Do (arr => arr.ForEach(r => {
				string val = r.Value<string>();
				if (val == null)
					lst.Add(null);
				else
				{					
					int i = val.IndexOf(':');
					if (i < 0)						
						lst.Add(val);
					else {
						string typeName = val.Substring(0, i);
						if (typeName.IndexOf(':') < 0)
							typeName = "System." + typeName;
						object obj;
						if (TypeResolver.Instance.TryConvertString(val.Substring(i + 1), typeName, out obj))
							lst.Add(obj);
						else
							lst.Add(null);
					}
				}
			}));
			return lst.ToArray ();
		}


		public static void TestValuePack()
		{
			object[] testArray = new object[1000000];
			for (int i = 0; i < testArray.Length; i++)
				testArray [i] = (object)ThreadSafeRandom.Next ();

			Console.WriteLine ("Packing one million integers as objects..");

			object[] test = new object[]{ true, false, true };

			string jsonBool = test.ToJson ();
			test = jsonBool.FromJson ();

			string json = null;
			PerformanceTimer.Time (() => {				
				json = testArray.ToJson();
				Console.WriteLine("Length: {0}", json.Length);
			});

			object[] arr;
			PerformanceTimer.Time (() => {
				arr = json.FromJson();
				Console.WriteLine("Length: {0}", arr.Length);
			});
				
			Console.ReadLine ();
		}
	}

}

