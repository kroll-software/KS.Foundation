﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Globalization;

namespace KS.Foundation
{
    public static class ObjectExtensions
    {
		public static T Cast<T>(this object o, T t) where T : class
		{
			return o as T;
		}


        public static object CloneObject(this object obj)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.Clone));
                binaryFormatter.Serialize(memStream, obj);
                memStream.Position = 0;
                return binaryFormatter.Deserialize(memStream);
            }
        }


        public static bool IsBinaryEqualTo(this object obj, object obj1)
        {
            if (obj == null || obj1 == null)
            {
                if (obj == null && obj1 == null)
                    return true;
                else
                    return false;
            }

            using (MemoryStream memStream = new MemoryStream())
            {                
                BinaryFormatter binaryFormatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.Clone));
                binaryFormatter.Serialize(memStream, obj);
                byte[] b1 = memStream.ToArray();
                memStream.SetLength(0);

                binaryFormatter.Serialize(memStream, obj1);
                byte[] b2 = memStream.ToArray();

                if (b1.Length != b2.Length)
                    return false;                

                for (int i = 0; i < b1.Length; i++)
                {
                    if (b1[i] != b2[i])
                        return false;
                }

                return true;
            }
        }


		public static string ToInvariantString (this object value)
		{
			if (value == null)
				return String.Empty;
			else {
				switch (value.GetType ().Name) {
				case "Boolean":
					return ((bool)value).ToLowerString();
				case "DateTime":
					return ((DateTime)value).FormatUtcServerString();
				case "Int32":
					return ((int)value).ToString(CultureInfo.InvariantCulture);
				case "Int16":
					return ((short)value).ToString(CultureInfo.InvariantCulture);
				case "Int64":
					return ((long)value).ToString(CultureInfo.InvariantCulture);
				case "Double":
					return ((double)value).ToString(CultureInfo.InvariantCulture);
				case "Single":
					return ((float)value).ToString(CultureInfo.InvariantCulture);
				case "Byte":
					return ((byte)value).ToString(CultureInfo.InvariantCulture);
				case "SByte":
					return ((sbyte)value).ToString(CultureInfo.InvariantCulture);
				case "Decimal":
					return ((decimal)value).ToString(CultureInfo.InvariantCulture);
				case "UInt32":
					return ((uint)value).ToString(CultureInfo.InvariantCulture);
				case "UInt64":
					return ((ulong)value).ToString(CultureInfo.InvariantCulture);
				case "UInt16":
					return ((ushort)value).ToString(CultureInfo.InvariantCulture);
				default:
					return value.ToString();
				}
			}				
		}

        public static bool IsNull(this object obj)
        {
            return obj == null || obj == DBNull.Value;
        }

		public static bool IsDefault<T>(this T obj)
		{
			return Equals(obj, default(T));
		}

        public static string SafeString(this object obj)
        {
            if (obj == null || obj == DBNull.Value)
				return String.Empty;
            else            
                return Convert.ToString(obj);
        }

        public static int SafeInt(this object obj)
        {
            return SafeInt(obj, 0);
        }

        public static int SafeInt(this object obj, int DefaultValue)
        {
            if (obj == null || obj == DBNull.Value)
                return DefaultValue;
            else if (obj is int)
                return (int)obj;
            else if (obj is byte)
                return (int)(byte)obj;
            else if (obj is sbyte)
                return (int)(sbyte)obj;
            else if (obj is Int16)
                return (int)(Int16)obj;
            else if (obj is UInt16)
                return (int)(UInt16)obj;
			else if (obj is bool)
				return (bool)obj ? 1 : 0;

            else
            {
                try
                {
                    if (obj is double)
                        return (int)(double)obj;
                    else if (obj is float)
                        return (int)(float)obj;
                    else if (obj is decimal)
                        return (int)(decimal)obj;

                    else if (obj is Int64)
                        return (int)(Int64)obj;
                    
                    else if (obj is UInt32)
                        return (int)(UInt32)obj;
                    else if (obj is UInt64)
                        return (int)(UInt64)obj;

                    else
                    {
                        int retValue;
                        if (int.TryParse(obj.ToString(), out retValue))
                            return retValue;
                        else
                            return DefaultValue;
                    }
                }
                catch (Exception)
                {
                    return DefaultValue;
                }            
            }
        }

        public static long SafeLong(this object obj)
        {
            return SafeLong(obj, 0);
        }

        public static long SafeLong(this object obj, long DefaultValue)
        {
            if (obj == null || obj == DBNull.Value)
                return DefaultValue;
            else if (obj is int)
                return (long)(int)obj;
            else if (obj is byte)
                return (long)(byte)obj;
            else if (obj is sbyte)
                return (long)(sbyte)obj;
            else if (obj is Int16)
                return (long)(Int16)obj;
            else if (obj is UInt16)
                return (long)(UInt16)obj;

            else
            {
                try
                {
                    if (obj is double)
                        return (long)(double)obj;
                    else if (obj is float)
                        return (long)(float)obj;
                    else if (obj is decimal)
                        return (long)(decimal)obj;

                    else if (obj is Int64)
                        return (long)(Int64)obj;

                    else if (obj is UInt32)
                        return (long)(UInt32)obj;
                    else if (obj is UInt64)
                        return (long)(UInt64)obj;

                    else
                    {
                        long retValue;
                        if (long.TryParse(obj.ToString(), out retValue))
                            return retValue;
                        else
                            return DefaultValue;
                    }
                }
                catch (Exception)
                {
                    return DefaultValue;
                }
            }
        }

        public static decimal SafeDecimal(this object obj)
        {
            return SafeDecimal(obj, Decimal.Zero);
        }

        public static decimal SafeDecimal(this object obj, decimal DefaultValue)
        {
            if (obj == null || obj == DBNull.Value)
                return DefaultValue;
            else if (obj is decimal)
                return (decimal)obj;            
            else if (obj is double)
                return (decimal)(double)obj;            
            else if (obj is float)
                return (decimal)(float)obj;

            else if (obj is byte)
                return (decimal)(byte)obj;
            else if (obj is sbyte)
                return (int)(sbyte)obj;

            else if (obj is Int16)
                return (decimal)(Int16)obj;
            else if (obj is Int32)
                return (decimal)(Int32)obj;
            else if (obj is Int64)
                return (decimal)(Int64)obj;            

            else if (obj is UInt16)
                return (decimal)(UInt16)obj;
            else if (obj is UInt32)
                return (decimal)(UInt32)obj;
            else if (obj is UInt64)
                return (decimal)(UInt64)obj;            
            else
            {
                decimal retValue;
                if (decimal.TryParse(obj.ToString(), out retValue))
                    return retValue;
                else
                    return DefaultValue;
            }
        }

        public static double SafeDouble(this object obj)
        {
            return SafeDouble(obj, 0d);
        }

        public static double SafeDouble(this object obj, double DefaultValue)
        {
            if (obj == null || obj == DBNull.Value)
                return DefaultValue;
            else if (obj is double)
                return (double)obj;            
            else if (obj is float)            
                return (double)(float)obj;
            else if (obj is decimal)
                return (double)(decimal)obj;
                        
            else if (obj is byte)
                return (double)(byte)obj;
            else if (obj is sbyte)
                return (double)(sbyte)obj;

            else if (obj is Int16)
                return (double)(Int16)obj;
            else if (obj is Int32)
                return (double)(Int32)obj;
            else if (obj is Int64)
                return (double)(Int64)obj;
            else if (obj is UInt16)
                return (double)(UInt16)obj;
            else if (obj is UInt32)
                return (double)(UInt32)obj;
            else if (obj is UInt64)
                return (double)(UInt64)obj;
            else
            {
                double retValue;
                if (double.TryParse(obj.ToString(), out retValue))
                    return retValue;
                else
                    return DefaultValue;
            }
        }

        public static float SafeFloat(this object obj)
        {
            return SafeFloat(obj, 0f);
        }

        public static float SafeFloat(this object obj, float DefaultValue)
        {
            if (obj == null || obj == DBNull.Value)
                return DefaultValue;
            else if (obj is float)
                return (float)obj;
            else
            {
                try
                {
                    if (obj is double)
                        return (float)(double)obj;
                    else if (obj is decimal)
                        return (float)(decimal)obj;
                    else if (obj is Int16)
                        return (float)(Int16)obj;
                    else if (obj is Int32)
                        return (float)(Int32)obj;
                    else if (obj is Int64)
                        return (float)(Int64)obj;
                    else if (obj is UInt16)
                        return (float)(UInt16)obj;
                    else if (obj is UInt32)
                        return (float)(UInt32)obj;
                    else if (obj is UInt64)
                        return (float)(UInt64)obj;                    
                    else if (obj is byte)
                        return (float)(byte)obj;
                    else if (obj is sbyte)
                        return (float)(sbyte)obj;
                    else
                    {
                        float retValue;
                        if (float.TryParse(obj.ToString(), out retValue))
                            return retValue;
                        else
                            return DefaultValue;
                    }
                }
                catch (Exception)
                {
                    return DefaultValue;
                }
            }            
        }

        public static DateTime SafeDateTime(this object obj)
        {
            return SafeDateTime(obj, DateTime.MinValue);
        }

        public static DateTime SafeDateTime(this object obj, DateTime DefaultValue)
        {
            if (obj is DateTime)
                return (DateTime)obj;
            else if (obj == null || obj == DBNull.Value)
                return DefaultValue;            
            else
            {
                DateTime retValue;
                if (DateTime.TryParse(obj.ToString(), out retValue))
                    return retValue;
                else
                    return DefaultValue;
            }
        }

        public static TimeSpan SafeTimeSpan(this object obj)
        {
            return SafeTimeSpan(obj, TimeSpan.Zero);
        }

        public static TimeSpan SafeTimeSpan(this object obj, TimeSpan DefaultValue)
        {
            if (obj is TimeSpan)
                return (TimeSpan)obj;
            else if (obj == null || obj == DBNull.Value)
                return DefaultValue;
            else
            {
                TimeSpan retValue;
                if (TimeSpan.TryParse(obj.ToString(), out retValue))
                    return retValue;
                else
                    return DefaultValue;
            }
        }

		public static bool SafeBool(this object obj, bool defaultValue = false)
		{
			if (obj == null || obj == DBNull.Value)
				return defaultValue;

			if (obj is bool)
				return (bool)obj;

			string objString = obj.ToString ().Trim();

			int intVal;
			if (Int32.TryParse (objString, out intVal))
				return intVal != 0;
			
			switch (objString.ToUpperInvariant())
			{
			case "0":
			case "F":
			case "FALSE":
			case "FALSCH":
			case "OFF":
				return false;

			default:
				return true;
			}
		}

        public static bool IsDate(this object value)
        {
            bool returnValue;

            if (value == null || value == DBNull.Value) return false;

            if (value is DateTime)
                return true;

            try
            {
                //DateTime d = System.Convert.ToDateTime(value);            
                returnValue = true;
            }
            catch (Exception)
            {
                returnValue = false;
            }
            return returnValue;
        }

        public static bool IsTimeSpan(this object value)
        {
            if (value == null || value == DBNull.Value) return false;

            if (value is TimeSpan)
                return true;

            TimeSpan tmp;
            return TimeSpan.TryParse(value.ToString(), out tmp);
        }

        public static bool IsNumeric(this object value)
        {
            if (value == null || value == DBNull.Value)
                return false;

            if (value is int || value is double || value is float || value is decimal || value is byte || value is Int16 || value is Int64 || value is UInt16 || value is UInt32 || value is UInt64 || value is sbyte)
                return true;

            double tmp;            
            return Double.TryParse(value.ToString(), out tmp);
        }
    }
}
