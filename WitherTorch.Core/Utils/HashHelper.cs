using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace WitherTorch.Core.Utils
{
    internal static class HashHelper
    {
        private static SHA1Managed sha1;

        public unsafe static byte[] HexStringToByte(string hexString)
        {
            int len = hexString.Length >> 1;
            if (len > 0)
            {
                byte[] result = new byte[len];
                fixed (char* hexStringPtr = hexString)
                fixed (byte* resultPtr = result)
                {
                    char* hexStringIterator = hexStringPtr;
                    byte* resultIterator = resultPtr;
                    for (int i = 0; i < len; i++)
                    {
                        byte b = 0;
                        char c1 = *hexStringIterator++;
                        char c2 = *hexStringIterator++;
                        if (c1 > '9')
                        {
                            if (c1 >= 'a' && c1 < 'g')
                                b = (byte)((10 + c1 - 'a') << 4);
                            else if (c1 >= 'A' && c1 < 'G')
                                b = (byte)((10 + c1 - 'A') << 4);
                        }
                        else if (c1 >= '0')
                        {
                            b = (byte)((c1 - '0') << 4);
                        }
                        if (c2 > '9')
                        {
                            if (c2 >= 'a' && c2 < 'g')
                                b |= (byte)(10 + c2 - 'a');
                            else if (c2 >= 'A' && c2 < 'G')
                                b |= (byte)(10 + c2 - 'A');
                        }
                        else if (c2 >= '0')
                        {
                            b |= (byte)(c2 - '0');
                        }
                        *resultIterator++ = b;
                    }
                }
                return result;
            }
            else
            {
                return Array.Empty<byte>();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ComputeSha1Hash(System.IO.Stream stream)
        {
            if (sha1 is null) sha1 = new SHA1Managed();
            return sha1.ComputeHash(stream);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool ByteArrayEquals(byte[] a, byte[] b)
        {
            int len = a.Length;
            if (len == b.Length)
            {
                fixed (byte* aPtr = a, bPtr = b)
                {
                    for (byte* aIterator = aPtr, bIterator = bPtr, endPtr = aPtr + len; aIterator < endPtr; aIterator++, bIterator++)
                        if (*aIterator != *bIterator) return false;
                }
                return true;
            }
            return false;
        }
    }
}
