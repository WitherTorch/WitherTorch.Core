using System.Runtime.CompilerServices;

namespace WitherTorch.Core.Utils
{
    internal static class UnsafeHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TTo BitCast<TFrom, TTo>(in TFrom from) where TFrom : struct where TTo : struct
        {
#if NET8_0_OR_GREATER
            return Unsafe.BitCast<TFrom, TTo>(from);
#else
            return Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(in from));
#endif
        }
    }
}
