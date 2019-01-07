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
        protected BlockingMessageQueue<T> m_Messages = null;
        protected Observable<T> m_Observable = null;
        protected CancellationTokenSource m_TokenSource = null;
        protected Task m_Task = null;

        public MessageQueue(bool startup)
        {        
            m_Messages = new BlockingMessageQueue<T>();
            m_Observable = new Observable<T>();

            if (startup)
                Start();
        }

        private bool m_IsRunning = false;
        public bool IsRunning
        {
            get
            {
                return m_IsRunning && !IsDisposed;
            }
        }        

        public virtual bool CanSendMessage
        {
            get
            {
                return !IsDisposed && m_IsRunning && m_SuspendCounter == 0 && m_SubscriptionCount > 0;
            }
        }

        public void Start()
        {
            if (m_IsRunning)
                return;

            lock (SyncObject)
            {                
                m_Messages.Start();
                m_IsRunning = true;
                m_TokenSource = new CancellationTokenSource();
                m_Task = new Task(LoopRun, m_TokenSource.Token, TaskCreationOptions.LongRunning);
                m_Task.Start(TaskScheduler.Default);                
            }
        }

        private int m_SubscriptionCount = 0;
        public int SubscriberCount 
        {
            get
            {
                return m_SubscriptionCount;
            }
        }

        public void Stop()
        {
            if (!m_IsRunning)
                return;

            lock (SyncObject)
            {
                if (m_TokenSource != null)
                    m_TokenSource.Cancel();

                if (m_Messages != null)
                    m_Messages.Quit();

                if (m_Task != null)
                {
                    m_Task.Wait(100);
                    m_Task = null;
                }

                m_IsRunning = false;
            }
        }

        protected void LoopRun()
        {
            //while (!m_Task.IsCanceled)
            while (!m_TokenSource.IsCancellationRequested && !IsDisposed)
            {                
                T message;
                if (m_Messages.Dequeue(out message) && message != null)
                {
                    try
                    {
                        if (!m_TokenSource.IsCancellationRequested)
                            m_Observable.SendMessage(message);
                    }
                    catch (Exception ex)
                    {
                        ex.LogError();
                    }                    
                }            
            }
        }

        public struct MessageParams
        {            
			public BlockingMessageQueue<T> Queue;
			public T Message;

            public override string ToString()
            {
                if (Message == null)
                    return base.ToString();

                return Message.ToString();
            }
        }

        public int Count
        {
            get
            {
                return m_Messages.Count;
            }
        }

        public float Workload
        {
            get
            {
                //if (m_Messages.Workload > 0.95f)
                //{
                //    T var = default(T);
                //    bool success = false;

                //    do
                //    {
                //        success = m_Messages.Dequeue(out var);
                //        System.Diagnostics.Debug.WriteLine(var.ToString());
                //    } while (success);

                //    int iTest = 0;                    
                //}

                return m_Messages.Workload;
            }
        }

        public void SendMessage(T message)
        {
            try
            {
                if (m_IsRunning && m_SuspendCounter == 0 && m_SubscriptionCount > 0 && !m_TokenSource.IsCancellationRequested)
                {
                    Task.Factory.StartNew((p) => ((MessageParams)p).Queue.Enqueue(((MessageParams)p).Message), 
                        new MessageParams { Queue = this.m_Messages, Message = message }, m_TokenSource.Token, 
						TaskCreationOptions.PreferFairness, TaskScheduler.Default);
                }
            }
            catch (Exception ex)
            {
                ex.LogError();
            }            
        }

        // never called
        public IDisposable Subscribe(IObserver<T> observer)
        {
            m_SubscriptionCount++;
            if (m_SubscriptionCount == 1)
                Start();

            return m_Observable.Subscribe(observer);
        }

        public void Unsubscribe(IObserver<T> observer)
        {            
            this.m_Observable.Unsubscribe(observer);

            if (m_SubscriptionCount > 0)
                m_SubscriptionCount--;

            if (m_SubscriptionCount <= 0)
            {
                Stop();
                m_SubscriptionCount = m_Observable.ObserverCount;
            }
        }

        // *** Suspension
        protected int m_SuspendCounter = 0;
        public void Suspend()
        {
            m_SuspendCounter++;
        }

        public void Resume()
        {
            if (m_SuspendCounter > 0)
                m_SuspendCounter--;
        }

        public bool IsSuspended
        {
            get
            {
                return m_SuspendCounter > 0;
            }
        }

        protected override void CleanupManagedResources()
        {
            Suspend();
            Stop();
            //m_Observable.SendCompleted();
            m_Observable.Dispose();            

            base.CleanupManagedResources();
        }
    }
}
