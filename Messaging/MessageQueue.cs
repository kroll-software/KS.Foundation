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
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Threading;

namespace KS.Foundation
{
    public class MessageQueue<T> : DisposableObject, IObservable<T> where T : IFoundationMessage
    {        
        protected BlockingMessageQueue<T> m_Messages;
        protected Observable<T> m_Observable;
        protected CancellationTokenSource m_TokenSource;
        protected Task m_Task;

        private volatile bool m_IsRunning = false;
        private int m_SubscriptionCount = 0;
        protected int m_SuspendCounter = 0;

        public MessageQueue(bool startup)
        {        
            m_Messages = new BlockingMessageQueue<T>();
            m_Observable = new Observable<T>();

            if (startup)
                Start();
        }

        public bool IsRunning => m_IsRunning && !IsDisposed;

        public virtual bool CanSendMessage
        {
            get
            {
                return !IsDisposed 
                    && m_IsRunning 
                    && Volatile.Read(ref m_SuspendCounter) == 0 
                    && Volatile.Read(ref m_SubscriptionCount) > 0;
            }
        }

        public void Start()
        {
            if (m_IsRunning)
                return;

            lock (SyncObject)
            {                
                if (m_IsRunning) return;

                m_Messages.Start();
                m_IsRunning = true;
                m_TokenSource = new CancellationTokenSource();
                
                // Korrekter Start eines LongRunning-Background-Tasks
                m_Task = Task.Factory.StartNew(
                    LoopRun, 
                    m_TokenSource.Token, 
                    TaskCreationOptions.LongRunning, 
                    TaskScheduler.Default);              
            }
        }

        public int SubscriberCount => Volatile.Read(ref m_SubscriptionCount);

        public void Stop()
        {
            if (!m_IsRunning)
                return;

            lock (SyncObject)
            {
                if (!m_IsRunning) return;

                if (m_TokenSource != null)
                {
                    m_TokenSource.Cancel();
                }

                if (m_Messages != null)
                {
                    m_Messages.Quit();
                }

                if (m_Task != null)
                {
                    // Kurz warten, bis die Loop abgewickelt ist
                    m_Task.Wait(150);
                    m_Task = null;
                }

                m_IsRunning = false;
            }
        }

        protected void LoopRun()
        {
            while (!IsDisposed && m_TokenSource != null && !m_TokenSource.IsCancellationRequested)
            {                
                if (m_Messages.Dequeue(out T message) && message != null)
                {
                    try
                    {
                        if (m_TokenSource != null && !m_TokenSource.IsCancellationRequested)
                        {
                            m_Observable.SendMessage(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.LogError();
                    }                    
                }            
            }
        }

        public int Count => m_Messages.Count;

        public float Workload => m_Messages.Workload;

        public void SendMessage(T message)
        {
            try
            {
                if (m_IsRunning 
                    && Volatile.Read(ref m_SuspendCounter) == 0 
                    && Volatile.Read(ref m_SubscriptionCount) > 0 
                    && m_TokenSource != null 
                    && !m_TokenSource.IsCancellationRequested)
                {
                    // Direktes synchrones Enqueueing (sehr schnell, bewahrt Reihenfolge)
                    m_Messages.Enqueue(message);
                }
            }
            catch (Exception ex)
            {
                ex.LogError();
            }            
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            lock (SyncObject)
            {
                m_SubscriptionCount++;
                if (m_SubscriptionCount == 1 || !m_IsRunning)
                {
                    Start();
                }

                var innerUnsubscriber = m_Observable.Subscribe(observer);
                return new SubscriptionWrapper(innerUnsubscriber, OnObserverUnsubscribed);
            }
        }

        private void OnObserverUnsubscribed()
        {
            lock (SyncObject)
            {
                if (m_SubscriptionCount > 0)
                    m_SubscriptionCount--;

                if (m_SubscriptionCount <= 0)
                {
                    Stop();
                    m_SubscriptionCount = m_Observable.ObserverCount;
                }
            }
        }

        public void Unsubscribe(IObserver<T> observer)
        {            
            lock (SyncObject)
            {
                m_Observable.Unsubscribe(observer);
                OnObserverUnsubscribed();
            }
        }

        // *** Suspension
        public void Suspend()
        {
            Interlocked.Increment(ref m_SuspendCounter);
        }

        public void Resume()
        {
            lock (SyncObject)
            {
                if (m_SuspendCounter > 0)
                    Interlocked.Decrement(ref m_SuspendCounter);
            }
        }

        public bool IsSuspended => Volatile.Read(ref m_SuspendCounter) > 0;

        protected override void CleanupManagedResources()
        {
            Suspend();
            Stop();
            m_Observable.Dispose();            

            base.CleanupManagedResources();
        }

        /// <summary>
        /// Kapselt den Unsubscriber, um das Abmelden sauber zu zählen
        /// </summary>
        private class SubscriptionWrapper : DisposableObject
        {
            private IDisposable m_InnerUnsubscriber;
            private readonly Action m_OnUnsubscribe;

            public SubscriptionWrapper(IDisposable innerUnsubscriber, Action onUnsubscribe)
            {
                m_InnerUnsubscriber = innerUnsubscriber;
                m_OnUnsubscribe = onUnsubscribe;
            }

            protected override void CleanupManagedResources()
            {
                m_InnerUnsubscriber?.Dispose();
                m_InnerUnsubscriber = null;
                m_OnUnsubscribe?.Invoke();
                base.CleanupManagedResources();
            }
        }
    }
}