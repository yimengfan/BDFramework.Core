using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MurmurHash.Net
{
    /// <summary>
    /// murmur hash算法只适合用来取文件hash值,不适合用来做加密
    /// </summary>
    public class MurmurHash3
    {
        public static uint Hash32(ReadOnlySpan<byte> bytes, uint seed=12345678u)
        {
            var length = bytes.Length;
            var h1 = seed;
            var remainder = length & 3;
            var position = length - remainder;
            for (var start = 0; start < position; start += 4)
            {
                h1 = (uint) ((int) RotateLeft(h1 ^ RotateLeft(MemoryMarshal.Read<uint>(bytes.Slice(start, 4)) * 3432918353U, 15) * 461845907U, 13) * 5 - 430675100);
            }

            if (remainder > 0)
            {
                uint num = 0;
                switch (remainder)
                {
                    case 1:
                        num ^= (uint) bytes[position];
                        break;
                    case 2:
                        num ^= (uint) bytes[position + 1] << 8;
                        goto case 1;
                    case 3:
                        num ^= (uint) bytes[position + 2] << 16;
                        goto case 2;
                }

                h1 ^= RotateLeft(num * 3432918353U, 15) * 461845907U;
            }

            h1 = FMix(h1 ^ (uint) length);

            return h1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint RotateLeft(uint x, byte r)
        {
            return x << (int) r | x >> 32 - (int) r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint FMix(uint h)
        {
            h = (uint) (((int) h ^ (int) (h >> 16)) * -2048144789);
            h = (uint) (((int) h ^ (int) (h >> 13)) * -1028477387);
            return h ^ h >> 16;
        }
    }
}
