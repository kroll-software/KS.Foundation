// Found at
// http://blog.abodit.com/2012/01/a-simple-state-machine-in-c/
// As stated on the website:
// "If you want to use the actual state machine in any of your own projects (gratis), here the current code:"
//
// Kroll says: Thank you for sharing this great work!


using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace KS.Foundation
{

	public interface IStateful<T> where T : IComparable
	{
		T State { get; set; }
	}

    /// <summary>
    /// A state machine allows you to track state and to take actions when states change
    /// This state machine provides a fluent interface for defining states and transitions
    /// </summary>
    /// <remarks>
    /// Nasty generic of self so we can refer to the inheriting class in here
    /// </remarks>    
    [DebuggerDisplay("Current State = {CurrentState.Name}")]
    public abstract class StateMachine<T> where T:StateMachine<T>
    {
        public State CurrentState { get; private set; }
        public State PreviousState { get; private set; }
 
        protected StateMachine(State initial)
        {
            CurrentState = initial;
            PreviousState = initial;
        }
 
        /// <summary>
        /// An event has happened, transition to next state
        /// </summary>
        public void EventHappens(Event @event, object args = null)
        {
            //System.Diagnostics.Debug.WriteLine(@event, "StateMachine: EventHappens");
            //System.Diagnostics.Debug.WriteLine(args, "StateMachine: args");

            PreviousState = CurrentState;
            CurrentState = CurrentState.OnEvent((T)this, @event, args);
        }
 
        /// <summary>
        /// An event that causes the state machine to transition to a new state
        /// </summary>
        /// <remarks>
        /// Defined as a nested class so that this state machine's events can only be used with it
        /// </remarks>
        [DebuggerDisplay("Event = {Name}")]
        public class Event
        {
            public string Name { get; private set; }            
            public Event(string name)
            {
                this.Name = name;                
            }
            public override string ToString()
            {
                return "~" + this.Name + "~";
            }
        }
 
        /// <summary>
        /// A state that the state machine can be in
        /// </summary>
        /// <remarks>
        /// Defined as a nested class so that this state machine's states can only be used with it
        /// </remarks>
        [DebuggerDisplay("State = {Name}")]
        public class State
        {           
            /// <summary>
            /// The Name of this state
            /// </summary>
            public string Name { get; private set; }
 
            public Action<T, State, Event, object> ExitAction { get; private set; }
            public Action<T, Event, State, object> EntryAction { get; private set; }
			            
            private readonly IDictionary<Event, Func<T, State, Event, State>> transitions = new Dictionary<Event, Func<T, State, Event, State>>();
 
            /// <summary>
            /// Create a new State with a name and an optional entry and exit action
            /// </summary>
            public State(string name, Action<T, Event, State, object> entryAction = null, Action<T, State, Event, object> exitAction = null)
            {
                this.Name = name;
                this.EntryAction = entryAction;
                this.ExitAction = exitAction;                
            }
 
            public State When(Event @event, Func<T, State, Event, State> action)
            {
                transitions.Add(@event, action);
                return this;
            }

            public State OnEvent(T parent, Event @event, object args = null)
            {
                //System.Diagnostics.Debug.WriteLine(@event, "StateMachine OnEvent");

                Func<T, State, Event, State> transition = null;
                if (transitions.TryGetValue(@event, out transition))
                {
                    State newState = transition(parent, this, @event);
                    if (newState != this)
                    {
                        // Entry and exit actions only fire when CHANGING state
                        if (this.ExitAction != null) this.ExitAction(parent, this, @event, args);
                        if (newState.EntryAction != null) newState.EntryAction(parent, @event, newState, args);
                    }
                    else
                    {
						this.LogDebug ("STATEMACHINE event {0}: NewState == OldState, No action was taken.", @event);
                    }
                    return newState;
                }
                else					
                {
					this.LogDebug ("STATEMACHINE event {0}, CurrentState: {1}: UNHANDLED TRANSITION", @event);
                    return this;        // did not change state
                }
            }
 
            public override string ToString()
            {
                return "*" + this.Name + "*";
            }
        }
    }
}



//// Sample

// public class LoginOutStatemachine : StateMachine<LoginOutStatemachine>
//{
//    public static void ReportEnter(LoginOutStatemachine m, Event e, State state)
//    {
//        Console.WriteLine(m.User + " entered state " + state + " via " + e);
//    }
 
//    public static void ReportLeave(LoginOutStatemachine m, State state, Event e)
//    {
//        Console.WriteLine(m.User + " left state " + state + " via " + e);
//    }
 
//    public static State Initial = new State("Initial", ReportEnter, ReportLeave);
//    public static State LoggedIn = new State("Logged In", ReportEnter, ReportLeave);
//    public static State LoggedOut = new State("Logged Out", ReportEnter, ReportLeave);
//    public static State Deleted = new State("Deleted", ReportEnter, ReportLeave);
 
//    private static Event eLogsIn = new Event("Logs In");
//    private static Event eLogsOut = new Event("Logs Out");
//    private static Event eDeletesAccount = new Event("Account Deleted");
 
//    static LoginOutStatemachine()
//    {
//        Initial
//                .When(eLogsIn, (m, s, e) => { Console.WriteLine("Logging in " + m.User); return LoggedIn; })
//                .When(eDeletesAccount, (m, s, e) => { Console.WriteLine("Deleting account " + m.User); return Deleted; });
//        LoggedIn
//                .When(eLogsOut, (m, s, e) => { Console.WriteLine("Logging out " + m.User); return LoggedOut; })
//                .When(eDeletesAccount, (m, s, e) => { Console.WriteLine("Account deleted " + m.User); return Deleted; });
//        LoggedOut
//                .When(eLogsIn, (m, s, e) => { Console.WriteLine("Logging in " + m.User); return LoggedIn; })
//                .When(eDeletesAccount, (m, s, e) => { Console.WriteLine("Account deleted " + m.User); return Deleted; });
//    }
 
//    public User User { get; private set; }
 
//    public LoginOutStatemachine(State initial, User user)
//        : base(initial)
//    {
//        this.User = user;
//    }
 
//    // Expose the events as public methods
 
//    public void LogsIn()
//    {
//        this.EventHappens(eLogsIn);
//    }
 
//    public void LogsOut()
//    {
//        this.EventHappens(eLogsOut);
//    }
 
//    public void DeletesAccount()
//    {
//        this.EventHappens(eDeletesAccount);
//    }
//}
