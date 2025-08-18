using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS8618

namespace WitherTorch.Core.Utils
{
    internal enum EitherSide : uint
    {
        None,
        Left,
        Right
    }

    /// <summary>
    /// 方便建立 <see cref="Either{TLeft, TRight}"/> 的工具類別，此類別為靜態類別
    /// </summary>
    public static class Either
    {
        /// <summary>
        /// 以指定的左側項目，建立新的 <see cref="Either{TLeft, TRight}"/> 結構，其中 TRight 為 <see cref="object"/> 類型
        /// </summary>
        /// <typeparam name="TLeft">左側項目的類型</typeparam>
        /// <param name="left">要建立 <see cref="Either{TLeft, TRight}"/> 結構的項目</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Either<TLeft, object> Left<TLeft>(TLeft left)
            => Either<TLeft, object>.CreateLeft(left);

        /// <summary>
        /// 以指定的左側項目，建立新的 <see cref="Either{TLeft, TRight}"/> 結構
        /// </summary>
        /// <typeparam name="TLeft">左側項目的類型</typeparam>
        /// <typeparam name="TRight">右側項目的類型</typeparam>
        /// <param name="left">要建立 <see cref="Either{TLeft, TRight}"/> 結構的項目</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Either<TLeft, TRight> Left<TLeft, TRight>(TLeft left)
            => Either<TLeft, TRight>.CreateLeft(left);

        /// <summary>
        /// 以指定的右側項目，建立新的 <see cref="Either{TLeft, TRight}"/> 結構，其中 TLeft 為 <see cref="object"/> 類型
        /// </summary>
        /// <typeparam name="TRight">右側項目的類型</typeparam>
        /// <param name="right">要建立 <see cref="Either{TLeft, TRight}"/> 結構的項目</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Either<object, TRight> Right<TRight>(TRight right)
            => Either<object, TRight>.CreateRight(right);

        /// <summary>
        /// 以指定的右側項目，建立新的 <see cref="Either{TLeft, TRight}"/> 結構
        /// </summary>
        /// <typeparam name="TLeft">左側項目的類型</typeparam>
        /// <typeparam name="TRight">右側項目的類型</typeparam>
        /// <param name="right">要建立 <see cref="Either{TLeft, TRight}"/> 結構的項目</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Either<TLeft, TRight> Right<TLeft, TRight>(TRight right)
            => Either<TLeft, TRight>.CreateRight(right);
    }

