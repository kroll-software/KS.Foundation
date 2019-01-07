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
using System.Threading.Tasks;

namespace KS.Foundation
{
	public class PulsedWorkerThread : DisposableObject
	{
		private Task m_Task = null;        
		private CancellationTokenSource m_TokenSource = null;

		private readonly object m_MonitorSync = new object();
		private Action m_Action = null;

		private bool m_IsInAction = false;
		public bool IsInAction
		{
			get
			{
				return m_IsInAction;
			}
		}

		/***
		public void Wait()
		{
			if (m_IsInAction)
			{
				Monitor.Enter(m_MonitorSync);
				Monitor.Exit(m_MonitorSync);
			}
		}
		***/

		public void Wait(int millisecondsTimeout)
		{
			if (m_IsInAction)
			{
				if (Monitor.TryEnter (m_MonitorSync, millisecondsTimeout)) {
					Monitor.Pulse (m_MonitorSync);
					Monitor.Exit (m_MonitorSync);
				}
			}
		}

		private bool m_Running = false;
		public bool IsRunning
		{
			get
			{
				return m_Running;
			}
		}

		public int Timeout { get; private set; }

		public PulsedWorkerThread(Action action, int timeout = 8000)
		{
			m_Action = action;
			Timeout = timeout;
		}

		public IAsyncResult Start()
		{
			return Start(TaskCallback);
		}

		private void TaskCallback(IAsyncResult result)
		{
			bool b = m_Task.IsCompleted;

			if (m_TokenSource != null)
			{
				m_TokenSource.Dispose();
				m_TokenSource = null;
			}

			if (m_Task != null)
			{
				m_Task.Dispose();
				m_Task = null;
			}
		}

		public IAsyncResult Start(AsyncCallback callback)
		{
			if (m_Task != null)
				return null;        

			m_Running = true;

			m_TokenSource = new CancellationTokenSource();            
			CancellationToken cToken = m_TokenSource.Token;

			m_Task = new Task((t) => RunProcess(cToken), null, cToken, TaskCreationOptions.AttachedToParent);

			if (callback != null)
			{
				m_Task.ContinueWith((tsk) => callback(tsk));
			}

			//TaskScheduler scheduler = TaskScheduler.Current;
			TaskScheduler scheduler = TaskScheduler.Default;
			m_Task.Start(scheduler);

			Thread.Sleep(10);

			//m_Task.Start();
			return m_Task;
		}               

		public void Stop()
		{            
			if (m_TokenSource != null)
			{
				m_TokenSource.Cancel();
				Pulse();

				try
				{
					if (m_Task != null)
						m_Task.Wait(m_TokenSource.Token);
				}
				catch (System.OperationCanceledException)
				{

				}
				catch (Exception)
				{
					//bool b = m_Task.IsCanceled;
				}
			}            

			m_Running = false;
		}        

		public bool Pulse()
		{
			if (Monitor.TryEnter(m_MonitorSync, Timeout))
			{
				Monitor.Pulse(m_MonitorSync);
				Monitor.Exit(m_MonitorSync);
				return true;
			}

			return false;
		}

		private Exception m_LastException = null;
		public Exception LastException
		{
			get
			{
				return m_LastException;
			}
		}

		private void RunProcess(CancellationToken ct)
		{
			while (!ct.IsCancellationRequested)
			{
				if (Monitor.TryEnter(m_MonitorSync, Timeout))
				{
					Monitor.Pulse(m_MonitorSync);
					if (Monitor.Wait(m_MonitorSync, Timeout))   // Dead-Lock
					{
						try
						{
							if (!ct.IsCancellationRequested)
							{
								m_IsInAction = true;
								m_Action();
								m_LastException = null;
							}
						}
						catch (Exception ex)
						{
							m_LastException = ex;
						}
						finally
						{
							m_IsInAction = false;
							Monitor.Exit(m_MonitorSync);
						}
					}
				}
			}
		}

		protected override void CleanupManagedResources ()
		{
			if (m_Task != null)
				Stop();
			base.CleanupManagedResources ();
		}
	}    
}
