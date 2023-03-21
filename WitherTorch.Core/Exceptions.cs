using System;

namespace WitherTorch.Core
{
    /// <summary>
    /// 當所使用的伺服器軟體未被註冊時將擲出此例外狀況
    /// </summary>
    public sealed class ServerSoftwareIsNotRegisteredException : Exception
    {
        public string SoftwareTypeName { get; }

        public ServerSoftwareIsNotRegisteredException(Type softwareType)
        {
            SoftwareTypeName = softwareType.ToString();
        }

        public ServerSoftwareIsNotRegisteredException(string softwareTypeName)
        {
            SoftwareTypeName = softwareTypeName;
        }

        public override string Message => $"{SoftwareTypeName} 未註冊在 SoftwareRegister 中!";
    }
}
