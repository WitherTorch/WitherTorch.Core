using System.Runtime.CompilerServices;

namespace WitherTorch.Core.Servers
{
    /// <summary>
    /// 此類別為 Java 版伺服器軟體之基底類別，無法直接使用
    /// </summary>
    public abstract class AbstractJavaEditionServer<T> : Server<T> where T : AbstractJavaEditionServer<T>
    {
        protected Utils.MojangAPI.VersionInfo mojangVersionInfo;
        
        protected AbstractJavaEditionServer()
        {
            //呼叫 Mojang API 進行版本列表提取
            SoftwareRegistrationDelegate += Utils.MojangAPI.Initialize;
        }

        /// <summary>
        /// 子類別需實作此函式，作為 <c>mojangVersionInfo</c> 未主動生成時的備用生成方案
        /// </summary>
        abstract protected void BuildVersionInfo();

        /// <summary>
        /// 取得這個伺服器的版本詳細資訊 (由 Mojang API 提供)
        /// </summary>
        public Utils.MojangAPI.VersionInfo GetMojangVersionInfo()
        {
            if (mojangVersionInfo.IsEmpty()) BuildVersionInfo();
            return mojangVersionInfo;
        }
    }
}
