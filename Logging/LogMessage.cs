using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using KS.Foundation;

namespace KS.Foundation
{	
	public struct LogMessage
	{		
		public readonly LogLevels Level;
		public readonly string Text;
		//public readonly string[] Args;
		public readonly object[] Args;
		public readonly DateTime Timestamp;

		public static readonly LogMessage Empty = new LogMessage (0, null, DateTime.MinValue);

		//public LogMessage(LogLevels level, string text, DateTime timestamp = default(DateTime), params string[] args)
		public LogMessage(LogLevels level, string text, DateTime timestamp = default(DateTime), params object[] args)
		{						
			Level = level;
			Text = text;
			if (timestamp == default(DateTime))
				timestamp = DateTime.UtcNow;
			Timestamp = timestamp;
			Args = args;
		}			

		public static LogMessage FromJson(string json)
		{
			if (String.IsNullOrEmpty (json))
				return Empty;

			JToken token = JToken.Parse (json);
			if (token == null)
				return Empty;

			return FromJsonToken (token);
		}

		public string FormattedMessage
		{
			get{
				if (Args.IsNullOrEmpty())
					return Text;

				try {
					return (String.Format(Text, Args));	
				} catch (Exception) {
					return Text;
				}
			}
		}

		public static LogMessage FromJsonToken(JToken token)
		{
			if (token == null)
				return Empty;

			/***
			List<object> args = new List<object> ();
			//token.Value<JArray> ("Args").Do (arr => arr.ForEach(r => args.Add(r.ToString(Formatting.None))));
			token.Value<JArray> ("Args").Do (arr => arr.ForEach(r => 
				args.Add(r.Value<object>())));
			***/

			object[] args = null;
			token.Value<JArray> ("Args").Do(a => args = a.ToString(Formatting.None).FromJson());	

			return new LogMessage (
				token.Value<string>("Level").ToEnum(LogLevels.None),
				token.Value<string>("Text"),
				token.Value<DateTime>("Timestamp"),
				args != null ?  args.ToArray() : null
			);
		}
			
		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ("{");
			sb.Append("\"$type\":\"LogMessage\",");
			sb.AppendFormat("\"Level\":{0},", Level.EscapeJson());
			sb.AppendFormat("\"Text\":{0},", Text.EscapeJson());
			if (!Args.IsNullOrEmpty ()) {				
				//sb.AppendFormat ("\"Args\":[{0}],", String.Join (",", Args.Select (arg => arg.EscapeJson())));
				//string json = Args.ToJson(Args);
				sb.AppendFormat("\"Args\":{0},", Args.ToJson());
			}
			sb.AppendFormat("\"Timestamp\":{0}", Timestamp.EscapeJson());
			sb.Append ("}");

			return sb.ToString ();
		}
	}
}

