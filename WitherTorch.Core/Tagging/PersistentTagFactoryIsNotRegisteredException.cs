using System;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Tagging
{
    /// <summary>
    /// 當所使用的持久化標籤工廠未被註冊時將擲出此例外狀況
    /// </summary>
    public sealed class PersistentTagFactoryIsNotRegisteredException : Exception
    {
        private static readonly Func<Type, string> _messageActionForLeft =
            val => $"{val.FullName} is not a valid Persistent tag type registered in {nameof(PersistentTagFactoryRegister)} !";
        private static readonly Func<string, string> _messageActionForRight =
            val => $"{val} is not registered in {nameof(PersistentTagFactoryRegister)} !";

        private readonly Either<Type, string> _either;

        /// <summary>
        /// 觸發此例外狀況的持久化標籤類型 (可能為 <see langword="null"/>)
        /// </summary>
        public Type? TagType => _either.IsLeft ? _either.Left : null;

        /// <summary>
        /// 觸發此例外狀況的持久化標籤類型 ID (可能為 <see langword="null"/>)
        /// </summary>
        public string? TagTypeId => _either.IsRight ? _either.Right : null;

        internal PersistentTagFactoryIsNotRegisteredException(Type type)
        {
            _either = Either.Left<Type, string>(type);
        }

        internal PersistentTagFactoryIsNotRegisteredException(string identifier)
        {
            _either = Either.Right<Type, string>(identifier);
        }

        /// <inheritdoc/>
        public override string Message => _either.Invoke(_messageActionForLeft, _messageActionForRight) ?? string.Empty;
    }
}
