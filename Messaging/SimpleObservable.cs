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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pfz.Collections;

namespace KS.Foundation
{    
	public class Observable<T> : DisposableObject, IObservable<T>
	{
		// be tolerant accinential double subsriptions
		// in both: observables and observers

		protected ThreadSafeHashSet<IObserver<T>> m_HashObservers;

		public Observable()
		{        
			m_HashObservers = new ThreadSafeHashSet<IObserver<T>>();
		}

		public int ObserverCount
		{
			get
			{
				return m_HashObservers.Count;
			}
		}


		public IDisposable Subscribe(IObserver<T> observer)
		{
			//lock (SyncObject)
			//{                

			try
			{
				if (observer == null || IsDisposed || m_HashObservers.Contains(observer))
					return null;

				m_HashObservers.Add(observer);                
				return new Unsubscriber<T>(this, observer);
			}
			catch (Exception ex)
			{
				ex.LogError();
				throw;
			}

			//}
		}

		public void Unsubscribe(IObserver<T> observer)
		{
			//lock (SyncObject)
			//{
			try
			{
				if (observer != null && m_HashObservers.Contains(observer))
					m_HashObservers.Remove(observer);                
			}
			catch (Exception ex)
			{
				ex.LogError();
			}

			//}
		}

		public virtual void InvokeSendMessage(T message)
		{
			if (!IsDisposed && !message.IsDefault()) {
				Task.Factory.StartNew (() => SendMessage (message), 
					System.Threading.CancellationToken.None, 
					TaskCreationOptions.PreferFairness, 
					TaskScheduler.Default);
			}
		}

		public virtual void SendMessage(T message)
		{			
			if (!IsDisposed && !message.IsDefault ()) {				
				// no locking here, this deadlocks when locked.
				try {				
					m_HashObservers.ToArray ().Where (obs => obs != null).ForEach (observer => {                    
						try {
							observer.OnNext (message);
						} catch (Exception ex) {
							ex.LogError ();
						}
					});
				} catch (Exception ex) {
					ex.LogError ();
				}
			}
		}

		public virtual void SendError(Exception exception)
		{
			if (!IsDisposed && exception != null) {
				try {
					m_HashObservers.ToArray ().Where (obs => obs != null).ForEach (o => {
						try {
							o.OnError (exception);
						} catch (Exception ex) {
							ex.LogError ();
						}
					});
				} catch (Exception ex) {
					ex.LogError ();
				}
			}
		}

		public virtual void SendCompleted()
		{
			if (!IsDisposed) {
				try {
					m_HashObservers.ToArray ().Where (obs => obs != null).ForEach (o => {
						try {
							o.OnCompleted ();	
						} catch (Exception ex) {
							ex.LogError();
						}
					});
				} catch (Exception ex) {
					ex.LogError ();
				}
			}
		}

		protected override void CleanupManagedResources()
		{
			base.CleanupManagedResources();

			try
			{
				if (m_HashObservers != null)
					m_HashObservers.Clear();
			}
			catch (Exception ex)
			{
				ex.LogError();                
			}     				
		}			
	}    

	public class Unsubscriber<T> : DisposableObject
	{
		public Observable<T> Observable { get; protected set; }
		public IObserver<T> Observer { get; protected set; }

		public Unsubscriber(Observable<T> observable, IObserver<T> observer)
		{
			Observable = observable;
			Observer = observer;            
		}

		protected override void CleanupManagedResources()
		{
			if (Observable != null && !Observable.IsDisposed && Observer != null)
				Observable.Unsubscribe(Observer);

			base.CleanupManagedResources();
		}

		protected override void CleanupUnmanagedResources()
		{
			base.CleanupUnmanagedResources();
			Observable = null;
			Observer = null;
		}
	}

	public class Observer<T> : DisposableObject, IObserver<T>  //, IKeyedObject
	{
		Unsubscriber<T> m_Unsubscriber = null;

		//public string Key { get; set; }

		public Observer()
		{				
		}

		public Observer(Action<T> onNextAction, Action<Exception> onErrorAction, Action onCompletedAction)
			: this()
		{
			OnNextAction = onNextAction;
			OnErrorAction = onErrorAction;
			OnCompletedAction = onCompletedAction;
		}

		public Action<T> OnNextAction { get; set; }
		public Action<Exception> OnErrorAction { get; set; }
		public Action OnCompletedAction { get; set; }

		public void OnNext(T value)
		{				
			if (!IsDisposed && OnNextAction != null)
				OnNextAction(value);			
		}

		public void OnError(Exception error)
		{
			if (!IsDisposed && OnErrorAction != null) {
				OnErrorAction (error);
			}
		}

		public void OnCompleted()
		{
			if (!IsDisposed && OnCompletedAction != null) {
				OnCompletedAction ();
				/****
					Task.Run ((() => {
						T v;
						while (MessageQueue.Dequeue (out v)) {
							OnNextAction(v);
						}
					}));
					**/
			}
		}

