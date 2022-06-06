namespace WitherTorch.Core.Servers
{
    /// <summary>
    /// 此類別為 Java 版伺服器軟體之基底類別，無法直接使用
    /// </summary>
    public abstract class AbstractJavaEditionServer : Server
    {
        protected Utils.MojangAPI.VersionInfo mojangVersionInfo;
        /// <summary>
        /// Java 版伺服器軟體的基底建構式<br/>
        /// 此建構式會自動呼叫 Mojang API 進行版本列表提取，無須在繼承類別中重複呼叫
        /// </summary>
        protected AbstractJavaEditionServer()
        {
            Utils.MojangAPI.Initialize();
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
