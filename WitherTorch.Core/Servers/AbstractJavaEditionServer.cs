using System.Runtime.CompilerServices;

namespace WitherTorch.Core.Servers
{
    /// <summary>
    /// 此類別為 Java 版伺服器軟體之基底類別，無法直接使用
    /// </summary>
    public abstract class AbstractJavaEditionServer<T> : Server<T>, IJavaEditionServer where T : Server<T>
    {
        protected Utils.MojangAPI.VersionInfo mojangVersionInfo;

        protected static void CallWhenStaticInitialize()
        {
            SoftwareRegistrationDelegate = Utils.MojangAPI.Initialize; //呼叫 Mojang API 進行版本列表提取
        }

        /// <summary>
        /// 子類別需實作此函式，作為 <c>mojangVersionInfo</c> 未主動生成時的備用生成方案
        /// </summary>
        abstract protected void BuildVersionInfo();

        /// <summary>
        /// 取得這個伺服器的版本詳細資訊 (由 Mojang API 提供)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utils.MojangAPI.VersionInfo GetMojangVersionInfo()
        {
            if (mojangVersionInfo is null) BuildVersionInfo();
            return mojangVersionInfo;
        }
    }

    public interface IJavaEditionServer
    {
        Utils.MojangAPI.VersionInfo GetMojangVersionInfo();
    }
}
