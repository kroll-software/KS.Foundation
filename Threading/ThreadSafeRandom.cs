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
			return Inst().NextDouble() > 0.5;
		}
    }

    internal class CryptoRandom
    {
        private const int BufferSize = 1024;  // must be a multiple of 4
        private byte[] RandomBuffer;
        private int BufferOffset;
        private RNGCryptoServiceProvider rng;

        public CryptoRandom()
            : this(BufferSize)
        {
        }

        public CryptoRandom(int buffersize)
        {
            if (buffersize % 4 != 0)
                throw new ArgumentException("CryptoRandom.BufferSize must be a multiple of 4");

            RandomBuffer = new byte[buffersize];
            rng = new RNGCryptoServiceProvider();
            BufferOffset = RandomBuffer.Length;
        }

        private void FillBuffer()
        {
            rng.GetBytes(RandomBuffer);
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
            rng.GetBytes(buff);
        }
    }
}
