using System;
using System.ComponentModel;
using Pfz.Collections;

namespace KS.Foundation
{
	public class TypeConverterTuple : Tuple<Type, TypeConverter> 
	{
		public TypeConverterTuple(Type t, TypeConverter tc) : base(t, tc) {}
	}

	public class TypeResolver
	{
		static TypeResolver()
		{
			Instance = new TypeResolver ();
		}

		public static TypeResolver Instance;

		readonly ThreadSafeDictionary<string, TypeConverterTuple> TypeConverters;

		public bool TryResolveType(string name, out TypeConverterTuple ttc)
		{						
			if (TypeConverters.TryGetValue (name, out ttc)) {
				return true;
			} else {
				Type t = Type.GetType (name, false, false);
				if (t == null) {
					return false;
				} else {
					TypeConverter tc = TypeDescriptor.GetConverter (t);
					TypeConverterTuple nttc = new TypeConverterTuple (t, tc);
					TypeConverters.Add (name, nttc);
					ttc = nttc;
					return true;
				}	
			}
		}

		public bool TryConvertString(string value, string name, out object item)
		{
			switch (name) {
			case "":
			case "String":
			case "System.String":
				item = value;
				return true;

			case "DateTime":
			case "System.DateTime":
				item = value.SafeDateTime ();
				return true;

			case "Boolean":
			case "System.Boolean":
				item = value.SafeBool ();
				return true;

			default:
				TypeConverterTuple ttc;
				if (TryResolveType (name, out ttc)) {
					if (String.IsNullOrEmpty (value) || ttc.Item2 == null)
						item = ttc.Item1.IsValueType ? Activator.CreateInstance (ttc.Item1) : null;
					else
						item = ttc.Item2.ConvertTo (value, ttc.Item1);
					return true;
				}
				item = null;
				return false;
			}
		}

		public TypeResolver ()
		{
			TypeConverters = new ThreadSafeDictionary<string, TypeConverterTuple> ();
		}
	}
}

