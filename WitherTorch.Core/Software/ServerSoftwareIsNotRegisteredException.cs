using System;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Software
{
    /// <summary>
    /// 當所使用的伺服器軟體未被註冊時將擲出此例外狀況
    /// </summary>
    public sealed class ServerSoftwareIsNotRegisteredException : Exception
    {
        private static readonly Func<Type, string> _messageActionForLeft =
            val => $"{val.FullName} is not a valid server type registered in {nameof(SoftwareRegister)} !";
        private static readonly Func<string, string> _messageActionForRight =
            val => $"{val} is not registered in {nameof(SoftwareRegister)} !";
        
        private readonly Either<Type, string> _either;

        /// <summary>
        /// 觸發此例外狀況的伺服器類型 (可能為 <see langword="null"/>)
        /// </summary>
        public Type? ServerType => _either.IsLeft ? _either.Left : null;

        /// <summary>
        /// 觸發此例外狀況的伺服器軟體 ID (可能為 <see langword="null"/>)
        /// </summary>
        public string? SoftwareId => _either.IsRight ? _either.Right: null;

        internal ServerSoftwareIsNotRegisteredException(Type softwareType)
        {
            _either = Either.Left<Type, string>(softwareType);
        }

        internal ServerSoftwareIsNotRegisteredException(string softwareId)
        {
            _either = Either.Right<Type, string>(softwareId);
        }

        /// <inheritdoc/>
        public override string Message => _either.Invoke(_messageActionForLeft, _messageActionForRight) ?? string.Empty;
    }
}
