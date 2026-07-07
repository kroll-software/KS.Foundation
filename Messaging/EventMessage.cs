using System;

namespace KS.Foundation
{	
	public struct EventMessage : IFoundationMessage, IEquatable<EventMessage>
	{	
		string m_Subject;
		public string Subject {
			get{
				return m_Subject;
			}
		}
			
		public readonly object Sender;
		public readonly object[] Args;
		public readonly DateTime Timestamp;

		public EventMessage(object sender, string subject, bool reenqueue = false, params object[] args)
		{			
			Sender = sender;
			m_Subject = subject;
			m_ShouldReEnqueue = reenqueue;
			Args = args;
			Timestamp = DateTime.Now;
		}

		bool m_ShouldReEnqueue;
		public bool ShouldReEnqueue
		{
			get
			{
				return m_ShouldReEnqueue;
			}
		}

		public override bool Equals (object obj)
		{
			return (obj is EventMessage) &&  Equals ((EventMessage)obj);
		}

		public bool Equals (EventMessage other)
		{
			return Subject == other.Subject && Timestamp == other.Timestamp && Sender == other.Sender;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return (int)Timestamp.Ticks ^ (Subject.SafeHash () + 31);
			}
		}
	}		
}

