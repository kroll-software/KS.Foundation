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
using System.Threading;

namespace KS.Foundation
{
    public class BlockingMessageQueue<T> where T : IFoundationMessage
    {
        readonly int m_Size = 50;
        readonly LinkedList<T> m_Queue = new LinkedList<T>();
        readonly Dictionary<string, LinkedListNode<T>> m_Dict = new Dictionary<string, LinkedListNode<T>>();
        readonly object SyncObject = new object();
        bool m_Quit = false;

        public BlockingMessageQueue()
        {
            m_Size = 50;
        }

        public BlockingMessageQueue(int size)
        {
            m_Size = size;
        }

        public void ResetStart()
        {
            lock (SyncObject)
            {
                m_Dict.Clear();
                m_Queue.Clear();                
                m_Quit = false;
            }
        }

        public void Clear()
        {
            lock (SyncObject)
            {
                m_Dict.Clear();
                m_Queue.Clear();
                Monitor.PulseAll(SyncObject);
            }
        }

        public void Start()
        {
            lock (SyncObject)
            {
                m_Quit = false;
            }
        }

        public bool IsStarted
        {
            get
            {
                lock (SyncObject)
                {
                    return !m_Quit;
                }
            }
        }

        public void Quit()
        {
            lock (SyncObject)
            {
                m_Quit = true;
                Monitor.PulseAll(SyncObject);
            }
        }

        public bool TryEnqueue(T t, int millisecondsTimeout = -1)
        {
            if (!Monitor.TryEnter(SyncObject, millisecondsTimeout))                            
                return false;            

            try
            {
                if (m_Quit || m_Queue.Count >= m_Size)
                    return false;

                EnqueueInternal(t);
                return true;
            }
            finally
            {
                Monitor.Exit(SyncObject);
            }
        }

        public bool Enqueue(T t)
        {
            lock (SyncObject)
            {
                while (!m_Quit && m_Queue.Count >= m_Size)
                {
                    if (!Monitor.Wait(SyncObject))
                        return false;
                }

                if (m_Quit)
                    return false;

                EnqueueInternal(t);
            }

            return true;
        }

        /// <summary>
        /// Interne Hilfsmethode – setzt voraus, dass SyncObject bereits geholtet wird!
        /// </summary>
        private void EnqueueInternal(T t)
        {
            string messageKey = t.Subject;
            if (m_Queue.Count > 0 && t.ShouldReEnqueue && messageKey != null && m_Dict.TryGetValue(messageKey, out LinkedListNode<T> node))
            {
                if (node != null && node != m_Queue.Last)
                {
                    m_Queue.Remove(node);
                    m_Dict.Remove(messageKey);

                    node = m_Queue.AddLast(t);
                    m_Dict[messageKey] = node;
                }
            }
            else
            {
                LinkedListNode<T> newNode = m_Queue.AddLast(t);
                if (t.ShouldReEnqueue && messageKey != null && !m_Dict.ContainsKey(messageKey))
                {
                    m_Dict.Add(messageKey, newNode);
                }
            }

            Monitor.PulseAll(SyncObject);
        }

        public bool TryDequeue(out T t, int millisecondsTimeout = -1)
        {
            if (!Monitor.TryEnter(SyncObject, millisecondsTimeout))
            {
                t = default(T);
                return false;
            }

            try
            {
                if (m_Quit || m_Queue.Count == 0)
                {
                    t = default(T);
                    return false;
                }

                return DequeueInternal(out t);
            }
            finally
            {
                Monitor.Exit(SyncObject);
            }
        }

        public bool Dequeue(out T t)
        {
            lock (SyncObject)
            {
                while (!m_Quit && m_Queue.Count == 0)
                {
                    if (!Monitor.Wait(SyncObject))
                    {
                        t = default(T);
                        return false;
                    }
                }

                if (m_Queue.Count == 0) 
                {
                    t = default(T);
                    return false;
                }

                return DequeueInternal(out t);
            }
        }

        /// <summary>
        /// Interne Hilfsmethode – setzt voraus, dass SyncObject bereits geholtet wird!
        /// </summary>
        private bool DequeueInternal(out T t)
        {
            t = m_Queue.First.Value;
            m_Queue.RemoveFirst();

            if (t.ShouldReEnqueue && t.Subject != null)
            {
                m_Dict.Remove(t.Subject);
            }

            if (m_Queue.Count == 0)
            {
                m_Dict.Clear();
            }

            Monitor.PulseAll(SyncObject);
            return true;
        }

        public int Count
        {
            get
            {
                lock (SyncObject)
                {
                    return m_Queue.Count;
                }
            }
        }

        public float Workload
        {
            get
            {
                lock (SyncObject)
                {
                    return (float)m_Queue.Count / (float)m_Size;
                }
            }
        }
    }
}