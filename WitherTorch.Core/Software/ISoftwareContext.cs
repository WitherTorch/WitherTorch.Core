﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace WitherTorch.Core.Software
{
    /// <summary>
    /// 表示一個與特定伺服器軟體相關聯的介面
    /// </summary>
    public interface ISoftwareContext
    {
        /// <summary>
        /// 取得伺服器軟體所對應的軟體 ID
        /// </summary>
        /// <returns>軟體的唯一辨識符 (ID)</returns>
        string GetSoftwareId();

        /// <summary>
        /// 取得伺服器軟體所對應的伺服器物件類型
        /// </summary>
        /// <returns><see cref="CreateServerInstance"/> 傳回之物件的具體類型</returns>
        Type GetServerType();

        /// <summary>
        /// 取得伺服器軟體所支援的版本列表
        /// </summary>
        /// <returns></returns>
        string[] GetSoftwareVersions();

        /// <summary>
        /// 建立一個新的伺服器物件，或是傳回 <see langword="null"/> 表示建立失敗
        /// </summary>
        /// <returns>一個新的伺服器物件</returns>
        Server? CreateServerInstance(string serverDirectory);

        /// <summary>
        /// 在使用者呼叫 <see cref="SoftwareRegister.TryRegisterServerSoftware(ISoftwareContext)"/> 來註冊伺服器軟體時會呼叫的初始化程式碼
        /// </summary>
        /// <returns>初始化作業是否成功</returns>
        Task<bool> TryInitializeAsync(CancellationToken cancellationToken);
    }
}
