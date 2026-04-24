using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace WitherTorch.Core.Utils
{
    internal static class NullSafetyHelper
    {
        [DebuggerStepThrough]
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ThrowIfNull<T>(T? obj, [CallerArgumentExpression(nameof(obj))] string? argName = null)
        {
            if (obj is null)
                Throw(argName ?? nameof(obj));
            return obj;
        }

        [DoesNotReturn]
        [DebuggerStepThrough]
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Throw(string message)
            => throw new ArgumentNullException(message);
    }
}
