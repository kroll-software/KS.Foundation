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
        private readonly int m_Size;
        private readonly Queue<T> m_Queue = new Queue<T>();
        private readonly object SyncObject = new object();
        private bool m_Quit = false;

        /// <summary>
        /// Parameterloser Konstruktor (z. B. für Generic Constraints wie where T : new())
        /// Initialisiert die Queue mit einer Standardgröße von 100.
        /// </summary>
        public BlockingQueue() : this(100)
        {
        }

        /// <summary>
        /// Initialisiert die Queue mit einer benutzerdefinierten Kapazität.
        /// </summary>
        public BlockingQueue(int size)
        {            
            m_Size = size > 0 ? size : 100;
        }

        public void ResetStart()
        {
            lock (SyncObject) 
            {
                m_Queue.Clear();
                m_Quit = false;
                Monitor.PulseAll(SyncObject);
            }
        }

        public void Clear()
        {
            lock (SyncObject)
            {
                m_Queue.Clear();
                Monitor.PulseAll(SyncObject); // Benachrichtigt evtl. blockierte Enqueuer
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
            get {
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

        public bool Enqueue(T t)
        {
            return TryEnqueue(t, Timeout.Infinite);
        }

        public bool TryEnqueue(T t, int millisecondsTimeout = Timeout.Infinite)
        {
            long lockTimeoutTicks = millisecondsTimeout < 0 
                ? long.MaxValue 
                : DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(millisecondsTimeout).Ticks;

            lock (SyncObject)
            {
                while (!m_Quit && m_Queue.Count >= m_Size)
                {
                    int remainingTimeout = GetRemainingTimeout(lockTimeoutTicks);
                    if (remainingTimeout == 0 || !Monitor.Wait(SyncObject, remainingTimeout))
                    {
                        return false; // Timeout erreicht
                    }
                }

                if (m_Quit)
                    return false;

                m_Queue.Enqueue(t);
                Monitor.PulseAll(SyncObject);
                return true;
            }
        }

        public bool Dequeue(out T t)
        {
            return TryDequeue(out t, Timeout.Infinite);
        }

        public bool TryDequeue(out T t, int millisecondsTimeout = Timeout.Infinite)
        {
            t = default(T);
            long lockTimeoutTicks = millisecondsTimeout < 0 
                ? long.MaxValue 
                : DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(millisecondsTimeout).Ticks;

            lock (SyncObject)
            {
                while (!m_Quit && m_Queue.Count == 0)
                {
                    int remainingTimeout = GetRemainingTimeout(lockTimeoutTicks);
                    if (remainingTimeout == 0 || !Monitor.Wait(SyncObject, remainingTimeout))
                    {
                        return false; // Timeout erreicht
                    }
                }

                if (m_Queue.Count == 0) 
                    return false;

                t = m_Queue.Dequeue();
                Monitor.PulseAll(SyncObject);
                return true;
            }
        }

        public int Count
        {
            get {
                lock (SyncObject)
                {
                    return m_Queue.Count;
                }
            }
        }

        public float Workload
        {
            get {
                lock (SyncObject)
                {
                    return (float)m_Queue.Count / (float)m_Size;
                }
            }
        }

        private static int GetRemainingTimeout(long lockTimeoutTicks)
        {
            if (lockTimeoutTicks == long.MaxValue)
                return Timeout.Infinite;

            long remainingTicks = lockTimeoutTicks - DateTime.UtcNow.Ticks;
            if (remainingTicks <= 0)
                return 0;

            long millis = remainingTicks / TimeSpan.TicksPerMillisecond;
            return millis > int.MaxValue ? int.MaxValue : (int)millis;
        }
    }
}