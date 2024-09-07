using System;

namespace WitherTorch.Core.Utils
{
    internal sealed class AutoDisposer
    {
        public static AutoDisposer<T> Create<T>(T disposable) where T : class, IDisposable
        {
            return disposable is null ? null : new AutoDisposer<T>(disposable);
        }
    }
    
    internal sealed class AutoDisposer<T> where T : class, IDisposable
    {
        private readonly T _disposable;

        public T Data => _disposable;

        internal AutoDisposer(T disposable)
        {
            if (disposable is null)
                throw new ArgumentNullException(nameof(disposable));
            _disposable = disposable;
        }

        ~AutoDisposer()
        {
            _disposable.Dispose();
        }
    }
}