    /// <summary>
    /// 一種可包含 <typeparamref name="TLeft"/> 和 <typeparamref name="TRight"/> 之間任一種資料的結構
    /// </summary>
    /// <typeparam name="TLeft"><see cref="Either{TLeft, TRight}.Left"/> 的類型</typeparam>
    /// <typeparam name="TRight"><see cref="Either{TLeft, TRight}.Right"/> 的類型</typeparam>
    [StructLayout(LayoutKind.Explicit, Pack = sizeof(uint))]
    public readonly struct Either<TLeft, TRight>
    {
        /// <summary>
        /// 取得空白的 <see cref="Either{TLeft, TRight}"/> 的結構
        /// </summary>
        public static readonly Either<TLeft, TRight> Empty = default;

        [FieldOffset(0)]
        private readonly EitherSide _side;
        [FieldOffset(sizeof(uint))]
        private readonly TLeft _left;
        [FieldOffset(sizeof(uint))]
        private readonly TRight _right;

        /// <summary>
        /// 取得該結構中位於左側的資料
        /// </summary>
        /// <remarks>如果此結構的資料並非在左側，則擲出 <see cref="InvalidOperationException"/></remarks>
        public TLeft Left
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_side != EitherSide.Left)
                    throw new InvalidOperationException();
                return _left;
            }
        }

        /// <summary>
        /// 取得該結構中位於右側的資料
        /// </summary>
        /// <remarks>如果此結構的資料並非在右側，則擲出 <see cref="InvalidOperationException"/></remarks>
        public TRight Right
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_side != EitherSide.Right)
                    throw new InvalidOperationException();
                return _right;
            }
        }

        /// <summary>
        /// 檢查此結構包含的資料是否位於左側
        /// </summary>
        public bool IsLeft => _side == EitherSide.Left;

        /// <summary>
        /// 檢查此結構包含的資料是否位於右側
        /// </summary>
        public bool IsRight => _side == EitherSide.Right;

        /// <summary>
        /// 檢查此結構是否不包含任何資料
        /// </summary>
        public bool IsEmpty => _side == EitherSide.None;

        private Either(EitherSide side, TLeft? left = default, TRight? right = default)
        {
            _side = side;
            switch (side)
            {
                case EitherSide.Left:
                    _left = left!;
                    break;
                case EitherSide.Right:
                    _right = right!;
                    break;
            }
        }

        /// <summary>
        /// 以指定的左側項目，建立新的 <see cref="Either{TLeft, TRight}"/> 結構
        /// </summary>
        /// <param name="left">要建立 <see cref="Either{TLeft, TRight}"/> 結構的項目</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Either<TLeft, TRight> CreateLeft(TLeft left)
            => new Either<TLeft, TRight>(EitherSide.Left, left: left);

        /// <summary>
        /// 以指定的右側項目，建立新的 <see cref="Either{TLeft, TRight}"/> 結構
        /// </summary>
        /// <param name="right">要建立 <see cref="Either{TLeft, TRight}"/> 結構的項目</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Either<TLeft, TRight> CreateRight(TRight right)
            => new Either<TLeft, TRight>(EitherSide.Right, right: right);

        /// <summary>
        /// 傳回一個左側項目類型為 <typeparamref name="T"/> 的新結構
        /// </summary>
        /// <typeparam name="T">要更換的左側類型</typeparam>
        /// <returns>更換類型後的新結構</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Either<T, TRight> CastLeft<T>() where T : class
        {
            if (_side != EitherSide.Right)
                return Either<T, TRight>.Empty;
            return new Either<T, TRight>(EitherSide.Right, right: _right);
        }

        /// <summary>
        /// 傳回一個右側項目類型為 <typeparamref name="T"/> 的新結構
        /// </summary>
        /// <typeparam name="T">要更換的右側類型</typeparam>
        /// <returns>更換類型後的新結構</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Either<TLeft, T> CastRight<T>() where T : class
        {
            if (_side != EitherSide.Left)
                return Either<TLeft, T>.Empty;
            return new Either<TLeft, T>(EitherSide.Left, left: _left);
        }

        /// <summary>
        /// 根據此 <see cref="Either{TLeft, TRight}"/> 的資料位置，執行指定的委派
        /// </summary>
        /// <param name="leftAction">資料位於左側時所要執行的委派</param>
        /// <param name="rightAction">資料位於右側時所要執行的委派</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(Action<TLeft> leftAction, Action<TRight> rightAction)
        {
            switch (_side)
            {
                case EitherSide.Left:
                    leftAction.Invoke(_left);
                    return;
                case EitherSide.Right:
                    rightAction.Invoke(_right);
                    return;
            }
        }

        /// <summary>
        /// 根據此 <see cref="Either{TLeft, TRight}"/> 的資料位置，執行指定的委派
        /// </summary>
        /// <typeparam name="TArg">委派內參數的類型</typeparam>
        /// <param name="leftAction">資料位於左側時所要執行的委派</param>
        /// <param name="rightAction">資料位於右側時所要執行的委派</param>
        /// <param name="state">要輸入至委派的參數</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke<TArg>(Action<TLeft, TArg> leftAction, Action<TRight, TArg> rightAction, TArg state)
        {
            switch (_side)
            {
                case EitherSide.Left:
                    leftAction.Invoke(_left, state);
                    return;
                case EitherSide.Right:
                    rightAction.Invoke(_right, state);
                    return;
            }
        }

        /// <summary>
        /// 根據此 <see cref="Either{TLeft, TRight}"/> 的資料位置，執行指定的委派
        /// </summary>
        /// <typeparam name="TResult">執行結果的類型</typeparam>
        /// <param name="leftFunc">資料位於左側時所要執行的委派</param>
        /// <param name="rightFunc">資料位於右側時所要執行的委派</param>
        /// <returns>執行結果。若此結構本身為空，則傳回 <see langword="default"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult? Invoke<TResult>(Func<TLeft, TResult> leftFunc, Func<TRight, TResult> rightFunc)
            => _side switch
            {
                EitherSide.Left => leftFunc.Invoke(_left),
                EitherSide.Right => rightFunc.Invoke(_right),
                _ => default
            };

        /// <summary>
        /// 根據此 <see cref="Either{TLeft, TRight}"/> 的資料位置，執行指定的委派
        /// </summary>
        /// <typeparam name="TArg">委派內參數的類型</typeparam>
        /// <typeparam name="TResult">執行結果的類型</typeparam>
        /// <param name="leftFunc">資料位於左側時所要執行的委派</param>
        /// <param name="rightFunc">資料位於右側時所要執行的委派</param>
        /// <param name="state">要輸入至委派的參數</param>
        /// <returns>執行結果。若此結構本身為空，則傳回 <see langword="default"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult? Invoke<TArg, TResult>(Func<TLeft, TArg, TResult> leftFunc, Func<TRight, TArg, TResult> rightFunc, TArg state)
            => _side switch
            {
                EitherSide.Left => leftFunc.Invoke(_left, state),
                EitherSide.Right => rightFunc.Invoke(_right, state),
                _ => default
            };
    }
}
