using System;
using System.Threading;
using System.Threading.Tasks;

namespace WitherTorch.Core.Utils
{
    internal static class TaskHelper
    {
        public static Task<TResult?> WaitForResultAsync<TResult>(Func<TResult?> func, TimeSpan delay)
            => WaitForResultAsync(func, delay, new CancellationTokenSource(), leaveOpen: false);

        public static Task<TResult?> WaitForResultAsync<TResult>(Func<TResult?> func, TimeSpan delay, CancellationTokenSource cancellationTokenSource)
            => WaitForResultAsync(func, delay, cancellationTokenSource, leaveOpen: false);

        private static async Task<TResult?> WaitForResultAsync<TResult>(Func<TResult?> func, TimeSpan delay,
            CancellationTokenSource cancellationTokenSource, bool leaveOpen)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            Task<TResult?> workingTask = Task.Factory.StartNew(func, cancellationToken, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current);
            Task resultTask = await Task.WhenAny(workingTask, Task.Delay(delay, cancellationToken));
            if (!leaveOpen)
                cancellationTokenSource.Dispose();
            if (ReferenceEquals(resultTask, workingTask))
            {
#if NET8_0_OR_GREATER
                if (!workingTask.IsCompletedSuccessfully)
                    return default;
#else
                if (workingTask.IsCanceled || workingTask.IsFaulted)
                    return default;
#endif
                return workingTask.Result;
            }
            return default;
        }

        public static Task<TResult?> WaitForResultAsync<TResult>(Func<CancellationToken, Task<TResult?>> factoryFunc, TimeSpan delay)
            => WaitForResultAsync(factoryFunc, delay, new CancellationTokenSource(), leaveOpen: false);

        public static Task<TResult?> WaitForResultAsync<TResult>(Func<CancellationToken, Task<TResult?>> factoryFunc, TimeSpan delay,
            CancellationTokenSource cancellationTokenSource)
            => WaitForResultAsync(factoryFunc, delay, cancellationTokenSource, leaveOpen: false);

        private static async Task<TResult?> WaitForResultAsync<TResult>(Func<CancellationToken, Task<TResult?>> factoryFunc, TimeSpan delay,
            CancellationTokenSource cancellationTokenSource, bool leaveOpen)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            Task<TResult?> workingTask = factoryFunc.Invoke(cancellationToken);
            Task resultTask = await Task.WhenAny(workingTask, Task.Delay(delay, cancellationToken));
            if (!leaveOpen)
                cancellationTokenSource.Dispose();
            if (ReferenceEquals(resultTask, workingTask))
            {
#if NET8_0_OR_GREATER
                if (!workingTask.IsCompletedSuccessfully)
                    return default;
#else
                if (workingTask.IsCanceled || workingTask.IsFaulted)
                    return default;
#endif
                return workingTask.Result;
            }
            return default;
        }
    }
}
