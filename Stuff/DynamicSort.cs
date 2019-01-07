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

using System.Diagnostics;
using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Linq.Expressions;

namespace KS.Foundation
{
    public static class DynSort
    {
        public static IEnumerable<T> DynamicSort<T>(IQueryable<T> items, string propertyName, bool ascending)
        {
            var parm = Expression.Parameter(typeof(T), "i");
            var conversion = Expression.Convert(Expression.Property(parm, propertyName), typeof(object));
            var getProperty = Expression.Lambda(conversion, parm);            
            return ascending ? items.OrderBy(p => getProperty) : items.OrderByDescending(p => getProperty);
        }

        public static IEnumerable<T> DynamicSort<T>(IEnumerable<T> items, string propertyName, bool ascending)
        {
            var propInfo = typeof(T).GetProperty(propertyName);
            return ascending ? items.OrderBy(i => propInfo.GetValue(i, null)) : items.OrderByDescending(i => propInfo.GetValue(i, null));
        }
    }
}