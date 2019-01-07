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
        readonly int m_Size = 0;
        readonly LinkedList<T> m_Queue = new LinkedList<T>();
        readonly Dictionary<string, LinkedListNode<T>> m_Dict = new Dictionary<string,LinkedListNode<T>>();
        readonly object SyncObject = new object();
        bool m_Quit = false;

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
            m_Dict.Clear();
            m_Queue.Clear();
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
                    return (!m_Quit);
                }
            }
        }

        public BlockingMessageQueue()
        {
            //m_Size = 500;
            m_Size = 50;
        }

        public BlockingMessageQueue(int size)
        {
            m_Size = size;
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

                return Enqueue(t);
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
                    if (!Monitor.Wait(SyncObject))
                        return false;

                if (m_Quit)
                    return false;

                //m_Queue.Enqueue(t);

                string messageKey = t.Subject;
				LinkedListNode<T> node = null;                
				if (Count > 0 && t.ShouldReEnqueue && m_Dict.TryGetValue(messageKey, out node))
                {                    
                    if (node != null && node != m_Queue.Last)
                    {
                        m_Queue.Remove(node);
                        m_Dict.Remove(messageKey);

                        node = m_Queue.AddLast(t);
                        m_Dict.Add(messageKey, node);
                    }
                }
                else
                {                    
                    node = m_Queue.AddLast(t);
                    if (t.ShouldReEnqueue && !m_Dict.ContainsKey(messageKey))
                        m_Dict.Add(messageKey, node);
                }

                Monitor.PulseAll(SyncObject);
            }

            return true;
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
                return Dequeue(out t);
            }
            finally
            {
                Monitor.Exit(SyncObject);
            }
        }

        public bool Dequeue(out T t)
        {
            t = default(T);

            lock (SyncObject)
            {
                while (!m_Quit && m_Queue.Count == 0)
                    if (!Monitor.Wait(SyncObject))
                        return false;

                if (m_Queue.Count == 0) 
					return false;

                t = m_Queue.First.Value;
                m_Queue.RemoveFirst();
				m_Dict.Remove(t.Subject);
				if (Count == 0)
					m_Dict.Clear ();
                Monitor.PulseAll(SyncObject);
            }

            return true;
        }

        public int Count
        {
            get
            {
                return m_Queue.Count;
            }
        }

        public float Workload
        {
            get
            {
                return (float)Count / (float)m_Size;
            }
        }
    }
}
