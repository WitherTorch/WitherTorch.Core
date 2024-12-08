using System;
using System.Runtime.CompilerServices;

namespace WitherTorch.Core.Utils
{
    internal static class ObjectUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        public static T ThrowIfNull<T>(T? obj, string? argName = null)
        {
            if (obj is null)
                throw new ArgumentNullException(argName ?? nameof(obj));
            return obj;
        }
    }
}
