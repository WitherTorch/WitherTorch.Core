namespace WitherTorch.Core.Utils
{
    public static class Either
    {
        public static EitherStruct<TLeft, object> Left<TLeft>(TLeft left) where TLeft : class
            => new EitherStruct<TLeft, object>(left);

        public static EitherStruct<TLeft, TRight> Left<TLeft, TRight>(TLeft left) where TLeft : class where TRight : class
            => new EitherStruct<TLeft, TRight>(left);

        public static EitherStruct<object, TRight> Right<TRight>(TRight right) where TRight : class
            => new EitherStruct<object, TRight>(right);

        public static EitherStruct<TLeft, TRight> Right<TLeft, TRight>(TRight right) where TLeft : class where TRight : class
            => new EitherStruct<TLeft, TRight>(right);
    }

    public readonly ref struct EitherStruct<TLeft, TRight> where TLeft : class where TRight : class
    {
        private readonly TLeft? _left;
        private readonly TRight? _right;

        public TLeft Left => ObjectUtils.ThrowIfNull(_left);

        public TRight Right => ObjectUtils.ThrowIfNull(_right);

        public bool IsLeft => _left is not null;

        public bool IsRight => _right is not null;

        public EitherStruct(TLeft left)
        {
            _left = left;
            _right = default;
        }

        public EitherStruct(TRight right)
        {
            _left = default;
            _right = right;
        }

        public EitherStruct<T, TRight> CastLeft<T>() where T : class
        {
            return new EitherStruct<T, TRight>(ObjectUtils.ThrowIfNull(_right));
        }

        public EitherStruct<TLeft, T> CastRight<T>() where T : class
        {
            return new EitherStruct<TLeft, T>(ObjectUtils.ThrowIfNull(_left));
        }
    }
}
