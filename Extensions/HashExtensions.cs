using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KS.Foundation
{
    public static class HashExtensions
    {
        const Int32 FNV1_prime_32 = 16777619;
        const Int32 FNV1_basis_32 = unchecked((int)2166136261);
        const Int64 FNV1_prime_64 = 1099511628211;
        const Int64 FNV1_basis_64 = unchecked((int)14695981039346656037);

        public static Int32 GetHash(Int64 x)
        {
            return ((Int32)x).CombineHash((Int32)(((UInt64)x) >> 32));
        }

        private static Int32 Fold(Int32 hash, byte value)
        {
            return (hash * FNV1_prime_32) ^ (Int32)value;
        }

        private static Int32 Fold(Int32 hash, Int32 value)
        {
            return Fold(Fold(Fold(Fold(hash,
                (byte)value),
                (byte)(((UInt32)value) >> 8)),
                (byte)(((UInt32)value) >> 16)),
                (byte)(((UInt32)value) >> 24));
        }

        public static Int32 CombineHash(this Int32 x, Int32 y)
        {
            return Fold(Fold(FNV1_basis_32, x), y);
        }

        public static Int32 CombineHash(this Int32 x, Int32 y, Int32 z)
        {
            return Fold(Fold(Fold(FNV1_basis_32, x), y), z);
        }
    }
}
