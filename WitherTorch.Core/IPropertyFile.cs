using System;

namespace WitherTorch.Core
{
    /// <summary>
    /// 表示一個設定檔案
    /// </summary>
    public interface IPropertyFile : IDisposable
    {
        /// <summary>
        /// 重新載入設定檔案
        /// </summary>
        void Reload();
        /// <summary>
        /// 儲存設定檔案
        /// </summary>
        /// <param name="force">是否強制執行儲存動作 (即使該設定檔案未改變)</param>
        void Save(bool force);
        /// <summary>
        /// 取得此設定檔案的描述器
        /// </summary>
        IPropertyFileDescriptor GetDescriptor();
        /// <summary>
        /// 設定此設定檔案的描述器
        /// </summary>
        void SetDescriptor(IPropertyFileDescriptor descriptor);
        /// <summary>
        /// 取得設定檔案的檔案路徑
        /// </summary>
        string GetFilePath();
    }
}
