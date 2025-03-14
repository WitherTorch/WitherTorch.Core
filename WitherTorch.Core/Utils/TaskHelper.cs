using System;
using System.Threading;
using System.Threading.Tasks;

namespace WitherTorch.Core.Utils
{
    internal static class TaskHelper
    {
        public static async Task<TResult?> DelayWithResult<TResult>(TimeSpan delay)
        {
            await Task.Delay(delay);
            return default;
        }

        public static async Task<TResult?> DelayWithResult<TResult>(TimeSpan delay, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return default;
        }
    }
}
