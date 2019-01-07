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
using System.Runtime.Serialization;

namespace KS.Foundation
{
    [Serializable]
    public abstract class DisposableObject : IDisposable
    {   		
		//public object SyncObject { get; private set; }
		public readonly object SyncObject = new object();

		[NonSerialized]
		public static bool ReportDisposingProblems = false;
		private static long m_NotDisposedCount = 0;

		/*** VisualStudio doesn't like it. Why ?
		[OnDeserializing]
		protected virtual void OnDeserializing()
		{
			SyncObject = new object();
		}
		[OnDeserialized]
		protected virtual void OnDeserialized()
		{
		}
		***/
        
		protected DisposableObject()
		{
			SyncObject = new object ();
		}

        [NonSerialized]
        private bool m_Disposed = false;

        [NonSerialized]
		private bool m_IsDisposing = false;

        public virtual bool IsDisposed
        {
            get
            {
                return m_Disposed || m_IsDisposing;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
			
        ~DisposableObject()
        {
			if (!m_Disposed && ReportDisposingProblems)
                OnFinalizerCalled();

            Dispose(false);
        }
        
        protected void Dispose(bool disposing)
        {
			if (!m_Disposed && !m_IsDisposing) {
				m_IsDisposing = true;

				if (disposing) {                    
					try {
						CleanupManagedResources ();
					} catch (Exception ex) {
						ex.LogError ();
					}                    
				}

				try {
					CleanupUnmanagedResources ();	
				} catch (Exception ex) {
					ex.LogError ();
				}

				m_Disposed = true;

			} else if (ReportDisposingProblems) {
				this.LogWarning ("An instance of {0} was already disposed.", this.GetType ().FullName);
			}
        }

        protected virtual void CleanupManagedResources()
        {			
        }

        protected virtual void CleanupUnmanagedResources()
        {
        }

        protected virtual void OnFinalizerCalled()
        {
            unchecked
            {   				
				this.LogWarning("[{0}] An instance of {1} was not disposed before garbage collection.", m_NotDisposedCount++, this.GetType().FullName);
            }
        }
    }
}
