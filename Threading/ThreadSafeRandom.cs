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
using System.Security.Cryptography;

namespace KS.Foundation
{
    public static class ThreadSafeRandom
    {        
        [ThreadStatic]
        private static CryptoRandom m_Rnd;        

        private static CryptoRandom Inst()
        {
            CryptoRandom inst = m_Rnd;
            if (inst == null)
            {                
                m_Rnd = inst = new CryptoRandom();
            }
            return inst;
        }

        public static int Next()
        {
            return Inst().Next();
        }

        public static int Next(int maxValue)
        {
            return Inst().Next(maxValue);
        }

        public static int Next(int minValue, int maxValue)
        {
            return Inst().Next(minValue, maxValue);
        }

        //public static void NextBytes(byte[] buffer)
        //{
        //    Inst().NextBytes(buffer);
        //}

        public static double NextDouble()
        {
            return Inst().NextDouble();
        }

		public static bool NextBool()
		{
            //return Inst().NextDouble() > 0.5;
            return Inst().Next(2) > 0;
        }

        /// <summary>
        ///   Generates normally distributed numbers.
        /// </summary>
        /// <param name = "mean">Mean of the distribution</param>
        /// <param name = "std">Standard deviation</param>
        /// <returns></returns>
        public static double NextGaussian(double mean = 0, double std = 1)
        {
            if (std <= 0)
                throw new ArgumentOutOfRangeException(nameof(std), "Must be greater than zero.");

            var r = Inst();
            double u1 = r.NextDouble();
            double u2 = r.NextDouble();

            double rand_std_normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            double rand_normal = mean + std * rand_std_normal;
            return rand_normal;
        }

        /// <summary>
        ///   Generates values from a triangular distribution.
        /// </summary>
        /// <remarks>
        /// See http://en.wikipedia.org/wiki/Triangular_distribution
        /// </remarks>
        /// <param name = "min">Minimum</param>
        /// <param name = "max">Maximum</param>
        /// <param name = "mode">Mode (most frequent value)</param>
        /// <returns></returns>
        public static double NextTriangular(double min, double max, double mode)
        {
            double u = Inst().NextDouble();
            return u < (mode - min) / (max - min)
                       ? min + Math.Sqrt(u * (max - min) * (mode - min))
                       : max - Math.Sqrt((1 - u) * (max - min) * (max - mode));
        }
    }

    internal class CryptoRandom
    {
        private const int BufferSize = 1024;  // must be a multiple of 4
        private byte[] RandomBuffer;
        private int BufferOffset;        

        public CryptoRandom()
            : this(BufferSize)
        {
        }

        public CryptoRandom(int buffersize)
        {
            if (buffersize % 4 != 0)
                throw new ArgumentException("CryptoRandom.BufferSize must be a multiple of 4");

            RandomBuffer = new byte[buffersize];            
            BufferOffset = RandomBuffer.Length;
        }

        private void FillBuffer()
        {            
            RandomNumberGenerator.Fill(RandomBuffer);            
            BufferOffset = 0;
        }

        public int Next()
        {
            if (BufferOffset >= RandomBuffer.Length)
            {
                FillBuffer();
            }

            int val = BitConverter.ToInt32(RandomBuffer, BufferOffset) & 0x7fffffff;
            //System.Threading.Interlocked.Add(ref BufferOffset, sizeof(int));
            BufferOffset += sizeof(int);
            return val;
        }

        public int Next(int maxValue)
        {
            if (maxValue == 0)
                return 0;
            else
                return Next() % maxValue;
        }

        public int Next(int minValue, int maxValue)
        {
            if (maxValue < minValue)
            {
                throw new ArgumentOutOfRangeException("maxValue must be greater than or equal to minValue");
            }
            int range = maxValue - minValue;
            return minValue + Next(range);
        }

        public double NextDouble()
        {
            int val = Next();
            return (double)val / int.MaxValue;
        }

        public void GetBytes(byte[] buff)
        {
            RandomNumberGenerator.Fill(buff);
        }
    }
}
