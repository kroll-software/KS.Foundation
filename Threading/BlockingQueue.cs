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
    public class BlockingQueue<T>
    {
        readonly int m_Size = 0;
        readonly Queue<T> m_Queue = new Queue<T>();
        readonly object SyncObject = new object();
        bool m_Quit = false;

        public void ResetStart()
        {
            lock (SyncObject) {
                m_Queue.Clear();
                m_Quit = false;
            }
        }

        public void Clear()
        {
            m_Queue.Clear();
        }

        public void Start()
        {
            lock (SyncObject) {
                m_Quit = false;
            }
        }

        public bool IsStarted
        {
            get {
                lock (SyncObject) {
                    return (!m_Quit);
                }
            }
        }

		public BlockingQueue()
		{
			m_Size = 100;
		}

		public BlockingQueue(int size = 100)
        {            
			m_Size = size;
        }
			        
        public void Quit()
        {
            lock (SyncObject) {
                m_Quit = true;
                Monitor.PulseAll(SyncObject);
            }
        }

        public bool TryEnqueue(T t, int millisecondsTimeout = -1)
        {
            if (!Monitor.TryEnter(SyncObject, millisecondsTimeout))                            
                return false;
            try {
                if (m_Quit || m_Queue.Count >= m_Size)
                    return false;
                return Enqueue(t);
            } finally {
                Monitor.Exit(SyncObject);
            }
        }

        public bool Enqueue(T t)
        {
            lock (SyncObject) {
                while (!m_Quit && m_Queue.Count >= m_Size)
                    if (!Monitor.Wait(SyncObject))
                        return false;
                if (m_Quit)
                    return false;
                m_Queue.Enqueue(t);
                Monitor.PulseAll(SyncObject);
            }
            return true;
        }


        public bool TryDequeue(out T t, int millisecondsTimeout = -1)
        {
            if (!Monitor.TryEnter(SyncObject, millisecondsTimeout)) {
                t = default(T);
                return false;
            }
            try {
                return Dequeue(out t);
            }
            finally {
                Monitor.Exit(SyncObject);
            }
        }

        public bool Dequeue(out T t)
        {
            t = default(T);
            lock (SyncObject) {
                while (!m_Quit && m_Queue.Count == 0)
                    if (!Monitor.Wait(SyncObject))
                        return false;
                if (m_Queue.Count == 0) 
					return false;
                t = m_Queue.Dequeue();
                Monitor.PulseAll(SyncObject);
            }
            return true;
        }

        public int Count
        {
            get {
                return m_Queue.Count;
            }
        }

        public float Workload
        {
            get {
                return (float)Count / (float)m_Size;
            }
        }
    }
}
