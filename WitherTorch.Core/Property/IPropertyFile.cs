using System;

namespace WitherTorch.Core.Property
{
    /// <summary>
    /// 表示一個設定檔案
    /// </summary>
    public interface IPropertyFile : IDisposable
    {
        /// <summary>
        /// 取得這個設定檔案的路徑
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// 取得或設定此設定檔案的描述器
        /// </summary>
        IPropertyFileDescriptor? Descriptor { get; set; }

        /// <summary>
        /// 重新載入設定檔案
        /// </summary>
        void Reload();

        /// <summary>
        /// 儲存設定檔案
        /// </summary>
        /// <param name="force">是否強制執行儲存動作 (即使該設定檔案未改變)</param>
        void Save(bool force);
    }
}
