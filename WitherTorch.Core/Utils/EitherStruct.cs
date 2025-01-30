using System;

namespace WitherTorch.Core.Utils
{
    /// <summary>
    /// 方便建立 <see cref="EitherStruct{TLeft, TRight}"/> 的工具類別，此類別為靜態類別
    /// </summary>
    public static class Either
    {
        /// <summary>
        /// 以指定的左側項目，建立新的 <see cref="EitherStruct{TLeft, TRight}"/> 結構，其中 TRight 為 <see langword="object"/> 類型
        /// </summary>
        /// <typeparam name="TLeft">左側項目的類型</typeparam>
        /// <param name="left">要建立 <see cref="EitherStruct{TLeft, TRight}"/> 結構的項目</param>
        /// <returns></returns>
        public static EitherStruct<TLeft, object> Left<TLeft>(TLeft left) where TLeft : class
            => new EitherStruct<TLeft, object>(left);

        /// <summary>
        /// 以指定的左側項目，建立新的 <see cref="EitherStruct{TLeft, TRight}"/> 結構
        /// </summary>
        /// <typeparam name="TLeft">左側項目的類型</typeparam>
        /// <typeparam name="TRight">右側項目的類型</typeparam>
        /// <param name="left">要建立 <see cref="EitherStruct{TLeft, TRight}"/> 結構的項目</param>
        /// <returns></returns>
        public static EitherStruct<TLeft, TRight> Left<TLeft, TRight>(TLeft left) where TLeft : class where TRight : class
            => new EitherStruct<TLeft, TRight>(left);

        /// <summary>
        /// 以指定的右側項目，建立新的 <see cref="EitherStruct{TLeft, TRight}"/> 結構，其中 TLeft 為 <see langword="object"/> 類型
        /// </summary>
        /// <typeparam name="TRight">右側項目的類型</typeparam>
        /// <param name="right">要建立 <see cref="EitherStruct{TLeft, TRight}"/> 結構的項目</param>
        /// <returns></returns>
        public static EitherStruct<object, TRight> Right<TRight>(TRight right) where TRight : class
            => new EitherStruct<object, TRight>(right);

        /// <summary>
        /// 以指定的右側項目，建立新的 <see cref="EitherStruct{TLeft, TRight}"/> 結構
        /// </summary>
        /// <typeparam name="TLeft">左側項目的類型</typeparam>
        /// <typeparam name="TRight">右側項目的類型</typeparam>
        /// <param name="right">要建立 <see cref="EitherStruct{TLeft, TRight}"/> 結構的項目</param>
        /// <returns></returns>
        public static EitherStruct<TLeft, TRight> Right<TLeft, TRight>(TRight right) where TLeft : class where TRight : class
            => new EitherStruct<TLeft, TRight>(right);
    }

    /// <summary>
    /// 一種可包含 <typeparamref name="TLeft"/> 和 <typeparamref name="TRight"/> 之間任一種資料的結構
    /// </summary>
    /// <typeparam name="TLeft"><see cref="EitherStruct{TLeft, TRight}.Left"/> 的類型</typeparam>
    /// <typeparam name="TRight"><see cref="EitherStruct{TLeft, TRight}.Right"/> 的類型</typeparam>
    public readonly struct EitherStruct<TLeft, TRight> where TLeft : class where TRight : class
    {
        private readonly TLeft? _left;
        private readonly TRight? _right;

        /// <summary>
        /// 取得該結構中位於左側的資料
        /// </summary>
        /// <remarks>如果此結構的資料並非在左側，則擲出 <see cref="InvalidOperationException"/></remarks>
        public TLeft Left => _left ?? throw new InvalidOperationException();

        /// <summary>
        /// 取得該結構中位於右側的資料
        /// </summary>
        /// <remarks>如果此結構的資料並非在右側，則擲出 <see cref="InvalidOperationException"/></remarks>
        public TRight Right => _right ?? throw new InvalidOperationException();

        /// <summary>
        /// 檢查此結構包含的資料是否位於左側
        /// </summary>
        public bool IsLeft => _left is not null;

        /// <summary>
        /// 檢查此結構包含的資料是否位於右側
        /// </summary>
        public bool IsRight => _right is not null;

        /// <summary>
        /// 檢查此結構是否不包含任何資料
        /// </summary>
        public bool IsEmpty => _left is null && _right is null;

        /// <summary>
        /// 以指定的左側項目，建立新的 <see cref="EitherStruct{TLeft, TRight}"/> 結構
        /// </summary>
        /// <param name="left">要建立 <see cref="EitherStruct{TLeft, TRight}"/> 結構的項目</param>
        /// <returns></returns>
        public EitherStruct(TLeft left)
        {
            _left = left;
            _right = default;
        }

        /// <summary>
        /// 以指定的右側項目，建立新的 <see cref="EitherStruct{TLeft, TRight}"/> 結構
        /// </summary>
        /// <param name="right">要建立 <see cref="EitherStruct{TLeft, TRight}"/> 結構的項目</param>
        /// <returns></returns>
        public EitherStruct(TRight right)
        {
            _left = default;
            _right = right;
        }

        /// <summary>
        /// 傳回一個左側項目類型為 <typeparamref name="T"/> 的新結構
        /// </summary>
        /// <typeparam name="T">要更換的左側類型</typeparam>
        /// <returns>更換類型後的新結構</returns>
        public EitherStruct<T, TRight> CastLeft<T>() where T : class
        {
            TRight? right = _right;
            if (right is null)
                return default;
            return new EitherStruct<T, TRight>(right);
        }

        /// <summary>
        /// 傳回一個右側項目類型為 <typeparamref name="T"/> 的新結構
        /// </summary>
        /// <typeparam name="T">要更換的右側類型</typeparam>
        /// <returns>更換類型後的新結構</returns>
        public EitherStruct<TLeft, T> CastRight<T>() where T : class
        {
            TLeft? left = _left;
            if (left is null)
                return default;
            return new EitherStruct<TLeft, T>(left);
        }
    }
}
