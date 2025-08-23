using System;
using System.IO;

namespace WitherTorch.Core.Runtime
{
    /// <summary>
    /// 暫存檔案介面，此介面會用在一些需要暫時存放資料以便下一步存取的地方
    /// </summary>
    /// <remarks>
    /// 使用此介面後記得要呼叫 <see cref="IDisposable.Dispose"/> 以進行暫存檔案的回收與刪除
    /// </remarks>
    public interface ITempFileInfo : IDisposable
    {
        /// <summary>
        /// 該檔案的檔案名稱 (含副檔名)
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 該檔案的絕對路徑
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// 開啟一個指向此暫存檔案的 <see cref="Stream"/> 物件
        /// </summary>
        /// <inheritdoc cref="FileStream(string, FileMode, FileAccess, FileShare, int, bool)"/>
        public Stream Open(FileAccess access, int bufferSize, bool useAsync);
    }
}
