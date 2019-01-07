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
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.Tracing;
//using System.Windows.Threading;

namespace KS.Foundation
{
    public static class Concurrency
    {
        //public static void LockFreeUpdate(ref object field, object newField)
        //{
        //    var spinWait = new SpinWait();
        //    while (true)
        //    {
        //        object snapshot1 = field;
        //        object snapshot2 = Interlocked.CompareExchange(ref field, newField, snapshot1);
        //        if (snapshot1 == snapshot2) return;
        //        spinWait.SpinOnce();
        //    }
        //}

        //public static void WaitWithPumping(this Task task)
        //{
        //    if (task == null) throw new ArgumentNullException("task");
        //    var nestedFrame = new DispatcherFrame();
        //    task.ContinueWith(_ => nestedFrame.Continue = false);
        //    Dispatcher.PushFrame(nestedFrame);
        //    task.Wait();
        //}

		public static void WaitSpinning (int n) 
		{
			var spinWait = new SpinWait();
			for (int i = 0; i < n; i++)
				spinWait.SpinOnce ();
		}			

		public static void LockFreeUpdate(ref int field, int newField)
		{
			var spinWait = new SpinWait();
			while (true)
			{
				int snapshot1 = field;
				int snapshot2 = Interlocked.CompareExchange(ref field, newField, snapshot1);
				if (snapshot1 == snapshot2) return;
				spinWait.SpinOnce();
			}
		}			

        public static void LockFreeUpdate<T>(ref T field, T newField) where T : class
        {
            var spinWait = new SpinWait();
            while (true)
            {
                T snapshot1 = field;
                T snapshot2 = Interlocked.CompareExchange(ref field, newField, snapshot1);
                if (snapshot1 == snapshot2) return;
                spinWait.SpinOnce();
            }
        }

        public static void LockFreeUpdate<T>(ref T field, Func<T, T> updateFunction) where T : class
        {
            var spinWait = new SpinWait();
            while (true)
            {
                T snapshot1 = field;
                T calc = updateFunction(snapshot1);
                T snapshot2 = Interlocked.CompareExchange(ref field, calc, snapshot1);
                if (snapshot1 == snapshot2) return;
                spinWait.SpinOnce();

                //System.Diagnostics.Debug.WriteLine("LockFreeUpdate => Spin()");
            }
        }

        public static string GetStackTrace()
        {
            StackTrace stackTrace = new StackTrace();           // get call stack
            StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)

            StringBuilder sb = new StringBuilder();
            //sb.AppendLine("");

            // write call stack method names
            foreach (StackFrame stackFrame in stackFrames)
            {
				// unfortunately no file/line information is available here,
				// even when PDB-files are present.
                sb.AppendLine(stackFrame.GetMethod().Name);
				//sb.AppendLine(stackFrame.ToString());
                //Console.WriteLine(stackFrame.GetMethod().Name);   // write method name
            }

            return sb.ToString();
        }			
    }    
}
