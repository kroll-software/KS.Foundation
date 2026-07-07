using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
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

		public static LogMessage FromJson(string json, JsonSerializerOptions options = null)
		{
			if (string.IsNullOrEmpty(json))
				return Empty;

			return JsonSerializer.Deserialize<LogMessage>(json, options ?? DefaultJsonOptions);
		}

		public static readonly JsonSerializerOptions DefaultJsonOptions = new JsonSerializerOptions
		{
			Converters = { new ObjectToInferredTypesConverter() },
			PropertyNameCaseInsensitive = true
		};

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

