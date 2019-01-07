using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using KS.Foundation;
using KS.Foundation.ECS;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KS.Foundation
{
	public struct XmlFont
	{
		public string FontFamily;
		[JsonConverter(typeof(StringEnumConverter))]
		public GraphicsUnit GraphicsUnit;
		public float Size;
		[JsonConverter(typeof(StringEnumConverter))]
		public FontStyle Style;

		public XmlFont(Font f)
		{
			if (f == null)
			{
				FontFamily = null;
				GraphicsUnit = GraphicsUnit.Pixel;
				Size = 0f;
				Style = FontStyle.Regular;
			}
			else
			{
				FontFamily = f.FontFamily.Name;
				GraphicsUnit = f.Unit;
				Size = f.Size;
				Style = f.Style;
			}
		}

		public Font ToFont()
		{
			if (String.IsNullOrEmpty(FontFamily))
				return null;
			else
				return new Font(FontFamily, Size, Style, GraphicsUnit);
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ("\"Font\":{");
			sb.AppendFormat ("\"FontFamily\":{0},", this.FontFamily.EscapeJson());
			sb.AppendFormat ("\"GraphicsUnit\":{0},", this.GraphicsUnit.EscapeJson());
			sb.AppendFormat ("\"Size\":{0},", this.Size.EscapeJson());
			sb.AppendFormat ("\"Style\":{0}", this.Style.EscapeJson());
			sb.Append ("}");
			return sb.ToString ();
		}
	}
}

