using System;
using System.Linq;
using System.Collections.Generic;
using Pfz.Collections;
using System.Reflection;
using KS.Foundation;

namespace KS.Foundation
{
	public interface IController
	{
		IController Parent { get; }
		IRootController Root { get; }

		T AddSubController<T> (T sub) where T : IController;
		bool RemoveSubController (Type type);
		bool RemoveSubController<T> ();
		bool GetSubController(Type type, out IController sub);
		bool GetSubController<T> (out T sub) where T : IController;
		IController this [Type t] { get; }

		void Suspend ();
		void Resume (bool reset = false);
		bool IsSuspended { get; }

		void StateChanged (object oldState, object newState);
		void StateChanged<T> (T oldState, T newState);

		void Reset();
	}

	public interface IRootController : IObservable<EventMessage>
	{
		MessageQueue<EventMessage> Messages { get; }
		void SendMessage(EventMessage message);
		void SendMessage (object sender, string subject, bool reenqueue = false, params object[] args);
	}
		
	public interface IRootControllerObserver
	{
		IObserver<EventMessage> RootControllerObserver { get; }
	}


	public class RootController : Controller, IRootController, IObservable<EventMessage>
	{
		public MessageQueue<EventMessage> Messages { get; private set; }
		public IDisposable Subscribe(IObserver<EventMessage> observer)
		{
			return Messages.Subscribe (observer);
		}

		public RootController() : base(null)
		{			
			Messages = new MessageQueue<EventMessage> (true);
		}
					
		public void SendMessage(EventMessage message)
		{
			Messages.SendMessage (message);
		}

		public void SendMessage(object sender, string subject, bool reenqueue = false, params object[] args)
		{
			Messages.SendMessage (new EventMessage(sender, subject, reenqueue, args));
		}

		protected override void CleanupManagedResources ()
		{
			Messages.Dispose ();
			base.CleanupManagedResources ();
		}
	}

	public abstract class Controller : DisposableObject, IController
	{
		public virtual IController Parent { get; private set; }
		public ThreadSafeDictionary<Type, IController> Children { get; private set; }

		protected Controller (IController parent)
		{
			Parent = parent;
			Children = new ThreadSafeDictionary<Type, IController> ();
		}

		public virtual IRootController Root
		{
			get{
				IController p = this;
				while (p.Parent != null)
					p = p.Parent;
				return p as IRootController;
			}
		}

		public T AddSubController<T>(T sub) where T : IController
		{
			if (Children.TryAdd (sub.GetType (), sub)) {
				return sub;
			}
			return default(T);
		}

		public bool RemoveSubController(Type type)
		{
			return Children.Remove (type);
		}

		public bool RemoveSubController<T>()
		{
			return Children.Remove (typeof(T));
		}
			
		public bool GetSubController<T>(out T sub) where T : IController
		{
			IController temp;
			if (GetSubController (typeof(T), out temp)) {
				sub = (T)temp;
				return true;
			}
			sub = default(T);
			return false;
		}

		public bool GetSubController(Type type, out IController sub)
		{
			if (Children.TryGetValue (type, out sub))
				return true;
			foreach (IController c in Children.Values) {
				if (c.GetSubController (type, out sub))
					return true;
			}
			sub = null;
			return false;
		}

		public IController this [Type t]
		{
			get{
				IController c;
				if (GetSubController (t, out c))
					return c;
				return null;
			}
		}

		int iSuspensionsCounter;
		public void Suspend()
		{
			iSuspensionsCounter++;
		}

		public void Resume(bool reset = false)
		{
			if (reset)
				iSuspensionsCounter = 0;
			else if (iSuspensionsCounter > 0)
				iSuspensionsCounter--;
		}

		public bool IsSuspended
		{
			get {
				return iSuspensionsCounter > 0 || (Parent != null && Parent.IsSuspended);
			}
		}

		protected virtual void OnStateChanged<T>(T oldState, T newState)
		{			
		}

		public void StateChanged(object oldState, object newState)
		{
			OnStateChanged (oldState, newState);
			Children.Values.ForEach (sub => sub.StateChanged(oldState, newState));
		}

		public void StateChanged<T>(T oldState, T newState)
		{
			OnStateChanged<T> (oldState, newState);
			Children.Values.ForEach (sub => sub.StateChanged<T>(oldState, newState));
		}

		protected virtual void OnReset()
		{			
		}

		public void Reset()
		{
			OnReset ();
			Children.Values.ForEach (sub => sub.Reset());
		}

		protected override void CleanupManagedResources ()
		{
			Parent = null;
			Children.Values.DisposeListObjects ();
			Children.Clear ();
			base.CleanupManagedResources ();
		}
	}
}

