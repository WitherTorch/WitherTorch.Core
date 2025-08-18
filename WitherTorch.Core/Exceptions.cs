using System;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core
{
    /// <summary>
    /// 當所使用的伺服器軟體未被註冊時將擲出此例外狀況
    /// </summary>
    public sealed class ServerSoftwareIsNotRegisteredException : Exception
    {
        private Either<Type, string> _software;

        /// <summary>
        /// 觸發此例外狀況的伺服器類型 (可能為 <see langword="null"/>)
        /// </summary>
        public Type? ServerType => _software.IsLeft ? _software.Left : null;

        /// <summary>
        /// 觸發此例外狀況的伺服器軟體 ID (可能為 <see langword="null"/>)
        /// </summary>
        public string? SoftwareId => _software.IsRight ? _software.Right: null;

        /// <summary>
        /// 以伺服器類型建立一個新的 <see cref="ServerSoftwareIsNotRegisteredException"/> 物件
        /// </summary>
        /// <param name="softwareType">伺服器的類型</param>
        public ServerSoftwareIsNotRegisteredException(Type softwareType)
        {
            _software = Either.Left<Type, string>(softwareType);
        }

        /// <summary>
        /// 以軟體 ID 建立一個新的 <see cref="ServerSoftwareIsNotRegisteredException"/> 物件
        /// </summary>
        /// <param name="softwareId">伺服器軟體的 ID</param>
        public ServerSoftwareIsNotRegisteredException(string softwareId)
        {
            _software = Either.Right<Type, string>(softwareId);
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                Either<Type, string> software = _software;
                if (software.IsLeft)
                    return $"{software.Left.FullName} is not a valid server type registered in {nameof(SoftwareRegister)} !";
                return $"{software.Right} is not registered in {nameof(SoftwareRegister)} !";
            }
        }
    }
}
