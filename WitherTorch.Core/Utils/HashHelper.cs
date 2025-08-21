using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace WitherTorch.Core.Utils
{
    /// <summary>
    /// 包含一些與雜湊相關的工具方法，此類別為靜態類別
    /// </summary>
    public static partial class HashHelper
    {
        private static readonly Lazy<HashAlgorithm>[] _algorithms = new Lazy<HashAlgorithm>[(int)HashMethod._Last - 1]
        {
            new Lazy<HashAlgorithm>(MD5.Create, LazyThreadSafetyMode.PublicationOnly),
            new Lazy<HashAlgorithm>(SHA1.Create, LazyThreadSafetyMode.PublicationOnly),
            new Lazy<HashAlgorithm>(SHA256.Create, LazyThreadSafetyMode.PublicationOnly),
        };

        /// <summary>
        /// 將雜湊字串轉換為二進制的雜湊數值
        /// </summary>
        /// <param name="hexString">傳入的雜湊字串</param>
        /// <returns></returns>
        public unsafe static byte[] HexStringToByte(string hexString)
        {
            int len = hexString.Length >> 1;
            if (len <= 0)
                return Array.Empty<byte>();

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

        /// <summary>
        /// 將二進制的雜湊數值轉換為雜湊字串
        /// </summary>
        /// <param name="hash">傳入的雜湊數值</param>
        /// <returns></returns>
        public static string? ByteToHexString(byte[]? hash)
        {
            if (hash is null)
                return null;
            else
            {
                int hashLength = hash.Length;
                StringBuilder builder = new StringBuilder(hashLength * 2);
                for (int i = 0; i < hashLength; i++)
                {
                    builder.AppendFormat("{0:X2}", hash[i]);
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// 傳回以指定的 <see cref="HashMethod"/> 運算得到的雜湊數值
        /// </summary>
        /// <param name="stream">要進行運算的資料串流</param>
        /// <param name="method">對 <paramref name="stream"/> 進行雜湊運算時所要使用的方法</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ComputeHash(System.IO.Stream stream, HashMethod method) 
            => _algorithms[(int)method - 1].Value.ComputeHash(stream);

        /// <summary>
        /// 傳回 <paramref name="a"/> 與 <paramref name="b"/> 是否相等
        /// </summary>
        /// <param name="a">第一個陣列</param>
        /// <param name="b">第二個陣列</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool ByteArrayEquals(byte[]? a, byte[]? b)
        {
            if (a is null || b is null) return false;
            else if (ReferenceEquals(a, b)) return true;
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
