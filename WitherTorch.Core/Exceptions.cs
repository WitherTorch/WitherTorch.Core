using System;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core
{
    /// <summary>
    /// 當所使用的伺服器軟體未被註冊時將擲出此例外狀況
    /// </summary>
    public sealed class ServerSoftwareIsNotRegisteredException : Exception
    {
        public EitherStruct<Type, string> Software { get; }

        public ServerSoftwareIsNotRegisteredException(Type softwareType)
        {
            Software = Either.Left<Type, string>(softwareType);
        }

        public ServerSoftwareIsNotRegisteredException(string softwareId)
        {
            Software = Either.Right<Type, string>(softwareId);
        }

        public override string Message
        {
            get
            {
                EitherStruct<Type, string> software = Software;
                if (software.IsLeft)
                    return $"{software.Left.FullName} is not a valid server type registered in {nameof(SoftwareRegister)} !";
                return $"{software.Right} is not registered in {nameof(SoftwareRegister)} !";
            }
        }
    }
}