		public void Subscribe(IObservable<T> observable)
		{
			if (IsDisposed || (m_Unsubscriber != null && !m_Unsubscriber.IsDisposed && m_Unsubscriber.Observable == observable))
				return;
			Unsubscriber<T> unsub = observable.Subscribe(this) as Unsubscriber<T>;
			if (unsub != null) {
				m_Unsubscriber = unsub;
			}
		}

		public void Unsubscribe(IObservable<T> observable)
		{
			if (!IsDisposed && m_Unsubscriber != null && !m_Unsubscriber.IsDisposed)
			{
				m_Unsubscriber.Dispose();
				//m_Unsubscriber = null;
			}
		}

		protected override void CleanupManagedResources()
		{
			if (m_Unsubscriber != null && !m_Unsubscriber.IsDisposed)
				m_Unsubscriber.Dispose();				
			base.CleanupManagedResources();
		}

		protected override void CleanupUnmanagedResources()
		{
			base.CleanupUnmanagedResources();
			m_Unsubscriber = null;
		}
	}


	public class QueuedObserver<T> : DisposableObject, IObserver<T>  //, IKeyedObject
	{
		Unsubscriber<T> m_Unsubscriber = null;

		//public string Key { get; set; }

		public QueuedObserver()
		{
			MessageQueue = new BlockingQueue<T>();
		}

		public QueuedObserver(Action<T> onNextAction, Action<Exception> onErrorAction, Action onCompletedAction)
			: this()
		{
			OnNextAction = onNextAction;
			OnErrorAction = onErrorAction;
			OnCompletedAction = onCompletedAction;
		}

		public Action<T> OnNextAction { get; set; }
		public Action<Exception> OnErrorAction { get; set; }
		public Action OnCompletedAction { get; set; }

		readonly BlockingQueue<T> MessageQueue;

		public void OnNext(T value)
		{
			if (!IsDisposed && OnNextAction != null) {
				if (MessageQueue.Enqueue (value)) {
					Task.Run ((() => {
						T v;
						while (MessageQueue.Dequeue (out v)) {
							OnNextAction (v);
						}
					}));
				} else {
					this.LogWarning ("Message was not enqueued !");
				}
			}
		}

		public void OnError(Exception error)
		{
			if (!IsDisposed && OnErrorAction != null) {
				OnErrorAction (error);
				MessageQueue.Clear ();
			}
		}

		public void OnCompleted()
		{
			if (!IsDisposed && OnCompletedAction != null) {					
				T v;
				while (MessageQueue.Dequeue(out v)) {
					OnNextAction(v);
				}
				OnCompletedAction ();
			}
		}

		public void Subscribe(IObservable<T> observable)
		{
			if (IsDisposed || (m_Unsubscriber != null && !m_Unsubscriber.IsDisposed && m_Unsubscriber.Observable == observable))
				return;

			Unsubscriber<T> unsub = observable.Subscribe(this) as Unsubscriber<T>;
			if (unsub != null) {
				m_Unsubscriber = unsub;
			}
			MessageQueue.ResetStart ();
		}

		public void Unsubscribe(IObservable<T> observable)
		{
			if (!IsDisposed && m_Unsubscriber != null && !m_Unsubscriber.IsDisposed)
			{
				m_Unsubscriber.Dispose();
				//m_Unsubscriber = null;
				MessageQueue.Quit();
				MessageQueue.Clear();
			}
		}

		protected override void CleanupManagedResources()
		{
			if (m_Unsubscriber != null && !m_Unsubscriber.IsDisposed)
				m_Unsubscriber.Dispose();
			MessageQueue.Quit ();
			MessageQueue.Clear ();
			base.CleanupManagedResources();
		}

		protected override void CleanupUnmanagedResources()
		{
			base.CleanupUnmanagedResources();
			m_Unsubscriber = null;
		}
	}

	public class MultiObserver<T> : DisposableObject, IObserver<T>
	{            
		readonly ThreadSafeDictionary<Observable<T>, Unsubscriber<T>> m_DictObservables;

		public MultiObserver()
		{
			m_DictObservables = new ThreadSafeDictionary<Observable<T>, Unsubscriber<T>>();
		}

		public MultiObserver(Action<T> onNextAction, Action<Exception> onErrorAction, Action onCompletedAction)
			: this()
		{
			OnNextAction = onNextAction;
			OnErrorAction = onErrorAction;
			OnCompletedAction = onCompletedAction;
		}

		public Action<T> OnNextAction { get; set; }
		public Action<Exception> OnErrorAction { get; set; }
		public Action OnCompletedAction { get; set; }

		public void OnNext(T value)
		{
			if (OnNextAction != null && !IsDisposed)
				OnNextAction(value);
		}

		public void OnError(Exception error)
		{
			if (OnErrorAction != null && !IsDisposed)
				OnErrorAction(error);
		}

		public void OnCompleted()
		{
			if (OnCompletedAction != null && !IsDisposed)
				OnCompletedAction();
		}

		public void Subscribe(Observable<T> observable)
		{
			//lock (SyncObject)
			//{                    
			if (observable == null || observable.IsDisposed || this.IsDisposed)
				return;
			Unsubscriber<T> unsub = observable.Subscribe(this) as Unsubscriber<T>;
			if (unsub != null)
				m_DictObservables.TryAdd(observable, unsub);
			// }
		}

