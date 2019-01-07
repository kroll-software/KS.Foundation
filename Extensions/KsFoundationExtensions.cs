/*
{*******************************************************************}
{                                                                   }
{          KS-GANTT Library                                         }
{          A Project Planning Solution for Professionals            }
{                                                                   }
{          Copyright (c) 2009 - 2015 by Kroll-Software,             }
{          Altdorf, Switzerland, All Rights Reserved                }
{          www.kroll-software.ch                                    }
{                                                                   }
{   Dual licensed under                                             }
{   (1) GNU Public License version 2 (GPLv2)                        }
{   http://www.gnu.org/licenses/old-licenses/gpl-2.0.en.html        }
{                                                                   }
{   and if this doesn't fit for you, there are                      }
{   (2) Commercial licenses available.                              }
{                                                                   }
{   This file belongs to the                                        }
{   KS-Gantt WinForms Control library v. 5.0.2                      }
{                                                                   }
{*******************************************************************}
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Drawing;
//using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;

namespace KS.Foundation
{    
    public static class KsFoundationExtensions
    {
        public static int SafeHash<T>(this T obj)
        {
            if (obj == null)
                return 0;

            return obj.GetHashCode();
        }        

        public static int CombineHash<T>(this int value, IEnumerable<T> other)
        {
            if (other.IsNullOrEmpty())
                return value;

            other.OfType<T>().ForEach(v => value = value.CombineHash(v.GetHashCode()));
            return value;
        }

        public static TResult SafeInvoke<T, TResult>(this T context, Func<T, TResult> func, bool BeginInvoke = true) where T : ISynchronizeInvoke
        {
            if (context.InvokeRequired)
            {
                if (BeginInvoke)
                {
                    IAsyncResult result = context.BeginInvoke(func, new object[] { context });
                    object endResult = context.EndInvoke(result);
                    return (TResult)endResult;
                }
                else
                {                    
                    return (TResult)context.Invoke(func, new object[] { context });
                }
            }
            else
                return func(context);
        }

        public static void SafeInvoke<T>(this T context, Action<T> action) where T : ISynchronizeInvoke
        {
            if (context.InvokeRequired)
                context.Invoke(action, new object[] { context });
            else
                action(context);
        }

        public static void SafeBeginInvoke<T>(this T context, Action<T> action) where T : ISynchronizeInvoke
        {
            if (context.InvokeRequired)
                context.BeginInvoke(action, new object[] { context });
            else
                action(context);
        }

        //public static string LogName(this object obj)
        //{
        //    if (IsNull(obj))
        //        return " Object was null";
        //    else
        //        return obj.ToString();
        //}

        public static string Combine(this string s, string other, char delimiter)
        {
            if (s.LastOrDefault() == ';' && other.FirstOrDefault() == ';')
                return s.Substring(0, s.Length - 1) + other;
            else if (s.LastOrDefault() != ';' && other.FirstOrDefault() != ';')
                return s + ";" + other;
            else
                return s + other;
        }

		public static TimeSpan Min(this TimeSpan ts1, TimeSpan ts2)
		{
			if (ts1 < ts2)
				return ts1;
			else
				return ts2;
		}

		public static TimeSpan Max(this TimeSpan ts1, TimeSpan ts2)
		{
			if (ts1 > ts2)
				return ts1;
			else
				return ts2;
		}

        public static DateTime Min(this DateTime d1, DateTime d2)
        {
            if (d1 < d2)
                return d1;
            else
                return d2;
        }

        public static DateTime Max(this DateTime d1, DateTime d2)
        {
            if (d1 > d2)
                return d1;
            else
                return d2;
        }

        public static bool IsDefined(this DateTime d)
        {
            // this should hopefully work with any calendar in the world, 
            // even after converting from UTC, which was a problem here.
            return d.Year > DateTime.MinValue.Year && d.Year < DateTime.MaxValue.Year;
            //return d != DateTime.MinValue && d.Year != DateTime.MaxValue.Year;
        }

        public static bool IsDisposable<T>(this T obj)
        {
            if (obj == null)
                return false;

            //return typeof(IDisposable).IsAssignableFrom(typeof(T));
            // this works and is 6 times faster:
            return (obj as IDisposable) != null;
        }

        public static bool IsDisposable(this Type t)
        {
            //return (t as IDisposable) != null;  // this doesn't work either
            return typeof(IDisposable).IsAssignableFrom(t);
        }

        public static bool HasInterface(this object obj, Type interfaceType)
        {
            if (obj == null)
                return false;

            return interfaceType.IsAssignableFrom(obj.GetType());
        }

        public static bool HasInterface<T>(this object obj)
        {
            if (obj == null)
                return false;

            return typeof(T).IsAssignableFrom(obj.GetType());
        }

        public static bool IsNullOrEmpty(this String s)
        {
            return s == null || s == String.Empty;
        }

        public static bool IsNullOrEmpty<T>(this ICollection<T> coll)
        {
			return coll == null || !coll.Any();
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enu)
        {
            return enu == null || !enu.Any();
        }

        public static double StdDev(this double[] values)
        {			
            double ret = 0;
            int count = values.Length;
            if (count > 1)
            {
                //Compute the Average
                double avg = values.Average();

                //Perform the Sum of (value-avg)^2
                double sum = values.Sum(d => (d - avg) * (d - avg));

                //Put it all together
                ret = Math.Sqrt(sum / count);
            }
            return ret;
        }

        // ************************** Common extensions ******************************

        [DebuggerStepThrough]
        public static void Do<T>(this T e, Action<T> action)
        {
            if (e != null && action != null)
                action(e);
        }

        [DebuggerStepThrough]
        public static IEnumerable<int> To(this int inclusiveLowerBound, int exclusiveUpperBound)
        {
            for (int i = inclusiveLowerBound; i < exclusiveUpperBound; i++)
                yield return i;
        }

        [DebuggerStepThrough]
        public static void ForEach<T>(this IEnumerable<T> e, Action<T> action)
        {
            if (e != null && action != null)
                foreach (var x in e)
                    action(x);
        }

        [DebuggerStepThrough]
        public static void ParallelForEach<T>(this IEnumerable<T> e, Action<T> action)
        {
            if (e != null && action != null)
            {
                Task.Run(() =>
                {
                    Parallel.ForEach(e, x => action(x));
                }).Wait();
            }
        }

        [DebuggerStepThrough]
        public static void AddRange<T>(this HashSet<T> h, IEnumerable<T> source)
        {
            if (source != null)
            {
                foreach (T val in source)
                    if (!h.Contains(val))
                        h.Add(val);
            }
        }        

        //public static Color MixAdd(this Color c1, Color c2)
        //{
        //    int a = Math.Min((c1.A + c2.A), 255);
        //    int r = Math.Min((c1.R + c2.R), 255);
        //    int g = Math.Min((c1.G + c2.G), 255);
        //    int b = Math.Min((c1.B + c2.B), 255);

        //    return Color.FromArgb((byte)a, (byte)(r), (byte)(g), (byte)(b));
        //}

        //public static Color MixMid(this Color c1, Color c2)
        //{
        //    int a = (c1.A + c2.A) / 2;
        //    int r = (c1.R + c2.R) / 2;
        //    int g = (c1.G + c2.G) / 2;
        //    int b = (c1.B + c2.B) / 2;

        //    return Color.FromArgb((byte)a, (byte)(r), (byte)(g), (byte)(b));
        //}

        //public static Color MixInv(this Color c1, Color c2)
        //{
        //    int a = ((255 - c1.A) + c2.A) / 2;
        //    int r = ((255 - c1.R) + c2.R) / 2;
        //    int g = ((255 - c1.G) + c2.G) / 2;
        //    int b = ((255 - c1.B) + c2.B) / 2;

        //    return Color.FromArgb((byte)a, (byte)(r), (byte)(g), (byte)(b));
        //}        

        public static void Swap<T>(ref T first, ref T second)
        {
            T temp = first;
            first = second;
            second = temp;
        }

        public static void Swap<T>(this IList<T> source, int i, int k)
        {
            T temp = source[i];
            source[i] = source[k];
            source[k] = temp;
        }

        public static IEnumerable<T> FisherYatesShuffle<T>(this IEnumerable<T> source)
        {
            if (source == null)                
                yield break;

            List<T> lst = source.ToList();
            if (lst.Count < 2)
                yield break;            
            
            for (int i = lst.Count - 1; i > 0; i--)
            {
                lst.Swap(i, ThreadSafeRandom.Next(i));
                yield return lst[i];
            }
            
            yield return lst[0];            
        }

        public static IEnumerable<T> OneRandomItem<T>(this IEnumerable<T> source)
        {
            if (source == null || !source.Any())
                yield break;

            yield return source.ElementAt(ThreadSafeRandom.Next(source.Count()));
        }

        public static T CastTo<T>(this object source)
        {
            if (source == null)
                return default(T);
            else
                return (T)source;
        }

        public static void DisposeListObjects<T>(this IEnumerable<T> source)
        {
			if (source != null)                
				source.OfType<IDisposable>().ForEach(item => item.Dispose());
        }
        
        public static void ClearAsync<T>(this IList<T> source, bool disposeItems = false)
        {
			Task.Run(() =>
            {
                if (source != null && source.Count > 0)
                {
                    if (disposeItems)                    
                        source.DisposeListObjects();                    

                    source.Clear();
                }
            });
        }

        //public static void CheckConsistencyParentChild(this IGanttModelContext context)
        //{
        //    StringBuilder sb = new StringBuilder();

        //    context.Tasks.Where(t => t.ParentItem != null).GroupBy(t => t.ParentItemKey).ForEach(gt =>
        //    {
        //        int c1 = gt.Count();
        //        GroupItem group = gt.FirstOrDefault(g => g.ParentItem != null).ParentItem;

        //        int c2 = group.ChildCount;

        //        if (c1 != c2)
        //            sb.AppendLine(String.Format("Group-Parent/Child mismatch '{0}': {1} / {2}", group.Text, c1.ToString(), c2.ToString()));                                   
        //    });     
       
        //    if (!String.IsNullOrEmpty(sb.ToString()))
        //        throw new Exception(sb.ToString());
        //}

        //public static void FixConsistencyParentChild(this IGanttModelContext context)
        //{            
        //    context.Tasks.Where(t => t.ParentItem != null).GroupBy(t => t.ParentItemKey).ForEach(gt =>
        //    {                
        //        GroupItem group = gt.FirstOrDefault(g => g.ParentItem != null).ParentItem;
        //        if (group != null)
        //            group.ReplaceAllChildren(gt);
        //    });            
        //}


        //public static void CheckConsistencyResources(this IGanttModelContext context)
        //{            
        //    int i = 0;
        //    int k = 0;
        //    context.Tasks.ForEach(t =>
        //    {
        //        foreach (WorkItem w in t.Works)
        //        {
        //            if (w.GanttModelContext == null)
        //                k++;

        //            foreach (ResourceRelation rel in w.AssignedResources)
        //            {
        //                if (rel.Resource == null)
        //                    i++;

        //                if (rel.GanttModelContext == null)
        //                    k++;
        //            }
        //        }

        //        foreach (ResourceRelation rel in t.Resources)
        //        {
        //            if (rel.Resource == null)
        //                i++;

        //            if (rel.GanttModelContext == null)
        //                k++;
        //        }                
        //    });

        //    if (i > 0)
        //        throw new Exception(String.Format("{0} Resources are NULL", i.ToString()));

        //    if (k > 0)
        //        throw new Exception(String.Format("{0} Resources GanttModelContext are NULL", i.ToString()));                    
        //}


        public static T BinarySearch<T, TKey>(this IList<T> list, Func<T, TKey> keySelector, TKey key) where TKey : IComparable<TKey>
        {
            int min = 0;
            int max = list.Count;
            while (min < max)
            {
                int mid = (max + min) / 2;
                T midItem = list[mid];
                TKey midKey = keySelector(midItem);
                int comp = midKey.CompareTo(key);
                if (comp < 0)
                {
                    min = mid + 1;
                }
                else if (comp > 0)
                {
                    max = mid - 1;
                }
                else
                {
                    return midItem;
                }
            }
            if (min == max &&
                keySelector(list[min]).CompareTo(key) == 0)
            {
                return list[min];
            }
            throw new InvalidOperationException("Item not found");
        }

        public static string Sci(this decimal x, int significant_digits)
        {
            return ((double)x).Sci(significant_digits);
        }

        /// <summary>
        /// Format a number with scientific exponents and specified sigificant digits.
        /// </summary>
        /// <param name="x">The value to format</param>
        /// <param name="significant_digits">The number of siginicant digits to show</param>
        /// <returns>The fomratted string</returns>
        public static string Sci(this double x, int significant_digits)
        {
            //Check for special numbers and non-numbers
            if (double.IsInfinity(x) || double.IsNaN(x) || x == 0)
            {
                return x.ToString();
            }
            // extract sign so we deal with positive numbers only
            int sign = Math.Sign(x);
            x = Math.Abs(x);
            // get scientific exponent, 10^3, 10^6, ...
            int sci = (int)Math.Floor(Math.Log(x, 10) / 3) * 3;
            // scale number to exponent found
            x = x * Math.Pow(10, -sci);
            // find number of digits to the left of the decimal
            int dg = (int)Math.Floor(Math.Log(x, 10)) + 1;
            // adjust decimals to display
            int decimals = Math.Min(significant_digits - dg, 15);
            // format for the decimals
            string fmt = new string('0', decimals);
            if (sci == 0)
            {
                //no exponent
                return string.Format("{0}{1:0." + fmt + "}",
                    sign < 0 ? "-" : string.Empty,
                    Math.Round(x, decimals));
            }
            //int index = sci / 3 + 6;
            // with 10^exp format
            return string.Format("{0}{1:0." + fmt + "}e{2}",
                sign < 0 ? "-" : string.Empty,
                Math.Round(x, decimals),
                sci);
        }


        public static bool Like(this string str, string wild, bool case_sensitive = false)
        {        
            if (String.IsNullOrEmpty(wild))
                return true;

            if (String.IsNullOrEmpty(str))                
                return false;

            if (!case_sensitive)
            {
                wild = wild.ToLower();
                str = str.ToLower();
            }            

            int cp = 0, mp = 0, i = 0, j = 0;            

            while ((i < str.Length) && (j < wild.Length) && (wild[j] != '*'))
            {
                if ((wild[j] != str[i]) && (wild[j] != '?'))                
                    return false;
                i++;
                j++;
            }

            while (i < str.Length)
            {
                if (j < wild.Length && wild[j] == '*')
                {
                    if ((j++) >= wild.Length)                    
                        return true;
                    mp = j;
                    cp = i + 1;
                }
                else if (j < wild.Length && (wild[j] == str[i] || wild[j] == '?'))
                {
                    j++;
                    i++;
                }
                else
                {
                    j = mp;
                    i = cp++;
                }
            }

            while (j < wild.Length && wild[j] == '*')
                j++;

            return j >= wild.Length;
        }
        
        public static IEnumerable<T> Distinct<T, TKey>(this IEnumerable<T> @this, Func<T, TKey> keySelector)
        {
            return @this.GroupBy(keySelector).Select(grps => grps).Select(e => e.First());
        }

        public static T If<T>(this T val, Func<T, bool> predicate, Func<T, T> func)
        {
            if (predicate(val))
            {
                return func(val);
            }
            return val;
        }

        public static T Chain<T>(this T source, Action<T> action)
        {
            action(source);
            return source;
        }

        public static bool RaiseStatic(this EventHandler eventhandler, object sender)
        {
            if (eventhandler == null)
                return false;

            try
            {
                eventhandler(sender, EventArgs.Empty);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.Assert(false);
            }

            return false;
        }

        public static bool RaiseStatic(this EventHandler<EventArgs> eventhandler, object sender)
        {
            if (eventhandler == null)
                return false;

            try
            {
                eventhandler(sender, EventArgs.Empty);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.Assert(false);
            }

            return false;
        }

        public static bool RaiseStatic(this EventHandler eventhandler, object sender, EventArgs e)
        {
            if (eventhandler == null)
                return false;

            try
            {
                eventhandler(sender, e);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.Assert(false);
            }

            return false;
        }

        public static bool RaiseStatic<T>(this EventHandler<T> eventhandler, object sender, T e) where T : EventArgs
        {
            if (eventhandler == null)
                return false;

            try
            {
                eventhandler(sender, e);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.Assert(false);
            }

            return false;
        }

        public static bool RaiseStatic(this PropertyChangedEventHandler eventhandler, object sender, PropertyChangedEventArgs e)
        {
            if (eventhandler == null)
                return false;

            try
            {
                eventhandler(sender, e);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                ex.LogError();                
            }

            return false;
        }

        public static bool RaiseStatic(this NotifyCollectionChangedEventHandler eventhandler, object sender, NotifyCollectionChangedEventArgs e)
        {
            if (eventhandler == null)
                return false;

            try
            {
                eventhandler(sender, e);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.Assert(false);
            }

            return false;
        }

        // ***************************

        public static Func<T, TResult> Memoize<T, TResult>(this Func<T, TResult> func)
        {
            var t = new Dictionary<T, TResult>();
            return n =>
            {
                if (t.ContainsKey(n)) return t[n];
                else
                {
                    var result = func(n);
                    t.Add(n, result);
                    return result;
                }
            };
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable) 
        {
            if (enumerable == null)
                return null;

            HashSet<T> hs = new HashSet<T>();
            foreach (T item in enumerable)
                hs.Add(item);
            return hs;
        }

        public static void AddDistinct<T>(this HashSet<T> hash, T item)
        {
            if (item != null && !hash.Contains(item))
                hash.Add(item);
        }

        public static void AddDistinct<T>(this HashSet<T> hash, IEnumerable<T> items)
        {
            if (items != null)
                items.ForEach(t => hash.AddDistinct(t));
        }

        public static void AddDistinct<TKey, TValue>(this Dictionary<TKey, TValue> dict, IEnumerable<TValue> items, Func<TValue, TKey> keySelector)
        {
            if (items != null)
                items.ForEach(t => dict.AddDistinct(t, keySelector));
        }

        public static void AddDistinct<TKey, TValue>(this Dictionary<TKey, TValue> dict, TValue item, Func<TValue, TKey> keySelector)
        {
            if (item == null)
                throw new ArgumentNullException();

            TKey key = keySelector(item);
            if (!dict.ContainsKey(key))
                dict.Add(key, item);
        }

        public static string Format(this string value, params object[] args)
        {
            if (value == null)
                return String.Empty;
            return string.Format(value, args);
        }                

        public static T MaxObject<T, U>(this List<T> source, Func<T, U> selector) where U : IComparable<U>
        {
            if (source == null) throw new ArgumentNullException("source");
            bool first = true;
            T maxObj = default(T);
            U maxKey = default(U);
            foreach (var item in source)
            {
                if (first)
                {
                    maxObj = item;
                    maxKey = selector(maxObj);
                    first = false;
                }
                else
                {
                    U currentKey = selector(item);
                    if (currentKey.CompareTo(maxKey) > 0)
                    {
                        maxKey = currentKey;
                        maxObj = item;
                    }
                }
            }
            if (first) throw new InvalidOperationException("Sequence is empty.");
            return maxObj;
        }

        //public static void DoubleBuffered<T>(this T obj, bool isDoubleBuffered)
        //{
        //    Type dgvType = obj.GetType();
        //    PropertyInfo pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.SetProperty);
        //    pi.SetValue(obj, isDoubleBuffered, null);
        //}        

        public static T GetAttribute<T>(this PropertyInfo pi) where T : Attribute
        {
            return pi.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
        }

        //var attribute = PropertyInfoInstance.GetAttribute<XmlSerializationAttribute>();

        public static Type FindCommonAncestor(this Type type, Type targetType)
        {
            if (targetType.IsAssignableFrom(type))
                return targetType;

            var baseType = targetType.BaseType;
            while (baseType != null && !baseType.IsPrimitive)
            {
                if (baseType.IsAssignableFrom(type))
                    return baseType;
                baseType = baseType.BaseType;
            }
            return null;
        } 

    }
}
