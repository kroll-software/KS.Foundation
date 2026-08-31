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
        public static void WaitSpinning(int n)
        {
            var spinWait = new SpinWait();
            for (int i = 0; i < n; i++)
                spinWait.SpinOnce();
        }

        /// <summary>
        /// Tauscht ein Int-Feld atomar gegen einen festen Wert aus.
        /// </summary>
        public static void LockFreeUpdate(ref int field, int newField)
        {
            Interlocked.Exchange(ref field, newField);
        }

        /// <summary>
        /// Tauscht ein Referenz-Feld atomar gegen einen festen Wert aus.
        /// </summary>
        public static void LockFreeUpdate<T>(ref T field, T newField) where T : class
        {
            Interlocked.Exchange(ref field, newField);
        }

        /// <summary>
        /// Berechnet atomar einen neuen Wert basierend auf dem aktuellen Feldzustand (CAS-Loop).
        /// WICHTIG: updateFunction muss frei von Seiteneffekten sein!
        /// </summary>
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
            }
        }

        /// <summary>
        /// Erzeugt einen StackTrace für Exception-Logs (lädt PDB-Zeilennummern, falls vorhanden).
        /// </summary>
        public static string GetStackTrace()
        {
            // true aktiviert PDB-Zeilennummern und Dateinamen für Debug-Builds
            StackTrace stackTrace = new StackTrace(true);
            StackFrame[] stackFrames = stackTrace.GetFrames();

            if (stackFrames == null)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            foreach (StackFrame stackFrame in stackFrames)
            {
                var method = stackFrame.GetMethod();
                if (method != null)
                {
                    string fileName = stackFrame.GetFileName();
                    int lineNumber = stackFrame.GetFileLineNumber();

                    if (!string.IsNullOrEmpty(fileName) && lineNumber > 0)
                    {
                        sb.AppendLine($"{method.DeclaringType?.Name}.{method.Name} in {fileName}:line {lineNumber}");
                    }
                    else
                    {
                        sb.AppendLine($"{method.DeclaringType?.Name}.{method.Name}");
                    }
                }
            }

            return sb.ToString();
        }
    }
}