		public void Unsubscribe(Observable<T> observable)
		{
			//lock (SyncObject)
			//{
			if (observable == null)
				return;

			try
			{                        
				Unsubscriber<T> target = null;
				if (m_DictObservables.TryGetValue(observable, out target))
				{
					m_DictObservables.Remove(observable);
					if (target != null)
						target.Dispose();
				}
			}
			catch (Exception e)
			{
				e.LogError();
			}                    
			//}
		}

		public void UnsubscribeAll()
		{
			lock (SyncObject)
			{
				try
				{                        
					m_DictObservables.Values.OfType<Unsubscriber<T>>().ForEach(o => o.Dispose());
					m_DictObservables.Clear();
				}
				catch (Exception e)
				{
					e.LogError();
				}                    
			}
		}            

		protected override void CleanupManagedResources()
		{
			try
			{
				if (m_DictObservables != null && m_DictObservables.Count > 0)
				{
					m_DictObservables.Values.DisposeListObjects();                        
				}
				//    m_DictObservables.Values.OfType<Observable<T>.Unsubscriber>().Where(o => !o.IsDisposed).ForEach(o => o.Dispose());
			}
			catch (Exception e)
			{
				e.LogError();
			}                

			base.CleanupManagedResources();
		}
	}


	public class QueuedMultiObserver<T> : DisposableObject, IObserver<T>
	{            
		readonly ThreadSafeDictionary<Observable<T>, Unsubscriber<T>> m_DictObservables;
		readonly BlockingQueue<T> MessageQueue;

		public QueuedMultiObserver()
		{
			m_DictObservables = new ThreadSafeDictionary<Observable<T>, Unsubscriber<T>>();
			MessageQueue = new BlockingQueue<T>();
		}

		public QueuedMultiObserver(Action<T> onNextAction, Action<Exception> onErrorAction, Action onCompletedAction)
			: this()
		{
			OnNextAction = onNextAction;
			OnErrorAction = onErrorAction;
			OnCompletedAction = onCompletedAction;
		}

		public Action<T> OnNextAction { get; set; }
		public Action<Exception> OnErrorAction { get; set; }
		public Action OnCompletedAction { get; set; }

		public void OnNext(T value)
		{
			if (OnNextAction != null && !IsDisposed) {
				if (MessageQueue.Enqueue (value)) {
					Task.Run (() => {
						T v;
						while (MessageQueue.Dequeue (out v)) {
							OnNextAction (v);
						}
					});
				}
			}
		}

		public void OnError(Exception error)
		{
			if (OnErrorAction != null && !IsDisposed) {
				OnErrorAction (error);
				MessageQueue.Clear ();
			}
		}

		public void OnCompleted()
		{
			if (OnCompletedAction != null && !IsDisposed) {
				Task.Run ((() => {
					T v;
					while (MessageQueue.Dequeue (out v)) {
						OnNextAction(v);
					}
				}));
				OnCompletedAction ();
			}
		}

		public void Subscribe(Observable<T> observable)
		{						
			if (observable == null || observable.IsDisposed || this.IsDisposed)
				return;
			if (m_DictObservables.Count == 0)
				MessageQueue.ResetStart();
			Unsubscriber<T> unsub = observable.Subscribe(this) as Unsubscriber<T>;
			if (unsub != null)
				m_DictObservables.TryAdd(observable, unsub);				
		}

		public void Unsubscribe(Observable<T> observable)
		{
			//lock (SyncObject)
			//{
			if (observable == null)
				return;
			try
			{                        
				Unsubscriber<T> target = null;
				if (m_DictObservables.TryGetValue(observable, out target))
				{
					m_DictObservables.Remove(observable);
					if (target != null)
						target.Dispose();
				}
				if (m_DictObservables.Count == 0) {
					MessageQueue.Quit();
					MessageQueue.Clear();
				}
			}
			catch (Exception e)
			{
				e.LogError();
			}                    
			//}
		}

		public void UnsubscribeAll()
		{
			lock (SyncObject)
			{
				try
				{                        
					m_DictObservables.Values.OfType<Unsubscriber<T>>().ForEach(o => o.Dispose());
					m_DictObservables.Clear();
					MessageQueue.Quit();
					MessageQueue.Clear();
				}
				catch (Exception e)
				{
					e.LogError();
				}                    
			}
		}            

		protected override void CleanupManagedResources()
		{
			try
			{
				if (m_DictObservables != null && m_DictObservables.Count > 0)
				{
					m_DictObservables.Values.DisposeListObjects();                        
				}
				//    m_DictObservables.Values.OfType<Observable<T>.Unsubscriber>().Where(o => !o.IsDisposed).ForEach(o => o.Dispose());
				MessageQueue.Quit();
				MessageQueue.Clear();
			}
			catch (Exception e)
			{
				e.LogError();
			}                

			base.CleanupManagedResources();
		}
	}
}
