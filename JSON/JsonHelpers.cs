using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using KS.Foundation;
using KS.Foundation.ECS;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Converters;

namespace KS.Foundation
{
	public static class JsonHelpers
	{
		public static string CurlyBraces(this string s)
		{
			return "{" + s + "}";
		}

		public static string Brackets(this string s)
		{
			return "[" + s + "]";
		}

		public static string SerializeColor(this Color color)
		{
			if (color.IsEmpty)
				return null;
			//else if (color.IsNamedColor)
			//	return string.Format("{0}", color.Name);
			else
				return string.Format("{0};{1};{2};{3}", color.A, color.R, color.G, color.B);
		}

		public static Color DeserializeColor(this string color, Color defaultColor = default(Color))
		{
			if (String.IsNullOrEmpty(color))
				return defaultColor;

			byte a, r, g, b;

			string[] pieces = color.Split(new char[] { ';' });

			if (pieces.Length == 4)
			{
				if (!byte.TryParse (pieces [0], out a))
					a = 128;
				if (!byte.TryParse (pieces [1], out r))
					a = 128;
				if (!byte.TryParse (pieces [2], out g))
					a = 128;
				if (!byte.TryParse (pieces [3], out b))
					a = 128;
				return Color.FromArgb(a, r, g, b);
			}
			else if (pieces.Length == 1)
			{
				try {
					return Color.FromName(pieces[0]);	
				} catch (Exception) {
					return defaultColor;
				}
			}
			else
			{
				return defaultColor;
			}
		}			

		public static string EscapeJson(this IEnumerable<string> values)
		{
			if (values == null)
				return String.Empty;
			return String.Join(",", values.Select(v => v.EscapeJson()));
		}

		public static string EscapeJson(this bool value)
		{
			return value.ToString ().ToLowerInvariant().EscapeJson (false);
		}

		public static string EscapeJson(this byte value)
		{
			return value.ToString ().EscapeJson (false);
		}

		public static string EscapeJson(this int value)
		{
			return value.ToString ().EscapeJson (false);
		}

		public static string EscapeJson(this float value)
		{
			return value.ToString ().EscapeJson (false);
		}

		public static string EscapeJson(this double value)
		{
			return value.ToString ().EscapeJson (false);
		}

		public static string EscapeJson(this long value)
		{
			return value.ToString ().EscapeJson (false);
		}			

		public static string EscapeJson(this DateTime value)
		{							
			//return new DateTime (value.Ticks, DateTimeKind.Unspecified).ToString ("G").EscapeJson();
			return value.FormatUtcServerString().EscapeJson(true);
		}

		public static string EscapeJson(this TimeSpan value)
		{
			return value.ToString ().EscapeJson (true);
		}

		public static string EscapeJson(this Enum value)
		{
			return value.ToString ().EscapeJson (true);
		}

		public static string EscapeJson(this Color value)
		{
			return value.SerializeColor ().EscapeJson (true);
		}

		public static string EscapeJson(this Font value)
		{
			if (value == null)
				return "\"\"";
			XmlFont font = new XmlFont (value);
			return font.ToString().EscapeJson (false);
		}
			
		public static string EscapeJson(this object value)
		{
			if (value == null)
				return "\"\"";

			switch (value.GetType ().Name) {
			case "String":
				return ((string)value).EscapeJson ();
			case "DateTime":
				return ((DateTime)value).EscapeJson ();
			case "Int32":
				return ((long)value).EscapeJson ();
			case "Int64":
				return ((int)value).EscapeJson ();
			case "Boolean":
				return ((bool)value).EscapeJson ();
			case "Double":
				return ((double)value).EscapeJson ();
			case "Single":
				return ((float)value).EscapeJson ();
			case "Byte":
				return ((byte)value).EscapeJson ();
			}

			return value.ToString ().EscapeJson ();
		}

		public static string EscapeJson(this string value, bool doublequote = true)
		{
			if (value == null) {
				if (doublequote)
					return "\"\"";
				return String.Empty;
			}
			StringBuilder sb = new StringBuilder ();
			if (doublequote)
				sb.Append ('"');
			for (int i = 0; i < value.Length; i++) {
				char c = value [i];
				switch (c) {
				case '\\':
				case '"':
					sb.Append ('\\');
					sb.Append (c);
					break;
				case '\t':
					sb.Append ("\\t");
					break;
				case '\r':
					sb.Append ("\\r");
					break;
				case '\n':
					sb.Append ("\\n");
					break;
				default:
					if (c > 31)
						sb.Append (c);
					else {
						sb.Append ("\\u");
						sb.Append (String.Format ("{0:x4}", (int)c));
					}
					break;
				}
			}
			if (doublequote)
				sb.Append ('"');
			return sb.ToString ();

		}
	}
}

