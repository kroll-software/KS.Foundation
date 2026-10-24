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
    /// <summary>
    /// Marker: Laeuft der aktuelle Thread in einem Message-Worker (WorkerLoop von
    /// QueuedObserver/QueuedMultiObserver)? Ruft ein Handler reentrans
    /// InvokeSendMessage, wird die Nachricht DIREKT enqueued statt ueber Task.Run
    /// (ThreadPool) — so entsteht kein "Thread-Flood" bei reentranten Haendlern (S10).
    /// </summary>
    internal static class MessageWorkerContext
    {
        internal static readonly ThreadLocal<bool> OnWorker = new ThreadLocal<bool> { Value = false };
    }

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
			/***
			Task.Factory.StartNew (() => SendMessage (message),
				System.Threading.CancellationToken.None,
				TaskCreationOptions.PreferFairness,
				TaskScheduler.Default);
			***/

				if (MessageWorkerContext.OnWorker.Value) {
					//Schon IN einem Message-Worker (reentrante Handler-Aufrufe):
					//  direkt enqueuen — kein neuer ThreadPool-Thread pro Nachricht
					//  (verhindert "Thread-Flood", S10). Der Worker pickt sie
					//  im naechsten Dequeue-Zyklus auf (keine Deadlock-Gefahr:
					//  Enqueue auf einer int.MaxValue-Queue blockiert nie).
					SendMessage (message);
				}
				else {
					Task.Run(() => SendMessage (message));
				}
			}
		}

		public virtual void SendMessage(T message)
        {           
            if (!IsDisposed && !message.IsDefault()) {             
                // no locking here, this deadlocks when locked.
                try {               
                    var observers = m_HashObservers.ToArray();
                    for (int i = 0; i < observers.Length; i++) {
                        var observer = observers[i];
                        if (observer == null) continue;

                        try {
                            observer.OnNext(message);
                        } catch (Exception ex) {
                            ex.LogError();
                        }
                    }
                } catch (Exception ex) {
                    ex.LogError();
                }
            }
        }

        public virtual void SendError(Exception exception)
        {
            if (!IsDisposed && exception != null) {
                try {
                    var observers = m_HashObservers.ToArray();
                    for (int i = 0; i < observers.Length; i++) {
                        var observer = observers[i];
                        if (observer == null) continue;

                        try {
                            observer.OnError(exception);
                        } catch (Exception ex) {
                            ex.LogError();
                        }
                    }
                } catch (Exception ex) {
                    ex.LogError();
                }
            }
        }

        public virtual void SendCompleted()
        {
            if (!IsDisposed) {
                try {
                    var observers = m_HashObservers.ToArray();
                    for (int i = 0; i < observers.Length; i++) {
                        var observer = observers[i];
                        if (observer == null) continue;

                        try {
                            observer.OnCompleted();   
                        } catch (Exception ex) {
                            ex.LogError();
                        }
                    }
                } catch (Exception ex) {
                    ex.LogError();
                }
            }
        }		

		protected override void CleanupManagedResources()
		{
			base.CleanupManagedResources();

			try
			{
                m_HashObservers?.Clear();
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

			//Ummelden: die ALTE Subscription muss aufgelöst werden — sonst laeuft der
			//  gemeinsame Observer an der alten Observable weiter (Referenz-Leck, S08).
			if (m_Unsubscriber != null && !m_Unsubscriber.IsDisposed)
				m_Unsubscriber.Dispose ();

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


		/// <summary>
	/// Queue-Nachricht der QueuedObserver-Familie.
	/// Traegt neben reinen Daten (Data) auch die Terminal-Marker Error/Completed als
	///  Envelope — dadurch feuern Abschluss-Events strikt in FIFO-Reihenfolge (nach
	///  allen zuvor gequeuezten Nachrichten) und es bedarf weder Polling noch
	///  Timeouts noch Datenverwerfung (kein Drop, kein Stillstand, S04/S06 beseitigt).
	/// </summary>
	public class QueuedMessage<T>
	{
		public enum Kind { Data, Error, Completed }

		public Kind MessageKind;
		public T Value;
		public Exception Error;

		public QueuedMessage()
		{
		}

		public QueuedMessage(Kind kind, T value, Exception error)
		{
			MessageKind = kind;
			Value = value;
			Error = error;
		}
	}

	/// <summary>
	/// Observer, der auf einer BlockingQueue laeuft: EIN permanenter Worker-Thread
	///  pro Instanz (bewusst keine Thread-Arme je Message). Der Worker blockiert
	///  wartungslos auf Dequeue(Infinite) — kein Polling (S04 beseitigt), kein
	///  Timeout (kein Datenverlust), kein Restart-Race (Lost-Wakeup beseitigt).
	/// OnCompleted/OnError werden als Envelope durch die Queue geschleust und feuern
	///  genau nach dem kompletten Backlog (FIFO, S06/S09 beseitigt). OnNext koennte
	///  auch von innen (im Worker-Thread) reentrant aufgerufen werden — dank
	///  unbegrenzter Queue-Kapazitaet blockiert Enqueue niemals auf freie Kapazitaet
	///  und t&ouml;tet damit keinen Self-Deadlock aus (S10 beseitigt).
	/// </summary>
	public class QueuedObserver<T> : DisposableObject, IObserver<T>  //, IKeyedObject
	{
		Unsubscriber<T> m_Unsubscriber = null;

		//public string Key { get; set; }

		//Unbegrenzte Kapazitaet: Ein Produzent — selbst reentrant im Worker-Thread —
		//  blockiert niemals in Enqueue auf freie Kapazitaet; der einzige Konsument
		//  waere sonst der gerade blockierte Worker selbst (Self-Deadlock, S09/S10).
		//  Backpressure ist damit Aufgabe der App; die Queue wirft nie Daten weg.
		private const int QueueCapacity = int.MaxValue;

		public QueuedObserver()
		{
			MessageQueue = new BlockingQueue<QueuedMessage<T>>(QueueCapacity);
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

		readonly BlockingQueue<QueuedMessage<T>> MessageQueue;

		private Thread m_Worker = null;	//EIN permanenter Worker pro Queue
		private int m_Terminated = 0;	//0 = aktiv, 1 = Error/Completed abgearbeitet

		public void OnNext(T value)
		{
			lock (SyncObject)
			{
				if (IsDisposed || OnNextAction == null || Volatile.Read (ref m_Terminated) == 1)
					return;	//Stream nach Error/Completed beendet: keine Zustellung mehr (Rx-Semantik)
				EnsureWorker ();
				if (!MessageQueue.Enqueue (new QueuedMessage<T> (QueuedMessage<T>.Kind.Data, value, null)))
					this.LogWarning ("Message was not enqueued !");
			}
		}

		public void OnError(Exception error)
		{
			lock (SyncObject)
			{
				if (IsDisposed || error == null || OnErrorAction == null || Volatile.Read (ref m_Terminated) == 1)
					return;
				EnsureWorker ();
				//Envelope: feuert im Worker strikt NACH dem kompletten Backlog (FIFO)
				MessageQueue.Enqueue (new QueuedMessage<T> (QueuedMessage<T>.Kind.Error, default (T), error));
				Volatile.Write (ref m_Terminated, 1);
			}
		}

		public void OnCompleted()
		{
			lock (SyncObject)
			{
				if (IsDisposed || OnCompletedAction == null || Volatile.Read (ref m_Terminated) == 1)
					return;
				EnsureWorker ();
				//Envelope: feuert im Worker strikt NACH dem kompletten Backlog (FIFO)
				MessageQueue.Enqueue (new QueuedMessage<T> (QueuedMessage<T>.Kind.Completed, default (T), null));
				Volatile.Write (ref m_Terminated, 1);
			}
		}

		//SyncObject muss (reentrant) gehalten sein
		private void EnsureWorker()
		{
			if (m_Worker != null && m_Worker.IsAlive)
				return;
			var worker = new Thread (WorkerLoop) { IsBackground = true, Name = "KS.QueuedObserver<T>-Worker" };
			m_Worker = worker;
			worker.Start ();
		}

		private void WorkerLoop()
		{
			//Marker: Dieser Thread ist ein Message-Worker (enablt den Inline-Pfad
			//  von InvokeSendMessage beim reentranten Handler, S10).
			MessageWorkerContext.OnWorker.Value = true;
			while (true)
			{
				QueuedMessage<T> msg;
				if (!DequeueSafe (out msg))
					break;	//Quit (Unsubscribe/Dispose) + leere Queue => sauberes Ende

				if (msg.MessageKind != QueuedMessage<T>.Kind.Data)
				{
					//Terminal-Marker: alles zuvor gequeuezte ist bereits zugestellt (FIFO).
					Volatile.Write (ref m_Terminated, 1);
					try {
						if (msg.MessageKind == QueuedMessage<T>.Kind.Error) {
							if (OnErrorAction != null)
								OnErrorAction (msg.Error);
						} else {
							OnCompletedAction ();
						}
					} catch (Exception ex) {
						ex.LogError ();	//auch ein Fehler im Abschluss-Callback darf den Worker nicht hängen lassen
					}
					break;	//Stream endet — der Worker verlasst die Queue
				}

				try {
					OnNextAction (msg.Value);
				} catch (Exception ex) {
					//Handler-Fehler duerfen den Worker nicht toeten — sonst: verwaiste Queue
					//  mit blockierten Sendern (S04/S08-Szenario). Worker faehrt weiter.
					ex.LogError ();
				}
			}
			MessageWorkerContext.OnWorker.Value = false;
		}

		private bool DequeueSafe(out QueuedMessage<T> msg)
		{
			try {
				return MessageQueue.Dequeue (out msg);
			} catch (Exception ex) {
				ex.LogError ();
				msg = null;
				return false;	//Wird wie 'Quit+leer' behandelt: Worker endet,
						//der nachste OnNext/Subscribe startet ihn neu (EnsureWorker) — kein Lost-Wakeup
			}
		}

		public void Subscribe(IObservable<T> observable)
		{
			lock (SyncObject)
			{
				if (observable == null || IsDisposed)
					return;
				if (m_Unsubscriber != null && !m_Unsubscriber.IsDisposed && m_Unsubscriber.Observable == observable)
					return;	//bereits an genau dieser Observable abgemeldet? — dann nichts tun

				//Ummelden: Die ALTE Subscription muss aufgel&ouml;st werden — sonst
				//  bleibt der gemeinsame Observer an der alten Observable hängen
				//  (Referenz-Leck, S08).
				if (m_Unsubscriber != null && !m_Unsubscriber.IsDisposed)
					m_Unsubscriber.Dispose ();

				Unsubscriber<T> unsub = observable.Subscribe (this) as Unsubscriber<T>;
				if (unsub == null)
					return;
				m_Unsubscriber = unsub;

				//Stream wieder aufnahmefertig machen (auch nach fr&uuml;herem Error/Completed):
				Volatile.Write (ref m_Terminated, 0);
				MessageQueue.ResetStart ();
				EnsureWorker ();
			}
		}

		public void Unsubscribe(IObservable<T> observable)
		{
			lock (SyncObject)
			{
				if (!IsDisposed && m_Unsubscriber != null && !m_Unsubscriber.IsDisposed)
				{
					//Zuerst Terminieren (blockiert jegliches neues Enqueue), DANN aufl&ouml;sen:
					Volatile.Write (ref m_Terminated, 1);
					m_Unsubscriber.Dispose ();
					MessageQueue.Quit ();
					MessageQueue.Clear ();
				}
			}
		}

		protected override void CleanupManagedResources()
		{
			Thread workerRef = null;
			lock (SyncObject)
			{
				if (m_Unsubscriber != null && !m_Unsubscriber.IsDisposed)
					m_Unsubscriber.Dispose ();
				MessageQueue.Quit ();
				MessageQueue.Clear ();
				Volatile.Write (ref m_Terminated, 1);
				workerRef = m_Worker;
				m_Worker = null;
			}
			base.CleanupManagedResources ();
			//Best-Effort: Worker beenden (Quit weckt ihn, leere Queue => Ende).
			//Nicht auf sich selbst Joinen (Reentrancy vom Worker aus: o2.Dispose()).
			if (workerRef != null && workerRef.IsAlive && workerRef.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
				workerRef.Join (5000);
		}

		protected override void CleanupUnmanagedResources()
		{
			m_Unsubscriber = null;
			base.CleanupUnmanagedResources ();
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


		/// <summary>
	/// Observer, der mehrere IObservable in EINER Queue mit EINEM permanenten Worker
	///  zusammenfasst (bewusst keine Thread-Arme je Message). Gleiche Protokoll-
	///  Eigenschaften wie QueuedObserver<T> (kein Polling, kein Timeout, kein Drop,
	///  Self-Deadlock-immun, S04/S06/S09/S10 beseitigt). Completion/OnError sind
	///  pro Observable, der Multi-Stream selbst bleibt aktiv.
	/// </summary>
	public class QueuedMultiObserver<T> : DisposableObject, IObserver<T>
	{
		//Die Observable, ihre Subscription und der Thread: alle drei gehören zusammen!
		readonly ThreadSafeDictionary<Observable<T>, Unsubscriber<T>> m_DictObservables;

		//Unbegrenzte Kapazitaet, Begr&uuml;ndung wie bei QueuedObserver<T>:
		private const int QueueCapacity = int.MaxValue;

		public QueuedMultiObserver()
		{
			m_DictObservables = new ThreadSafeDictionary<Observable<T>, Unsubscriber<T>>();
			MessageQueue = new BlockingQueue<QueuedMessage<T>>(QueueCapacity);
		}

		public QueuedMultiObserver(Action<T> onNext, Action<Exception> onError, Action onCompleted)
			: this()
		{
			OnNextAction = onNext;
			OnErrorAction = onError;
			OnCompletedAction = onCompleted;
		}

		public Action<T> OnNextAction { get; set; }
		public Action<Exception> OnErrorAction { get; set; }
		public Action OnCompletedAction { get; set; }

		readonly BlockingQueue<QueuedMessage<T>> MessageQueue;

		private Thread m_Worker = null;	//EIN permanenter Worker fuer alle subscribed Observables

		public void OnNext(T value)
		{
			lock (SyncObject)
			{
				if (IsDisposed || OnNextAction == null)
					return;
				EnsureWorker ();
				if (!MessageQueue.Enqueue (new QueuedMessage<T> (QueuedMessage<T>.Kind.Data, value, null)))
					this.LogWarning ("Message was not enqueued !");
			}
		}

		public void OnError(Exception error)
		{
			lock (SyncObject)
			{
				if (IsDisposed || error == null || OnErrorAction == null)
					return;
				EnsureWorker ();
				//Envelope: feuert im Worker nach dem Backlog. Bewusst NICHT terminal
				//  und kein Clear: andere subscribed Observables liefern weiter,
				//  keine Daten werden verworfen.
				MessageQueue.Enqueue (new QueuedMessage<T> (QueuedMessage<T>.Kind.Error, default (T), error));
			}
		}

		public void OnCompleted()
		{
			lock (SyncObject)
			{
				if (IsDisposed || OnCompletedAction == null)
					return;
				EnsureWorker ();
				//Envelope: feuert im Worker nach dem Backlog; der Multi-Stream bleibt aktiv.
				MessageQueue.Enqueue (new QueuedMessage<T> (QueuedMessage<T>.Kind.Completed, default (T), null));
			}
		}

		//SyncObject muss (reentrant) gehalten sein
		private void EnsureWorker()
		{
			if (m_Worker != null && m_Worker.IsAlive)
				return;
			var worker = new Thread (WorkerLoop) { IsBackground = true, Name = "KS.QueuedMultiObserver<T>-Worker" };
			m_Worker = worker;
			worker.Start ();
		}

		private void WorkerLoop()
		{
			//Marker: Dieser Thread ist ein Message-Worker (enablt den Inline-Pfad
			//  von InvokeSendMessage beim reentranten Handler, S10).
			MessageWorkerContext.OnWorker.Value = true;
			while (true)
			{
				QueuedMessage<T> msg;
				if (!DequeueSafe (out msg))
					break;	//Quit (Unsubscribe/UnsubscribeAll/Dispose) + leer => Ende

				try {
					if (msg.MessageKind == QueuedMessage<T>.Kind.Data)
					{
						OnNextAction (msg.Value);
					}
					else if (msg.MessageKind == QueuedMessage<T>.Kind.Error)
					{
						if (OnErrorAction != null)
							OnErrorAction (msg.Error);
						//kein break: Multi-Stream bleibt aktiv
					}
					else
					{
						if (OnCompletedAction != null)
							OnCompletedAction ();
						//kein break: Multi-Stream bleibt aktiv
					}
				} catch (Exception ex) {
					ex.LogError ();	//Handler-Fehler toeten den Worker nicht
				}
			}
			MessageWorkerContext.OnWorker.Value = false;
		}

		private bool DequeueSafe(out QueuedMessage<T> msg)
		{
			try {
				return MessageQueue.Dequeue (out msg);
			} catch (Exception ex) {
				ex.LogError ();
				msg = null;
				return false;	//Wie 'Quit+leer': Worker endet, naechster Subscribe
						//startet ihn neu (EnsureWorker) — kein Lost-Wakeup
			}
		}

		public void Subscribe(Observable<T> observable)
		{
			lock (SyncObject)
			{
				if (observable == null || IsDisposed)
					return;
				bool first = m_DictObservables.Count == 0;
				Unsubscriber<T> unsub = observable.Subscribe (this) as Unsubscriber<T>;
				if (unsub == null)
					return;
				m_DictObservables.TryAdd (observable, unsub);
				if (first)
					//Erste Subscription (oder Wiederaufnahme nach Unsubscribe/All):
					MessageQueue.ResetStart ();
				//no-op, falls der Worker noch l&auml;uft
				EnsureWorker ();
			}
		}

		/// <summary>Unsubscribes a subscriber from an <see cref="IObservable{T}"/> object.</summary>
		public void Unsubscribe(Observable<T> observable)
		{
			lock (SyncObject)
			{
				if (observable != null && m_DictObservables.ContainsKey(observable))
				{
					Unsubscriber<T> target = null;
					if (m_DictObservables.TryGetValue(observable, out target))
					{
						m_DictObservables.Remove(observable);
						if (target != null && !target.IsDisposed)
							target.Dispose ();
					}

					if (m_DictObservables.Count == 0)
					{
						MessageQueue.Quit();
						MessageQueue.Clear();
					}
				}
			}
		}

		/// <summary>Unsubscribes a subscriber from all <see cref="IObservable{T}"/> objects.</summary>
		public void UnsubscribeAll()
		{
			lock (SyncObject)
			{
				if (m_DictObservables.Count > 0)
				{
					foreach (var unsub in m_DictObservables)
						unsub.Value.Dispose();
					m_DictObservables.Clear ();
					MessageQueue.Quit();
					MessageQueue.Clear();
				}
			}
		}

		protected override void CleanupManagedResources()
		{
			Thread workerRef = null;
			lock (SyncObject)
			{
				foreach (var item in m_DictObservables)
					if (item.Value != null && !item.Value.IsDisposed)
						item.Value.Dispose ();
				MessageQueue.Quit();
				MessageQueue.Clear();
				workerRef = m_Worker;
				m_Worker = null;
			}
			base.CleanupManagedResources ();
			//Best-Effort: Worker beenden; nicht auf sich selbst Joinen (Reentrancy).
			if (workerRef != null && workerRef.IsAlive && workerRef.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
				workerRef.Join (5000);
		}

		protected override void CleanupUnmanagedResources()
		{
			m_DictObservables.Clear ();
			base.CleanupUnmanagedResources ();
		}
	}
}
