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
using System.Threading;
using System.Diagnostics;

namespace KS.Foundation
{
    public class Singleton<T> where T : class, new()
    {
		[DebuggerNonUserCode]
        Singleton() { }

        // ToDo: System.Lazy is insanely slow, replace !!
        private static readonly Lazy<T> instance = new Lazy<T>(() => new T());
		[DebuggerNonUserCode]
        public static T Instance { get { return instance.Value; } }
    }    

    /*
    Usage
     
    public static class ImportDataQueue
    {
        public static BlockingQueue<DataRow> RowQueue
        {
            get
            {
                return Singleton<BlockingQueue<DataRow>>.Instance;
            }
        }
    }
      
    */
}
